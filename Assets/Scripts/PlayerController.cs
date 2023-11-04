using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

namespace Player
{
    public class NetworkTimer {
        float timer;
        public float MinTimeBetweenTicks { get; }
        public int CurrentTick { get; private set; }
        
        public NetworkTimer(float serverTickRate) {
            MinTimeBetweenTicks = 1f / serverTickRate;
        }

        public void Update(float deltaTime) {
            timer += deltaTime;
        }
        
        public bool ShouldTick() {
            if (timer >= MinTimeBetweenTicks) {
                timer -= MinTimeBetweenTicks;
                CurrentTick++;
                return true;
            }

            return false;
        }
    }
    
    public class CircularBuffer<T> {
        T[] buffer;
        int bufferSize;
        
        public CircularBuffer(int bufferSize) {
            this.bufferSize = bufferSize;
            buffer = new T[bufferSize];
        }
        
        public void Add(T item, int index) => buffer[index % bufferSize] = item;
        public T Get(int index) => buffer[index % bufferSize];
        public void Clear() => buffer = new T[bufferSize];
    }
    
    public struct InputPayload : INetworkSerializable {
        public int tick;
        public DateTime timestamp;
        public ulong networkObjectId;
        public Vector2 inputVector;
        public Vector2 position;
        
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter {
            serializer.SerializeValue(ref tick);
            serializer.SerializeValue(ref timestamp);
            serializer.SerializeValue(ref networkObjectId);
            serializer.SerializeValue(ref inputVector);
            serializer.SerializeValue(ref position);
        }
    }
    
    public struct StatePayload : INetworkSerializable {
        public int tick;
        public ulong networkObjectId;
        public Vector2 position;
        public Quaternion rotation;
        public Vector2 velocity;
        public float angularVelocity;
        
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter {
            serializer.SerializeValue(ref tick);
            serializer.SerializeValue(ref networkObjectId);
            serializer.SerializeValue(ref position);
            serializer.SerializeValue(ref rotation);
            serializer.SerializeValue(ref velocity);
            serializer.SerializeValue(ref angularVelocity);
        }
    }
    
    public class PlayerController : NetworkBehaviour
    {
        [SerializeField] private Rigidbody2D rb;

        public Vector2 inputVector;
        
        // Netcode general
        NetworkTimer networkTimer;
        const float k_serverTickRate = 60f; // 60 FPS
        const int k_bufferSize = 1024;
    
        // Netcode client specific
        CircularBuffer<StatePayload> clientStateBuffer;
        CircularBuffer<InputPayload> clientInputBuffer;
        StatePayload lastServerState;
        StatePayload lastProcessedState;
    
        // Netcode server specific
        CircularBuffer<StatePayload> serverStateBuffer;
        Queue<InputPayload> serverInputQueue;
        
        [SerializeField] float reconciliationThreshold = 10f;


        private void Awake()
        {
            networkTimer = new NetworkTimer(k_serverTickRate);
            clientStateBuffer = new CircularBuffer<StatePayload>(k_bufferSize);
            clientInputBuffer = new CircularBuffer<InputPayload>(k_bufferSize);
            
            serverStateBuffer = new CircularBuffer<StatePayload>(k_bufferSize);
            serverInputQueue = new Queue<InputPayload>();
        }

        private void Update()
        {
            if (IsOwner)
            {
                float h = Input.GetAxis("Horizontal");
                float v = Input.GetAxis("Vertical");
                inputVector = new Vector2(h, v);
            }
            
            networkTimer.Update(Time.deltaTime);
        }

        private void FixedUpdate()
        {
            if (!IsOwner) return;

            while (networkTimer.ShouldTick())
            {
                HandleClientTick();
                HandleServerTick();
            }
        }
        
        void HandleClientTick() {
            if (!IsClient || !IsOwner) return;

            var currentTick = networkTimer.CurrentTick;
            var bufferIndex = currentTick % k_bufferSize;
            
            InputPayload inputPayload = new InputPayload() {
                tick = currentTick,
                timestamp = DateTime.Now,
                networkObjectId = NetworkObjectId,
                inputVector = inputVector,
                position = transform.position
            };
            
            clientInputBuffer.Add(inputPayload, bufferIndex);
            SendToServerRpc(inputPayload);
            
            StatePayload statePayload = ProcessMovement(inputPayload);
            clientStateBuffer.Add(statePayload, bufferIndex);
            
            HandleServerReconciliation();
        }
        
        [ServerRpc]
        void SendToServerRpc(InputPayload input) {
            serverInputQueue.Enqueue(input);
        }
        
        void HandleServerTick() {
            if (!IsServer) return;
             
            var bufferIndex = -1;
            InputPayload inputPayload = default;
            while (serverInputQueue.Count > 0) {
                inputPayload = serverInputQueue.Dequeue();
                
                bufferIndex = inputPayload.tick % k_bufferSize;
                
                StatePayload statePayload = ProcessMovement(inputPayload);
                serverStateBuffer.Add(statePayload, bufferIndex);
            }
            
            if (bufferIndex == -1) return;
            SendToClientRpc(serverStateBuffer.Get(bufferIndex));
        }
        
        [ClientRpc]
        void SendToClientRpc(StatePayload statePayload) {
            if (!IsOwner) return;
            lastServerState = statePayload;
        }
        
        bool ShouldReconcile() {
            bool isNewServerState = !lastServerState.Equals(default);
            bool isLastStateUndefinedOrDifferent = lastProcessedState.Equals(default) 
                                                   || !lastProcessedState.Equals(lastServerState);

            return isNewServerState && isLastStateUndefinedOrDifferent;
        }
        
        void HandleServerReconciliation() {
            if (!ShouldReconcile()) return;

            float positionError;
            int bufferIndex;
            
            bufferIndex = lastServerState.tick % k_bufferSize;
            if (bufferIndex - 1 < 0) return; // Not enough information to reconcile
            
            StatePayload rewindState = IsHost ? serverStateBuffer.Get(bufferIndex - 1) : lastServerState; // Host RPCs execute immediately, so we can use the last server state
            StatePayload clientState = IsHost ? clientStateBuffer.Get(bufferIndex - 1) : clientStateBuffer.Get(bufferIndex);
            positionError = Vector3.Distance(rewindState.position, clientState.position);

            if (positionError > reconciliationThreshold) {
                ReconcileState(rewindState);
            }

            lastProcessedState = rewindState;
        }
        
        void ReconcileState(StatePayload rewindState) {
            transform.position = rewindState.position;
            transform.rotation = rewindState.rotation;
            rb.velocity = rewindState.velocity;
            rb.angularVelocity = rewindState.angularVelocity;

            if (!rewindState.Equals(lastServerState)) return;
            
            clientStateBuffer.Add(rewindState, rewindState.tick % k_bufferSize);
            
            // Replay all inputs from the rewind state to the current state
            int tickToReplay = lastServerState.tick;

            while (tickToReplay < networkTimer.CurrentTick) {
                int bufferIndex = tickToReplay % k_bufferSize;
                StatePayload statePayload = ProcessMovement(clientInputBuffer.Get(bufferIndex));
                clientStateBuffer.Add(statePayload, bufferIndex);
                tickToReplay++;
            }
        }

        StatePayload ProcessMovement(InputPayload input) {
            Move(input.inputVector);
            
            return new StatePayload() {
                tick = input.tick,
                networkObjectId = NetworkObjectId,
                position = transform.position,
                rotation = transform.rotation,
                velocity = rb.velocity,
                angularVelocity = rb.angularVelocity
            };
        }

        public void Move(Vector2 inputVector)
        {
            rb.velocity = inputVector * Time.deltaTime * 100f;
        }
    }
}



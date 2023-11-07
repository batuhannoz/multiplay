using System;
using System.Collections;
using System.Threading.Tasks;
using Matchplay.Networking;
using Matchplay.Shared;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

namespace Matchplay.Client.UI
{
    public class MainMenuUI : MonoBehaviour
    {
        [SerializeField] public NetworkManager networkManager;
        [SerializeField] public TMP_InputField tm;
        [SerializeField] public TMP_InputField tmRelay;
        ClientGameManager gameManager;
        AuthState m_AuthState;

        async void Start()
        {
            if (ClientSingleton.Instance == null)
                return;
            
            SetUpInitialState();
            //Default mode is Matchmaker
            // SetMatchmakerMode();

            m_AuthState = await AuthenticationWrapper.Authenticating();
            
            if (m_AuthState == AuthState.Authenticated)
                Debug.Log("Authenticated!");
            else
            {
                Debug.Log(
                    "Error Authenticating: Check the Console for more details.\n" +
                    "(Did you remember to link the editor with the Unity cloud Project?)");
            }
        }
        
        public void PlayButtonPressed()
        {
            {
#pragma warning disable 4014
                gameManager.MatchmakeAsync();
#pragma warning restore 4014
            }
        }
        
        private void SetUpInitialState()
        {
            gameManager = ClientSingleton.Instance.Manager;

            SetName(gameManager.User.Name);
            gameManager.User.onNameChanged += SetName;
            gameManager.NetworkClient.OnLocalConnection += OnConnectionChanged;
            gameManager.NetworkClient.OnLocalDisconnection += OnConnectionChanged;
            
            gameManager.SetGameMode(GameMode.Normal);
            gameManager.SetGameMap(Map.Normal);
            gameManager.SetGameQueue(GameQueue.Normal);
        }
        
        void SetName(string newName)
        {
            // TODO 
        }
        
        void OnConnectionChanged(ConnectStatus status)
        {
            // TODO
        }

   
        
        public async void JoinLobby()
        {
            try
            {
                await LobbyService.Instance.JoinLobbyByCodeAsync(tm.text);
                Debug.Log("Successfully joined lobby");
            }
            catch (LobbyServiceException e)
            {
                Debug.Log(e);
            }
        }

        public async void CreateLobby()
        {
            string lobbyName = "new lobby";
            int maxPlayers = 4;
            CreateLobbyOptions options = new CreateLobbyOptions();
            options.IsPrivate = true;

            Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, options);
            Debug.Log(lobby.LobbyCode);
        }
        
        const int m_MaxConnections = 4;
        public string RelayJoinCode;

        public static async Task<RelayServerData> CreateRelay(int maxConnections, string region = null)
        {
            Allocation allocation;
            string createJoinCode;
            try
            {
                allocation = await RelayService.Instance.CreateAllocationAsync(maxConnections, region);
            }
            catch (Exception e)
            {
                Debug.LogError($"Relay create allocation request failed {e.Message}");
                throw;
            }
            

            Debug.Log($"server: {allocation.ConnectionData[0]} {allocation.ConnectionData[1]}");
            Debug.Log($"server: {allocation.AllocationId}");

            try
            {
                createJoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
                Debug.Log("relay join code: " + createJoinCode);
            }
            catch
            {
                Debug.LogError("Relay create join code request failed");
                throw;
            }
            return new RelayServerData(allocation, "dtls");
        }
        
        IEnumerator CreateRelayOrig()
        {
            Debug.Log("start1");
            var serverRelayUtilityTask = CreateRelay(m_MaxConnections);
            Debug.Log("start2");

            while (!serverRelayUtilityTask.IsCompleted)
            {
                Debug.Log("start3");

                yield return null;
            }
            if (serverRelayUtilityTask.IsFaulted)
            {
                Debug.LogError("Exception thrown when attempting to start Relay Server. Server not started. Exception: " + serverRelayUtilityTask.Exception.Message);
                yield break;
            }

            var relayServerData = serverRelayUtilityTask.Result;

            // Display the joinCode to the user.

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);
            NetworkManager.Singleton.StartHost();
            yield return null;
        }
        
        public void CreateRelayUI()
        {
            var serverRelayUtilityTask = CreateRelay(m_MaxConnections);
            Debug.Log("wtf aq");
            while (!serverRelayUtilityTask.IsCompleted)
            {
                Debug.Log("(!serverRelayUtilityTask.IsCompleted");
            }
            if (serverRelayUtilityTask.IsFaulted)
            {
                Debug.LogError("Exception thrown when attempting to start Relay Server. Server not started. Exception: " + serverRelayUtilityTask.Exception.Message);
                
            }              
            Debug.Log("conntinue");
            //Debug.Log(serverRelayUtilityTask);
            //Debug.Log(serverRelayUtilityTask.Result);

            //var relayServerData = serverRelayUtilityTask.Result;

            // Display the joinCode to the user.

            //NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);
            //NetworkManager.Singleton.StartHost();
        }
        
        public void JoinRelayUI()
        {
            Debug.Log(tmRelay.text);
            JoinRelayServerFromJoinCode(tmRelay.text);
        }

        
        public static async Task<RelayServerData> JoinRelayServerFromJoinCode(string joinCode)
        {
            JoinAllocation allocation;
            try
            {
                allocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
            }
            catch
            {
                Debug.LogError("Relay create join code request failed");
                throw;
            }

            Debug.Log($"client: {allocation.ConnectionData[0]} {allocation.ConnectionData[1]}");
            Debug.Log($"host: {allocation.HostConnectionData[0]} {allocation.HostConnectionData[1]}");
            Debug.Log($"client: {allocation.AllocationId}");

            return new RelayServerData(allocation, "dtls");
        }
        
        IEnumerator Example_ConfigureTransportAndStartNgoAsConnectingPlayer()
        {
            // Populate RelayJoinCode beforehand through the UI
            var clientRelayUtilityTask = JoinRelayServerFromJoinCode(RelayJoinCode);

            while (!clientRelayUtilityTask.IsCompleted)
            {
                yield return null;
            }

            if (clientRelayUtilityTask.IsFaulted)
            {
                Debug.LogError("Exception thrown when attempting to connect to Relay Server. Exception: " + clientRelayUtilityTask.Exception.Message);
                yield break;
            }

            var relayServerData = clientRelayUtilityTask.Result;

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

            NetworkManager.Singleton.StartClient();
            yield return null;
        }
        
    }
}
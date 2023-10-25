using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Core;
using Unity.Services.Multiplay;
using Unity.Services.Matchmaker.Models;
using UnityEngine;
using Newtonsoft.Json;
using System.Threading;
using Unity.Services.Matchmaker;
using UnityEditor.Networking.PlayerConnection;
using Unity.Collections.LowLevel.Unsafe;

public class ServerStartUp : MonoBehaviour
{
    public static event System.Action ClientInstance;
    private string internalServerIP = "0.0.0.0";
    private string externalServerIP = "0.0.0.0";
    private ushort serverPort = 9000;
    private string externalConnectionString => $"{externalServerIP}:{serverPort}";
    private bool Server = false;
    private IMultiplayService multiplayService;
    private const int multiplayServiceTimeout = 20000;

    private string allocationID;
    private MultiplayEventCallbacks serverCallbacks;
    private IServerEvents serverEvents;
    
    private BackfillTicket localBackfillTicket;
    private CreateBackfillTicketOptions createBackfillTicketOptions;
    private MatchmakingResults matchmakingPayload;
    private const int ticketCheckMS = 1000;
    private bool backfilling = false;

    async void Start() {
        var args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++) {
            if (args[i] == "-dedicatedServer") Server = true;
            if (args[i] == "-port" && (i + 1 < args.Length)) {
                serverPort = (ushort)int.Parse(args[i + 1]);
            }
            if (args[i] == "-ip" && (i + 1 < args.Length)) {
                externalServerIP = args[i + 1] ;
            }
        }
        if (Server) {
            StartServer();
            await StartServerServices();
        } else {
            ClientInstance.Invoke();
        }
    }

    private void StartServer() {
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetConnectionData(internalServerIP, (ushort)serverPort);
        NetworkManager.Singleton.StartServer();
        NetworkManager.Singleton.OnClientDisconnectCallback += ClientDisconected;
    }

    async Task StartServerServices() {
        await UnityServices.InitializeAsync();
        try {
            multiplayService = MultiplayService.Instance;
            await multiplayService.StartServerQueryHandlerAsync(2, "n/a", "n/a", "0", "n/a");
        } catch (Exception ex) {
            Debug.LogWarning(ex);
        }

        try {
            matchmakingPayload = await GetMatchmakerPayload(multiplayServiceTimeout);
            if (matchmakingPayload != null) {
                Debug.Log($"Got Payload: {matchmakingPayload}");
                await StartBackfill(matchmakingPayload);
            } else {
                Debug.LogWarning("Matchmaking Payload Timed Out.");
            }
        }
        catch (Exception ex) {
            Debug.LogWarning(ex);
        }
    }

    private async Task<MatchmakingResults> GetMatchmakerPayload(int timeout) {
        var matchmakerPayloadTask = SubscribeAndMatchmakerAllocation();

        if (await Task.WhenAny(matchmakerPayloadTask, Task.Delay(timeout)) == matchmakerPayloadTask) {
            return matchmakerPayloadTask.Result;
        }
        return null;
    }

    private async Task<MatchmakingResults> SubscribeAndMatchmakerAllocation() {
        if (multiplayService ==  null) return null;    
        allocationID = null;
        serverCallbacks = new MultiplayEventCallbacks();
        serverCallbacks.Allocate += OnMultiplayAllocation;
        serverEvents = await multiplayService.SubscribeToServerEventsAsync(serverCallbacks);

        allocationID = await AwaitAllocationID();
        var mmPayload = await GetMatchmakerAllocationPayloadAsync();
        return mmPayload;
    }

    private void OnMultiplayAllocation(MultiplayAllocation allocation) {
        Debug.Log($"OnAllocation: {allocation.AllocationId}");
        if (string.IsNullOrEmpty(allocation.AllocationId)) return;
        allocationID = allocation.AllocationId;
    }

    private async Task<string> AwaitAllocationID() {
         var config = multiplayService.ServerConfig;
            Debug.Log($"Awaiting Allocation. Server Config is:\n" +
                $"-ServerID: {config.ServerId}\n" +
                $"-AllocationID: {config.AllocationId}\n" +
                $"-Port: {config.Port}\n" +
                $"-QPort: {config.QueryPort}\n" +
                $"-logs: {config.ServerLogDirectory}");

            //Waiting on OnMultiplayAllocation() event (Probably wont ever happen in a matchmaker scenario)
            while (string.IsNullOrEmpty(allocationID))
            {
                var configID = config.AllocationId;

                if (!string.IsNullOrEmpty(configID) && string.IsNullOrEmpty(allocationID))
                {
                    Debug.Log($"Config had AllocationID: {configID}");
                    allocationID = configID;
                }

                await Task.Delay(100);
            }

            return allocationID;
    }

    private async Task<MatchmakingResults> GetMatchmakerAllocationPayloadAsync() {
        var payloadAllocation = await MultiplayService.Instance.GetPayloadAllocationFromJsonAs<MatchmakingResults>();
        var modelAsJson = JsonConvert.SerializeObject(payloadAllocation, Formatting.Indented);
        Debug.Log(nameof(GetMatchmakerAllocationPayloadAsync) + ":" + Environment.NewLine + modelAsJson);
        return payloadAllocation;
    }

    private async Task StartBackfill(MatchmakingResults payload) {
        var backfillProperties = new BackfillTicketProperties(payload.MatchProperties);
        localBackfillTicket = new BackfillTicket {
            Id = payload.MatchProperties.BackfillTicketId,
            Properties = backfillProperties 
        };
        await BeginBackfilling(payload);    
    }

    private async Task BeginBackfilling(MatchmakingResults payload) {
        var matchProperties = payload.MatchProperties;
        

        if (string.IsNullOrEmpty(localBackfillTicket.Id)) {
            createBackfillTicketOptions = new CreateBackfillTicketOptions {
            Connection = externalConnectionString,
            QueueName = payload.QueueName,
            Properties = new BackfillTicketProperties(matchProperties)
            };

            localBackfillTicket.Id = await MatchmakerService.Instance.CreateBackfillTicketAsync(createBackfillTicketOptions);
        }
        backfilling = true;
        BackfillLoop();
    }

    private async Task BackfillLoop() {
        while (backfilling && NeedsPlayers()) {
            localBackfillTicket = await MatchmakerService.Instance.ApproveBackfillTicketAsync(localBackfillTicket.Id);
            if (!NeedsPlayers()) {
                await MatchmakerService.Instance.DeleteTicketAsync(localBackfillTicket.Id);
                localBackfillTicket.Id = null;
                backfilling = false;
                return;
            }
            await Task.Delay(ticketCheckMS);
        }
        backfilling = false;
    }

    private bool NeedsPlayers() {
        // TODO change 2 to another const depends on the game
        return NetworkManager.Singleton.ConnectedClients.Count < 2;
    }

    private void Dispose() {
        serverCallbacks.Allocate -= OnMultiplayAllocation;
        serverEvents?.UnsubscribeAsync();
    }

    private void ClientDisconected(ulong clientID) {
        if (!backfilling && NetworkManager.Singleton.ConnectedClients.Count > 0 && NeedsPlayers()) {
            BeginBackfilling(matchmakingPayload);
        }
    }
}

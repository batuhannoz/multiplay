using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;
using Unity.Services.Matchmaker.Models;
using StatusOptions = Unity.Services.Matchmaker.Models.MultiplayAssignment.StatusOptions;
using System.Net.Http.Headers;
using Unity.Services.Matchmaker;
using System.Collections.Generic;
using System;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

#if UNITY_EDITOR
using ParrelSync;
#endif

public class MatchmakerClient : MonoBehaviour
{
    private string ticketID;
    private void OnEnable() {
        ServerStartUp.ClientInstance += SignIn;
    }

    private void OnDisable() {
        ServerStartUp.ClientInstance -= SignIn;
    }

    private async void SignIn() {
        await ClientSignIn("Player");
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    private async Task ClientSignIn(string serviceProfileName = null) {

        if (serviceProfileName != null) {
            #if UNITY_EDITOR
            serviceProfileName = $"{serviceProfileName}{GetCloneNumberSuffix()}";
            #endif
            var initOptions = new InitializationOptions();
            initOptions.SetProfile(serviceProfileName);
            await UnityServices.InitializeAsync(initOptions);
        } else {
            await UnityServices.InitializeAsync();
        }

        Debug.Log($"{serviceProfileName}{PlayerID()}");
    }

    private string PlayerID() {
        return AuthenticationService.Instance.PlayerId;
    }

    #if UNITY_EDITOR
    private string GetCloneNumberSuffix() {
        string projectPath = ClonesManager.GetCurrentProjectPath();
        int lastUnderscore = projectPath.LastIndexOf("_");
        string projectCloneSuffix = projectPath.Substring(lastUnderscore + 1);
        if (projectCloneSuffix.Length != 1) {
            projectCloneSuffix = "";
        }
        return projectCloneSuffix;
    }   
    #endif

    public void StartClient() {
        CreateATicket();
    }

    private async void CreateATicket() { 
        var options = new CreateTicketOptions("sample-queue");
        var players = new List<Player> {
            new Player(
                PlayerID(),
                new MatchmakingPlayerData {
                Skill = 100
                }
            )
        };
        var ticketResponse = await MatchmakerService.Instance.CreateTicketAsync(players, options);
        ticketID = ticketResponse.Id;
        Debug.Log(ticketID);
        PollTicketStatus();
    }
    
    private async void PollTicketStatus() {
        MultiplayAssignment multiplayAssignment = null;
        bool gotAssigment = false;
        do {
            await Task.Delay(TimeSpan.FromSeconds(1f));
            var ticketStatus = await MatchmakerService.Instance.GetTicketAsync(ticketID);
            if (ticketStatus == null) continue;
            if (ticketStatus.Type == typeof(MultiplayAssignment)) {
                multiplayAssignment = ticketStatus.Value as MultiplayAssignment;
            }

            switch (multiplayAssignment.Status) {
                case StatusOptions.Found:
                    gotAssigment = true;
                    TicketAssigned(multiplayAssignment);
                    break;
                case StatusOptions.InProgress:
                    break;
                case StatusOptions.Failed:
                    gotAssigment = true;
                    Debug.Log(multiplayAssignment.Message);
                    break;
                case StatusOptions.Timeout:    
                    gotAssigment = true;
                    Debug.Log("Ticket timed out");
                    break;
                default:
                    throw new InvalidOperationException();
            }
        } while (!gotAssigment);
    }

    private void TicketAssigned(MultiplayAssignment assignment) {
        Debug.Log($"Ticket Assigned: {assignment.Ip}:{assignment.Port}");
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetConnectionData(assignment.Ip, (ushort)assignment.Port);
        NetworkManager.Singleton.StartClient();
    }

    [Serializable]
    public class MatchmakingPlayerData {
        public int Skill;
    }
}

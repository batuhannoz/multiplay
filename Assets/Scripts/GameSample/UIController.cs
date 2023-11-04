using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using TMPro;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;

public class UIController : MonoBehaviour
{
    [SerializeField] public NetworkManager networkManager;
    [SerializeField] public TMP_InputField tm;
    
    public void Start()
    {
        initUnityServices();
    }

    public async void initUnityServices()
    {
        await CreateServer();
        await AuthenticationService.Instance.SignInAnonymouslyAsync();   
    }
    
    public async Task CreateServer()
    {
        await UnityServices.InitializeAsync();
    }
    
    public void StartClient()
    {
        networkManager.StartClient();   
    }
    
    public void StartHost()
    {
        networkManager.StartHost();
    }
    
    
    public void StartServer()
    {
        networkManager.StartServer();
    }

    public async void JoinLobby()
    {
        try
        {
            await LobbyService.Instance.JoinLobbyByCodeAsync(tm.text);
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
}

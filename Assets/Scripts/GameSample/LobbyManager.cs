using System;
using Matchplay.Client;
using Unity.Services.Authentication;
using UnityEngine;

public class LobbyManager : MonoBehaviour
{
    AuthState m_AuthState;

    public async void Start()
    { 
        await AuthenticationService.Instance.SignInAnonymouslyAsync();   
    }
}

using Matchplay.Networking;
using Matchplay.Shared;
using UnityEngine;

namespace Matchplay.Client.UI
{
    public class MainMenuUI : MonoBehaviour
    {
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
    }
}
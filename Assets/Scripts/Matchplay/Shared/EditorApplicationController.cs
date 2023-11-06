#if UNITY_EDITOR
using ParrelSync;
#endif
using Matchplay.Shared;
using UnityEngine;

using Random = System.Random;

namespace Matchplay.Editor
{
    ///Helps launch ParrelSynced Projects for easy testing
    public class EditorApplicationController : MonoBehaviour
    {
        public ApplicationController m_Controller;


        public void Start()
        {
#if UNITY_EDITOR

            if (ClonesManager.IsClone())
            {
                var argument = ClonesManager.GetArgument();
                if (argument == "server")
                    m_Controller.OnParrelSyncStarted(true,"server");
                else if (argument == "client")
                {
                    m_Controller.OnParrelSyncStarted(false,"client");
                }
            }
            else
            {
                Random random = new Random();
                int randomNumber = random.Next(100, 1000);
                m_Controller.OnParrelSyncStarted(false, "client" + randomNumber);
            }
#endif
        }
    }
}
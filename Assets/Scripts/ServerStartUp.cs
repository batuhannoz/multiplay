using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

public class ServerStartUp : MonoBehaviour
{
    private string InternalServerIP = "0.0.0.0";
    private ushort Port = 9000;
    private bool Server = false;
    void Start() {
        var args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++) {
            if (args[i] == "-dedicatedServer") Server = true;
            if (args[i] == "-port" && (i + 1 < args.Length)) {
                Port = (ushort)int.Parse(args[i + 1]);
            }

        }
        if (Server) {
            StartServer();
        }
    }

    private void StartServer() {
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetConnectionData(InternalServerIP, (ushort)Port);
        NetworkManager.Singleton.StartServer();
    }
}

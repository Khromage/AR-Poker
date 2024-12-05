using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Fusion;
using Fusion.Sockets;

public class MultiPlayerGameManager : MonoBehaviour
{
    [SerializeField] private NetworkRunner networkRunner;
    

    public string GeneratedCode { get; private set; } // Holds the generated lobby code

    public void Initialize()
    {
        Debug.Log("Multi Player Manager Initialized");
        // Automatically generate a 4-letter code for the lobby
        GeneratedCode = GenerateLobbyCode();
        Debug.Log($"Lobby Code: {GeneratedCode}");
    }

    private string GenerateLobbyCode()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        char[] stringChars = new char[4];
        System.Random random = new System.Random();

        for (int i = 0; i < stringChars.Length; i++)
        {
            stringChars[i] = chars[random.Next(chars.Length)];
        }

        return new string(stringChars);
    }

    public async void StartHost()
    {
        if (networkRunner == null)
        {
            Debug.LogError("NetworkRunner not assigned in Inspector!");
            return;
        }

        Debug.Log("Starting Host...");
        var result = await networkRunner.StartGame(new StartGameArgs()
        {
            GameMode = GameMode.Host,
            SessionName = GeneratedCode,
            //Scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex,
            Scene = null,
            PlayerCount = 4 // Maximum players, not inlcuidng host
        });

        if (result.Ok)
        {
            Debug.Log("Host started successfully!");
        }
        else
        {
            Debug.LogError($"Failed to start Host: {result.ShutdownReason}");
        }
    }

    public async void JoinRoom(string lobbyCode)
    {
        if (networkRunner == null)
        {
            Debug.LogError("NetworkRunner not assigned in Inspector!");
            return;
        }

        Debug.Log($"Joining Lobby: {lobbyCode}");
        var result = await networkRunner.StartGame(new StartGameArgs()
        {
            GameMode = GameMode.Client,
            SessionName = lobbyCode
        });

        if (result.Ok)
        {
            Debug.Log("Joined lobby successfully!");
        }
        else
        {
            Debug.LogError($"Failed to join lobby: {result.ShutdownReason}");
        }
    }
}

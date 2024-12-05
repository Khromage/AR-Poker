using UnityEngine;
using Fusion;
using Fusion.Sockets;

public class MultiPlayerGameManager : MonoBehaviour
{
    [SerializeField] private NetworkRunner networkRunnerPrefab;

    public string GeneratedCode { get; private set; } // Holds the generated lobby code
    private NetworkRunner runner;

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
        if (networkRunnerPrefab == null)
        {
            Debug.LogError("NetworkRunner prefab not assigned in Inspector!");
            return;
        }

        // Instantiate the runner
        runner = Instantiate(networkRunnerPrefab);
        var startGameArgs = new StartGameArgs()
        {
            GameMode = GameMode.Host,
            SessionName = GeneratedCode,
            Scene = null, // We assume you're already in the gameplay scene
            PlayerCount = 4
        };

        Debug.Log("Starting Host...");
        var result = await runner.StartGame(startGameArgs);

        if (result.Ok)
        {
            Debug.Log("Host started successfully! Session Code: " + GeneratedCode);
        }
        else
        {
            Debug.LogError($"Failed to start Host: {result.ShutdownReason}");
        }
    }

    public async void JoinRoom(string lobbyCode)
    {
        if (networkRunnerPrefab == null)
        {
            Debug.LogError("NetworkRunner prefab not assigned in Inspector!");
            return;
        }

        // Instantiate the runner
        runner = Instantiate(networkRunnerPrefab);
        var startGameArgs = new StartGameArgs()
        {
            GameMode = GameMode.Client,
            SessionName = lobbyCode
        };

        Debug.Log($"Joining Lobby: {lobbyCode}");
        var result = await runner.StartGame(startGameArgs);

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

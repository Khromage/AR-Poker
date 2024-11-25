using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;

public class GameManager : MonoBehaviour
{
    // Singleton instance for easy access
    public static GameManager Instance { get; private set; }


    // GameMode Prefabs
    public GameObject SinglePlayerPrefab;
    public GameObject MultiPlayerPrefab;

    // Holds current Single/Multi Player game
    [SerializeField]
    protected GameObject CurrentGame;

    private void Awake()
    {
        // Implement Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Optional: Persist across scenes
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    // Start is called before the first frame update
    void Start()
    {
        
        
        // If UI NOT already open, load scene 
        if(SceneManager.GetSceneByBuildIndex(1).isLoaded == false)
        {
            Debug.Log("UI Layer Scene not detected. Loading UI");
            SceneManager.LoadScene(1, LoadSceneMode.Additive);
        }
        // Set Gameplay Scene as active scene 
        SceneManager.SetActiveScene(SceneManager.GetSceneByBuildIndex(0));

        

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnEnable()
    {
        MenuManager.OnGameStart += HandleGameStart;

    }

    private void OnDisable()
    {
        MenuManager.OnGameStart -= HandleGameStart;
    }

    // Method to start the Game
    private void HandleGameStart(object sender, GameStartEventData e)
    {
        if (CurrentGame == null)
        {
            Debug.Log($"Game Starting! Mode: {e.GameMode}, Difficulty: {e.Difficulty}, NPCs: {e.NPCCount}");
        
            switch (e.GameMode)
            {
                case "Single":
                    GameObject SinglePlayerGame = Instantiate(SinglePlayerPrefab, transform.position, transform.rotation);
                    SinglePlayerGame.GetComponent<SinglePlayerGameManager>().Initialize(e.Difficulty, e.NPCCount);
                    CurrentGame = SinglePlayerGame;
                    
                    break;
                case "Multi":
                    GameObject MultiPlayerGame = Instantiate(MultiPlayerPrefab, transform.position, transform.rotation);
                    MultiPlayerGame.GetComponent<MultiPlayerGameManager>().Initialize();
                    CurrentGame = MultiPlayerGame;
                    break;
            }
        } else 
        {
            Debug.Log($"ERROR ~ CurrentGame already Exists: {CurrentGame}");
        }

    }

    public void EndCurrentGame()
    {
        if (CurrentGame != null)
        {
            Destroy(CurrentGame);
            CurrentGame = null;
            // Notify any necessary components about the game ending (optional)
        }
        else
        {
            Debug.Log("No active game to end.");
        }
    }
    
}

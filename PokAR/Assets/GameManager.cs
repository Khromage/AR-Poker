using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;

public class GameManager : MonoBehaviour
{
    


    // Singleton instance for easy access
    public static GameManager Instance { get; private set; }

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

    private void HandleGameStart(object sender, GameStartEventData e)
    {
        Debug.Log($"Game Started! Mode: {e.GameMode}, Difficulty: {e.Difficulty}, NPCs: {e.NPCCount}");
        


    }

    // Method to start the Game
    public void StartGame(string gameMode, string difficulty, int npcCount)
    {
        Debug.Log("Game is starting...");
        

    }

    
}

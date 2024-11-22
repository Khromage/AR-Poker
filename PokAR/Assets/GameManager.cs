using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;

public class GameManager : MonoBehaviour
{
    
    // Define the delegate for the event
    public event EventHandler<GameStartEventData> OnGameStart;

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

    

    // Method to start the Game
    public void StartGame(string gameMode, string difficulty, int npcCount)
    {
        Debug.Log("Game is starting...");
        
        // Create event data
        GameStartEventData eventData = new GameStartEventData(gameMode, difficulty, npcCount);
        
        // Invoke the event
        OnGameStart?.Invoke(this, eventData);
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    
}

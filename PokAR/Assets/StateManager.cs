using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StateManager : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        if(SceneManager.GetSceneByBuildIndex(1).isLoaded == false)
        {
            Debug.Log("UI Layer Scene not detected. Loading UI");
            SceneManager.LoadScene(1, LoadSceneMode.Additive);
        }
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

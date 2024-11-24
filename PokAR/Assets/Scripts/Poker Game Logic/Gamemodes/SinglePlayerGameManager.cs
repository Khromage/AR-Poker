using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SinglePlayerGameManager : MonoBehaviour
{
    [SerializeField]
    private string difficulty;
    [SerializeField]
    private int numNPC;

    public void Initialize(string gameDifficulty, int npcCount)
    {
        difficulty = gameDifficulty;
        numNPC = npcCount;
        Debug.Log("Single Player Manager PARAMS were INITIALIZED");
    }


    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("Single Player Manager WAS CREATED & START RAN");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class sampleSO : MonoBehaviour
{

    public PlayingCards playingCards;
    // Start is called before the first frame update
    void Start()
    {
        Material cardFaceToDraw = playingCards.Suits[3].Faces[1];

        transform.GetChild(0).GetComponent<Renderer>().material = cardFaceToDraw;

        //cardFace = cardFaceToDraw;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

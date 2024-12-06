using System;
using UnityEngine;

[CreateAssetMenu(fileName = "Data", menuName = "PokAR/PlayingCardsSO", order = 1)]

public class PlayingCards : ScriptableObject
{
    public GameObject Prefab;
    public Material[] Backs;
    public Suit[] Suits;
}

[Serializable]
public struct Suit
{
    public string Name;
    public Material[] Faces;
}
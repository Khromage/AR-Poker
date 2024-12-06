using System;
using UnityEngine;

[CreateAssetMenu(fileName = "Data", menuName = "PokAR/CasinoChipsSO", order = 2)]
public class CasinoChips : ScriptableObject
{
    public GameObject Prefab;
    public Chip[] Chips;
}

[Serializable]
public struct Chip
{
    public int value;
    public Material material;
}
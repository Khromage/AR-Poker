using System;
using UnityEngine;

[CreateAssetMenu(fileName = "Data", menuName = "PokAR/PlayerAvatarsSO", order = 1)]

public class PlayerAvatars : ScriptableObject
{
    public GameObject[] Prefab;
}


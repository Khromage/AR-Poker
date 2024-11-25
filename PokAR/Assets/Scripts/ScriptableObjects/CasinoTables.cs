using System;
using UnityEngine;

[CreateAssetMenu(fileName = "Data", menuName = "PokAR/CasinoTablesSO", order = 3)]
public class CasinoTables : ScriptableObject
{
    public Table[] Tables;
}

[Serializable]
public struct Table
{
    public GameObject Prefab;
    public string name;
}
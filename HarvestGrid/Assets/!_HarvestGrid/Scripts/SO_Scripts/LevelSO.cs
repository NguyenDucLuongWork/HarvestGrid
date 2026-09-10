using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LevelSO", menuName = "Scriptable Objects/LevelSO")]
public class LevelSO : ScriptableObject
{
    public string levelID;
    public FarmMono farmMono;
    [SerializeField]
    public Dictionary<ItemPrototype, float> itemAndChancePool;

    public StoringSpaceSO storingSpaceSO;



}



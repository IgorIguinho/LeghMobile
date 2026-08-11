using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct WaveInfo
{
    public List<GameObject> enemyPrefabs; // which enemies can spawn in this wave
    public float spawnInterval;           // time between spawns
    public int totalEnemiesToSpawn;       // count of enemies in this wave
}

[CreateAssetMenu(fileName = "NewFase7WaveConfig", menuName = "ScriptableObjects/Fase7WaveConfig")]
public class Fase7WaveConfig : ScriptableObject
{
    public List<WaveInfo> waves;
    public int totalEnemiesToSpawnInAllWaves;
}
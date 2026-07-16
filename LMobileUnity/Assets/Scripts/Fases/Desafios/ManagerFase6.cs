using System;
using System.Collections.Generic;
using UnityEngine;

public class ManagerFase6 : MonoBehaviour
{
    public static ManagerFase6 Instance { get; private set; }

    public List<LevelFase6> levelFase6List;
    public List<TerrainFase6> terrainFase6List;
    public int currentLevelIndex = 0;

    public DialogueData dialogue;
    PlayerInteract player;

    public void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        foreach(var level in levelFase6List)
        {
            level.Start();
        }

        levelFase6List[currentLevelIndex].Initialize();
    }

    // Update is called once per frame
    void Update()
    {
      
    }

    public void RegisterHitAlvo(GameObject alvo)
    {
        LevelFase6 faseActual = levelFase6List[currentLevelIndex];
        if (faseActual.obstaculosAtivos.Contains(alvo))
        {
            faseActual.obstaculosAtivos.Remove(alvo);
            faseActual.obstaculosInativos.Add(alvo);

            alvo.SetActive(false);

            if (faseActual.CheckIfAllObstaculosDestroyed())
            {
                currentLevelIndex++;
                if (currentLevelIndex < levelFase6List.Count)
                {
                    levelFase6List[currentLevelIndex].Initialize();

                    //Disable the terrain of the previous level
                    foreach (var terrain in terrainFase6List)
                    {
                        if (terrain.indexDisable == currentLevelIndex)
                        {
                            terrain.terrainGrid.SetActive(false);
                        }
                    }
                }
                else
                {
                    if (dialogue == null) return;
                    player = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerInteract>();
                    player.CanOpenDialogue(false, dialogue);
                    player.OpenDialogue();
                    return;
                }
            }
        }
    }
}

[Serializable]
public class LevelFase6
{
    public GameObject levelGrid;
    public List<GameObject> obstaculosList;
    public List<GameObject> obstaculosAtivos;
    public List<GameObject> obstaculosInativos;
    public bool isFinish;

    public void Start()
    {
        obstaculosAtivos = new List<GameObject>();
        obstaculosInativos = new List<GameObject>();
        foreach (var obstaculo in obstaculosList)
        {
            obstaculo.SetActive(false);
            obstaculosInativos.Add(obstaculo);
        }
    }

    public void Initialize()
    {
        foreach (var obstaculo in obstaculosList)
        {
            obstaculo.SetActive(true);
            obstaculosAtivos.Add(obstaculo);
            obstaculosInativos.Remove(obstaculo);

        }
    }

    public bool CheckIfAllObstaculosDestroyed()
    {
        foreach (var obstaculo in obstaculosAtivos)
        {
            if (obstaculo != null && obstaculo.activeInHierarchy)
            {
                return false;
            }
        }
        isFinish = true;
        return true;
    }

}

[Serializable]
public class TerrainFase6 
{
    public GameObject terrainGrid;
    public int indexDisable;
}

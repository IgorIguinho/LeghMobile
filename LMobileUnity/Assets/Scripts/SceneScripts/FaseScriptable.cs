using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu (fileName = "NewSceneScriptable", menuName = "NewSceneScritable")]
public class FaseScriptable : ScriptableObject
{
    [Header("Coletaveis")]
    public List<int> colectedCoin; //Save
    public int totalCoin;
    [Space(10)]
    public int colectedMedal; //Save
    public int totalMedal;
    [Space(10)]
    public int colectedParti; //Save
    public int totalParti;

    [Header("Seleção de fase")]
    public Sprite imageHolder;
    public string nameFase;
    public string sceneFase;
    public bool finishFase; //Save
}

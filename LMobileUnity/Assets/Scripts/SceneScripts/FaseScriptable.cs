using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu (fileName = "NewSceneScriptable", menuName = "NewSceneScritable")]
public class FaseScriptable : ScriptableObject
{
    [Header("Coletaveis")]
    public int colectedCoin;
    public int totalCoin;
    [Space(10)]
    public int colectedMedal;
    public int totalMedal;
    [Space(10)]
    public int colectedParti;
    public int totalParti;

    [Header("Seleção de fase")]
    public Sprite imageHolder;
    public string nameFase;
    public string sceneFase;
}

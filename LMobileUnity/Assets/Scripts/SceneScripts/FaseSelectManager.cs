using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class FaseSelectManager : MonoBehaviour
{
    public List<FaseScriptable> faseList;

    [Header("Variaveis UI")]
    public Image imageFaseHolder;
    public TextMeshProUGUI coinTextUI;
    public TextMeshProUGUI medalTextUI;
    public TextMeshProUGUI partiTextUI;

    // Start is called before the first frame update
    void Start()
    {
        UpdateFaseUi(0);   
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void UpdateFaseUi(int i)
    {
        imageFaseHolder.sprite = faseList[i].imageHolder;
        coinTextUI.text = "Moeda " + faseList[i].colectedCoin.ToString() + "/" + faseList[i].totalCoin;
        medalTextUI.text ="Medalhas " + faseList[i].colectedMedal.ToString() + "/" + faseList[i].totalMedal;
        partiTextUI.text = "Partitura " + faseList[i].colectedParti.ToString() + "/" + faseList[i].totalCoin;
    }

    public void StartFase(int i)
    {
        SceneManager.LoadScene(faseList[i].nameFase);
    }
}

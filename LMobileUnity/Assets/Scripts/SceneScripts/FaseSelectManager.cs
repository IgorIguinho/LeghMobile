using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class FaseSelectManager : MonoBehaviour
{
    public List<FaseScriptable> faseList;

    public int faseAtual = 0;

    [Header("Variaveis UI")]
    public Image imageFaseHolder;
    public TextMeshProUGUI coinTextUI;
    public TextMeshProUGUI medalTextUI;
    public TextMeshProUGUI partiTextUI;

    // Start is called before the first frame update
    void Start()
    {
        UpdateFaseUi(faseAtual);   
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

    public void StartFase()
    {
        SceneManager.LoadScene(faseList[faseAtual].nameFase);
    }

    public void NextFase()
    {
        if (faseAtual < faseList.Count - 1)
        {
            faseAtual++;
            UpdateFaseUi(faseAtual);
        }
    }

    public void PreviousFase()
    {
        if (faseAtual > 0)
        {
            faseAtual--;
            UpdateFaseUi(faseAtual);
        }
    }
}

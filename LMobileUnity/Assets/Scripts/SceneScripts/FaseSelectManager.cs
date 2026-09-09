using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class FaseSelectManager : MonoBehaviour
{
    public static FaseSelectManager Instance { get; private set; }
    public List<FaseScriptable> faseList;

    public int selectedFaseAtual = 0;
    

    [Header("Variaveis UI")]
    public Image imageFaseHolder;
    public TextMeshProUGUI coinTextUI;
    public TextMeshProUGUI medalTextUI;
    public TextMeshProUGUI partiTextUI;
    public TextMeshProUGUI nameTextUI;

    [Header("Variaveis Button")]
    public Button SkillButton;
    public GameObject groupButton;
    private void Awake()
    {
        // If there is an instance, and it's not me, delete myself.

        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }
    }

        // Start is called before the first frame update
    void Start()
    {
       
        UpdateFaseUi(selectedFaseAtual);
        PlayerPrefs.DeleteAll();

        if (FaseProgressSaveManager.Instance.IsFaseFinished(faseList[1].sceneFase))
        {
            SkillButton.interactable = true;
        }
        else {SkillButton.interactable = false; }
        if (FaseProgressSaveManager.Instance.IsFaseFinished(faseList[0].sceneFase))
        {
            groupButton.SetActive(true);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void UpdateFaseUi(int i)
    {
        imageFaseHolder.sprite = faseList[i].imageHolder;
        coinTextUI.text = "Moeda " + faseList[i].colectedCoin.Count.ToString() + "/" + faseList[i].totalCoin;
        medalTextUI.text ="Medalhas " + faseList[i].colectedMedal.ToString() + "/" + faseList[i].totalMedal;
        partiTextUI.text = "Partitura " + faseList[i].colectedParti.ToString() + "/" + faseList[i].totalCoin;
        nameTextUI.text = faseList[i].nameFase;
    }

    public void StartFase()
    {
        SceneManager.LoadScene(faseList[selectedFaseAtual].nameFase);
    }

    public void NextFase()
    {
        if (!FaseProgressSaveManager.Instance.IsFaseFinished(faseList[selectedFaseAtual].sceneFase)) { return; }
        FaseProgressSaveManager.Instance.ApplyToScriptable(faseList[selectedFaseAtual]);
        if (selectedFaseAtual < faseList.Count - 1)
        {
            selectedFaseAtual++;
            UpdateFaseUi(selectedFaseAtual);
        }
    }

    public void PreviousFase()
    {
        if (selectedFaseAtual > 0)
        {
            selectedFaseAtual--;
            UpdateFaseUi(selectedFaseAtual);
        }
    }
}

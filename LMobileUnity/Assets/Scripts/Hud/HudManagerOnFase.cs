using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem.Composites;
using UnityEngine.UI;


public class HudManagerOnFase : MonoBehaviour
{
    public static HudManagerOnFase Instance { get; private set; }

    public TextMeshProUGUI coinCount;
    public TextMeshProUGUI hpCount;

    public GameObject buttonControlGroup;

    [Header("Dialogue")]
    public GameObject groupDialogue;
    public Image dialogueBG;
    public TextMeshProUGUI dialogueText;
    public TextMeshProUGUI dialogueName;
    public GameObject skipDialogueButton;
    [Space(10)]
    public GameObject winScreen;

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

    private void Start()
    {
        if (dialogueBG == null) return;
        dialogueBG.canvasRenderer.SetAlpha(0f);
        winScreen.SetActive(false);
    }

    public void OpenDialogueHud(float open, bool firstTimeReadDialogue)
    {
        dialogueBG.CrossFadeAlpha(open,0.2f,true) ;
        buttonControlGroup.SetActive(open == 0 ? true : false);
      if (open == 0)
        {
            dialogueText.text = "";
            dialogueName.text = "";
        }
        
        skipDialogueButton.SetActive(firstTimeReadDialogue);
        skipDialogueButton.GetComponent<Image>().CrossFadeAlpha(open, 0.2f, true);
    }
    public void OpenWinScreen()
    {
        winScreen.SetActive(true);
        EndFase();
    }

    void EndFase()
    {
        PlayerPrefs.DeleteAll();
        FaseManager.Instance.UptadeFaseScritable();
        Time.timeScale = 0f;
    }
}

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
    public Image dialogueBG;
    public TextMeshProUGUI dialogueText;
    public TextMeshProUGUI dialogueName;

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
        dialogueBG.canvasRenderer.SetAlpha(0f);
    }

    public void OpenDialogueHud(float open)
    {
        dialogueBG.CrossFadeAlpha(open,0.2f,true) ;
        buttonControlGroup.SetActive(open == 0 ? true : false);
      dialogueText.text = "";
        dialogueName.text = "";
    }
}

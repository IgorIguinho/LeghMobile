using TMPro;
using Unity.VisualScripting;
using UnityEngine;

using UnityEngine.UI;



public enum DialogueState
{
    Disable,
    Typing,
    Waiting
}
public class DialogueSystem : MonoBehaviour
{
  [SerializeField]  DialogueState currentState = DialogueState.Disable;
    TypeTextAnimation typeText;

    public DialogueData dialogueData;
    public int currentIndex;
    public bool isFinished = false;

    [Header("UI")]
    public TextMeshProUGUI nameText;
    public Image portraitImage;
    private Sprite nullImage;
    private void OnEnable()
    {
        InputReader.Instance.InteractDialogueTriggered += OnInteractDialogue;
    }

    private void OnDisable()
    {
        InputReader.Instance.InteractDialogueTriggered -= OnInteractDialogue;
    }


    private void Awake()
    {
        typeText = GetComponent<TypeTextAnimation>();

        typeText.TypeFinished += OnTypingFinished; // Subscribe to the TypeFinished event
        
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        currentState = DialogueState.Disable;
        nullImage = Resources.Load<Sprite>("Null");
    }

    // Update is called once per frame


    public void OnInteractDialogue()
    {
        if (currentState == DialogueState.Disable) return;

        switch (currentState)
        {
            case DialogueState.Waiting:
                Waiting();
                break;
            case DialogueState.Typing:
              Typing();
                break;
        }
    }

    public void NextDialogue()
    {
     
        if (isFinished) return;
        portraitImage.gameObject.SetActive(true);
        nameText.text = dialogueData.dialogues[currentIndex].speakerName;
        if (dialogueData.dialogues[currentIndex].speakerPortrait == null)
        {
            portraitImage.sprite = nullImage;
        }
        else 
        { 
            portraitImage.sprite = dialogueData.dialogues[currentIndex].speakerPortrait;
        }
        typeText.fullText = dialogueData.dialogues[currentIndex++].dialogueText;
       
        if (currentIndex == dialogueData.dialogues.Count) isFinished = true;

        typeText.StartTyping();
        currentState = DialogueState.Typing;
    }


    public void Waiting()
    {

            if (!isFinished)
            { NextDialogue();}
            else
            {FinishDialogue();}
    
    }

    void FinishDialogue()
    {
     
        isFinished = false;
        currentIndex = 0;
        portraitImage.gameObject.SetActive(false);
        currentState = DialogueState.Disable; 
        InputReader.Instance.TradeActionMap(InputReader.Instance.controls.Land, InputReader.Instance.controls.Dialogue);
        HudManagerOnFase.Instance.OpenDialogueHud(0f, false);
        if (dialogueData.learnSkill)
        {
            PlayerSkillsManager.Instance.UnlockSkill(dialogueData.skillToLearn);
        }
        if (dialogueData.lastDialogue)
        {
            HudManagerOnFase.Instance.groupDialogue.SetActive(false);
            HudManagerOnFase.Instance.OpenWinScreen();
        }
        if(dialogueData.isBossDialogue)
        {
            FaseManager.Instance.StartBossFight();
        }

        if (dialogueData.triggerLevelStart && Fase7LevelManager.Instance != null)
        {
            Fase7LevelManager.Instance.OnDialogueStartFinished();
        }
    }

    public void FinishDialogueButton()
    {
        isFinished = false;
        currentIndex = 0;
        currentState = DialogueState.Disable;
        portraitImage.gameObject.SetActive(false);
        InputReader.Instance.TradeActionMap(InputReader.Instance.controls.Land, InputReader.Instance.controls.Dialogue);
        HudManagerOnFase.Instance.OpenDialogueHud(0f, false);
        if (dialogueData.lastDialogue)
        {
            HudManagerOnFase.Instance.groupDialogue.SetActive(false);
            HudManagerOnFase.Instance.OpenWinScreen();
        }

        if (dialogueData.triggerLevelStart && Fase7LevelManager.Instance != null)
        {
            Fase7LevelManager.Instance.OnDialogueStartFinished();
        }
    }

   public void Typing()
    {
            typeText.Skip();
            currentState = DialogueState.Waiting;
    }
        

    void OnTypingFinished()
    { currentState = DialogueState.Waiting; }
}


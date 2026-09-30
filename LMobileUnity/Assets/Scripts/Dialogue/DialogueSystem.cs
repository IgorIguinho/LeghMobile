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
    /// <summary>
    /// Evento estático disparado quando um novo diálogo se inicia (primeira fala).
    /// </summary>
    public static event System.Action<DialogueData> OnDialogueStarted;

    /// <summary>
    /// Evento estático disparado quando um diálogo é finalizado ou pulado.
    /// </summary>
    public static event System.Action<DialogueData> OnDialogueFinished;

    /// <summary>
    /// Dispara o evento OnDialogueStarted manualmente se necessário.
    /// </summary>
    public static void NotifyDialogueStarted(DialogueData data)
    {
        OnDialogueStarted?.Invoke(data);
    }

    /// <summary>
    /// Dispara o evento OnDialogueFinished manualmente se necessário.
    /// </summary>
    public static void NotifyDialogueFinished(DialogueData data)
    {
        OnDialogueFinished?.Invoke(data);
    }

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
        portraitImage.gameObject.SetActive(false);
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

        if (currentState == DialogueState.Disable && currentIndex == 0)
        {
            OnDialogueStarted?.Invoke(dialogueData);
        }

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
        DialogueData finishedData = dialogueData;
        isFinished = false;
        currentIndex = 0;
        portraitImage.gameObject.SetActive(false);
        currentState = DialogueState.Disable; 
        InputReader.Instance.TradeActionMap(InputReader.Instance.controls.Land, InputReader.Instance.controls.Dialogue);
        HudManagerOnFase.Instance.OpenDialogueHud(0f, false);
        if (dialogueData.learnSkill)
        {
            PlayerSkillsManager.Instance.UnlockSkill(dialogueData.skillToLearn);
            if (dialogueData.skillToLearn == SkillType.Dash)
                PlayerSkillsManager.Instance.gameObject.GetComponent<PlayerMovements>().UnlockDashButton();
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

        OnDialogueFinished?.Invoke(finishedData);
    }

    public void FinishDialogueButton()
    {
        DialogueData finishedData = dialogueData;
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

        OnDialogueFinished?.Invoke(finishedData);
    }

   public void Typing()
    {
            typeText.Skip();
            currentState = DialogueState.Waiting;
    }
        

    void OnTypingFinished()
    { currentState = DialogueState.Waiting; }
}


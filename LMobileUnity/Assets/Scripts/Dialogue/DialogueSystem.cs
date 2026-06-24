using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.XR;


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
        Debug.Log("NextDialogue" + isFinished);
        if (isFinished) return;
        nameText.text = dialogueData.dialogues[currentIndex].speakerName;
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
        currentState = DialogueState.Disable; 
        InputReader.Instance.TradeActionMap(InputReader.Instance.controls.Land, InputReader.Instance.controls.Dialogue);
        HudManagerOnFase.Instance.OpenDialogueHud(0f);
    }

   public void Typing()
    {
            typeText.Skip();
            currentState = DialogueState.Waiting;
    }
        

    void OnTypingFinished()
    { currentState = DialogueState.Waiting; }
}


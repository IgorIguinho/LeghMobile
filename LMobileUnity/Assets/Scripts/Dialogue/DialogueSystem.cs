using TMPro;
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
    public int currendIndex;
    public bool isFinished = false;

    [Header("UI")]
    public TextMeshProUGUI nameText;

    private void Awake()
    {
        typeText = GetComponent<TypeTextAnimation>();

        typeText.TypeFinished += OnTypingFinished; // Subscribe to the TypeFinished event
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        currentState = DialogueState.Disable;
        NextDialogue();
    }

    // Update is called once per frame
    void Update()
    {
        if (currentState == DialogueState.Disable) return;

        switch (currentState)
        {
            case DialogueState.Waiting:
                //Waiting();
                break;
            case DialogueState.Typing:
                //Typing();
                break;
        }
    }
    void NextDialogue()
    {
        if (isFinished) return;
        nameText.text = dialogueData.dialogues[currendIndex].speakerName;
        typeText.fullText = dialogueData.dialogues[currendIndex++].dialogueText;
       
        if (currendIndex == dialogueData.dialogues.Count) isFinished = true;

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
        currendIndex = 0;
        currentState = DialogueState.Disable; 
    }

   public void Typing()
    {
            typeText.Skip();
            currentState = DialogueState.Waiting;
    }
        

    void OnTypingFinished()
    { currentState = DialogueState.Waiting; }
}


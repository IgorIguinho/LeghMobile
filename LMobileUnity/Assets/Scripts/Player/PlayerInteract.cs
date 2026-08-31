using NUnit.Framework.Interfaces;
using UnityEngine;

public class PlayerInteract : MonoBehaviour
{

    InputReader input;
    GameObject managerUI;
    DialogueData dialogue;

    private void OnDisable()
    {
        if (input != null)
        {
            input.OpenDialogueTriggered -= OpenDialogue;
        }
    }

    private void Awake()
    {
        input = GetComponent<InputReader>();
        managerUI = GameObject.FindGameObjectWithTag("HudManager");

    }

    public void CanOpenDialogue(bool boolOpen, DialogueData _dialogue)
    {
        if (_dialogue == null)
        {
            input.OpenDialogueTriggered -= OpenDialogue;
            return; 
        }
        dialogue = _dialogue;
        if (boolOpen)
        {
            input.OpenDialogueTriggered -= OpenDialogue;
            input.OpenDialogueTriggered += OpenDialogue;
        }
        else
        {
        input.OpenDialogueTriggered -= OpenDialogue;
        }
    
    }

    public void OpenDialogue()
    {
        input.TradeActionMap(input.controls.Dialogue, input.controls.Land);
        managerUI.GetComponent<DialogueSystem>().dialogueData = dialogue;

        managerUI.GetComponent<DialogueSystem>().NextDialogue();
        if (ReadDialogueData.Instance != null)
        {
            managerUI.GetComponent<HudManagerOnFase>().OpenDialogueHud(1, ReadDialogueData.Instance.readDialogues.Contains(dialogue));
            if (!ReadDialogueData.Instance.readDialogues.Contains(dialogue))
            {
                ReadDialogueData.Instance.readDialogues.Add(dialogue);
            }
        }
     
        
        
    }
}

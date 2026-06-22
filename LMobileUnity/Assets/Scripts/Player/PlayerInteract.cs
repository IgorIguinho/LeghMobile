using UnityEngine;

public class PlayerInteract : MonoBehaviour
{

    InputReader input;

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
      
    }

    public void ReloadActionMap(bool boolOpen)
    {
        if (boolOpen)
        {
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
    }
}

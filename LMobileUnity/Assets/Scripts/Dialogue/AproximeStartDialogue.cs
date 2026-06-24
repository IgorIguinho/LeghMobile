using Unity.VisualScripting;
using UnityEngine;

public class AproximeStartDialogue : MonoBehaviour
{
    GameObject player;
    public GameObject pressUI;
    public DialogueData dialogue;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            other.GetComponent<PlayerInteract>().CanOpenDialogue(true, dialogue);
            pressUI.SetActive(true);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            other.GetComponent<PlayerInteract>().CanOpenDialogue(false, null);
            pressUI.SetActive(false);
        }
    }
}

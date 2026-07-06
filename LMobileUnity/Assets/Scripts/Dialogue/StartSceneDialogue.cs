using UnityEngine;

public class StartSceneDialogue : MonoBehaviour
{
    public DialogueData dialogue;
    PlayerInteract player;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (dialogue == null) return;
        player = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerInteract>();
        player.CanOpenDialogue(false, dialogue);
        player.OpenDialogue();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

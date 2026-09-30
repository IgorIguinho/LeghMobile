using UnityEngine;

public class StartSceneDialogue : MonoBehaviour
{
    public DialogueData dialogue;
    PlayerInteract player;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (dialogue == null) return;
        if (PlayerPrefs.GetInt("SecondTimeFase") == 1)
        {
            //Desativa a imagem do retrato e o botão de pular diálogo no início
            HudManagerOnFase.Instance.OpenDialogueHud(0f, false);
            return; 
        }
        player = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerInteract>();
        player.CanOpenDialogue(false, dialogue);
        player.OpenDialogue();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

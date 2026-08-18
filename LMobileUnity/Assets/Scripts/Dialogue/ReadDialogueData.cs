using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class ReadDialogueData : MonoBehaviour
{
    public static ReadDialogueData Instance { get; private set; }

    public List<DialogueData> readDialogues;

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
     
}

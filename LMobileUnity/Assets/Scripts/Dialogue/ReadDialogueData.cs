using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ReadDialogueData : MonoBehaviour
{
    public static ReadDialogueData Instance { get; private set; }

    public List<DialogueData> readDialogues = new List<DialogueData>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }
    }

    // ---------------------------------------------------------------
    // INTEGRAÇÃO COM O SAVE SYSTEM (novo)
    // ---------------------------------------------------------------

    public HashSet<string> readDialogueIds = new HashSet<string>();

    public List<string> GetSaveData() => readDialogueIds.ToList();

    public void LoadSaveData(List<string> data)
    {

        readDialogueIds.Clear();
        readDialogues.Clear();

        if (data == null) return;

        foreach (var id in data)
        {
            readDialogueIds.Add(id);

            var dialogue = DialogueDB.Instance.GetById(id);
            if (dialogue != null)
            {
                readDialogues.Add(dialogue);
            }
            else
            {
                Debug.LogWarning($"[ReadDialogueData] ID '{id}' do save não foi encontrado no DialogueDB. Save pode estar desatualizado ou o asset foi removido/recriado.");
            }
        }

    }

    public bool HasRead(DialogueData d) => readDialogueIds.Contains(d.DialogueId);
    public void MarkAsRead(DialogueData d) => readDialogueIds.Add(d.DialogueId);
}
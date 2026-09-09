// DialogueDB.cs
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class DialogueDB : MonoBehaviour
{
    public static DialogueDB Instance { get; private set; }

    // Dictionary é runtime-only — Unity NÃO serializa Dictionary no Inspector
    // nem no JsonUtility. Por isso ela é preenchida em código (via Awake ou
    // pelo botão de Editor abaixo), nunca arrastada manualmente.
    private Dictionary<string, DialogueData> dialogueDB = new Dictionary<string, DialogueData>();

    // O que É serializável e aparece no Inspector: a lista "crua" de assets.
    // A Dictionary é reconstruída a partir dela.
    [SerializeField] private List<DialogueData> allDialogues = new List<DialogueData>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
   

        BuildDictionary();
    }

    private void BuildDictionary()
    {
        dialogueDB.Clear();

        foreach (var dialogue in allDialogues)
        {
            if (dialogue == null) continue;

            if (string.IsNullOrEmpty(dialogue.DialogueId))
            {
                Debug.LogError($"[DialogueDB] '{dialogue.name}' está sem DialogueId. Pulei ele.");
                continue;
            }

            if (dialogueDB.ContainsKey(dialogue.DialogueId))
            {
                Debug.LogError($"[DialogueDB] ID duplicado '{dialogue.DialogueId}' em '{dialogue.name}'. Mantendo o primeiro.");
                continue;
            }

            dialogueDB[dialogue.DialogueId] = dialogue;
            //Debug.Log($"[DialogueDB] Adicionado '{dialogue.DialogueId}' => '{dialogue.name}'");
        }
    }

    public DialogueData GetById(string id)
    {
        Debug.Log($"[DialogueDB] Buscando ID: '{id}' | dialogueDB tem {dialogueDB.Count} entradas.");
        if (string.IsNullOrEmpty(id)) return null;

        bool achou = dialogueDB.TryGetValue(id, out var d);
        Debug.Log($"[DialogueDB] Achou? {achou}");
        return achou ? d : null;
    }

    public bool Contains(string id) => dialogueDB.ContainsKey(id);

    // -----------------------------------------------------------------
    // PARTE EXCLUSIVA DO EDITOR
    // -----------------------------------------------------------------
#if UNITY_EDITOR
    [Header("Editor Only — Auto Populate")]
    [SerializeField] private string searchFolderPath = "Assets/Dialogues";

    [ContextMenu("Populate From Folder")]
    private void PopulateFromFolder()
    {
        string[] guids = AssetDatabase.FindAssets("t:DialogueData", new[] { searchFolderPath });

        allDialogues.Clear();
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var dialogue = AssetDatabase.LoadAssetAtPath<DialogueData>(path);
            if (dialogue != null)
                allDialogues.Add(dialogue);
        }

        EditorUtility.SetDirty(this);
        Debug.Log($"[DialogueDB] {allDialogues.Count} diálogos encontrados em '{searchFolderPath}'.");
    }
#endif
}
using System.Collections.Generic;
using System;
using UnityEngine;

[Serializable]
public struct Dialogue
{
    public string speakerName;
    [TextArea(5, 10)]
    public string dialogueText;
    public Sprite speakerPortrait;
   
}

[CreateAssetMenu(fileName = "NewDialogue", menuName = "NewDialogue")]
public class DialogueData : ScriptableObject
{

    [SerializeField] private string dialogueId; // GUID gerado uma vez, nunca mais mudado
    public string DialogueId => dialogueId;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrEmpty(dialogueId))
            dialogueId = System.Guid.NewGuid().ToString();
    }
#endif
    [Header("Variaveis de text")]
   public List<Dialogue> dialogues;
    [Header("Variaveis unicas")]
    public bool lastDialogue;
    [Space(5f)]
    public bool learnSkill;
    public SkillType skillToLearn;

    [Space(5f)]
    [Header("Variaveis de boss")]
    public bool isBossDialogue;

    [Space(5f)]
    public bool triggerLevelStart;
}

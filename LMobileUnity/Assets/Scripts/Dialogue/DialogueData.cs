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
    [Header("Variaveis de text")]
   public List<Dialogue> dialogues;
    [Header("Variaveis unicas")]
    public bool lastDialogue;
    [Space(5f)]
    public bool learnSkill;
    public SkillType skillToLearn;
}

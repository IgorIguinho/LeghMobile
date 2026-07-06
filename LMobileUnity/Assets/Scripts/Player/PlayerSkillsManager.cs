using UnityEngine;
using System;
using System.Collections.Generic;

public class PlayerSkillsManager : MonoBehaviour
{
    private const string PlayerPrefsPrefix = "LeghSkill_";

    public static PlayerSkillsManager Instance { get; private set; }

    [Header("Default Unlocked Skills")]
    [SerializeField] private List<SkillType> defaultUnlockedSkills = new List<SkillType> { SkillType.Sword, SkillType.Dash };

    private HashSet<SkillType> _unlockedSkills = new HashSet<SkillType>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadAllSkills();
    }

    private void LoadAllSkills()
    {
        _unlockedSkills.Clear();
        
        // Load default unlocked skills
        if (defaultUnlockedSkills != null)
        {
            foreach (SkillType defaultSkill in defaultUnlockedSkills)
            {
                _unlockedSkills.Add(defaultSkill);
            }
        }

        // Override with saved preferences if they exist
        foreach (SkillType skill in Enum.GetValues(typeof(SkillType)))
        {
            string key = PlayerPrefsPrefix + skill.ToString();
            if (PlayerPrefs.HasKey(key))
            {
                if (PlayerPrefs.GetInt(key, 0) == 1)
                {
                    _unlockedSkills.Add(skill);
                }
                else
                {
                    _unlockedSkills.Remove(skill);
                }
            }
        }
    }

    public void UnlockSkill(SkillType skill)
    {
        if (!_unlockedSkills.Contains(skill))
        {
            _unlockedSkills.Add(skill);
        }
        string key = PlayerPrefsPrefix + skill.ToString();
        PlayerPrefs.SetInt(key, 1);
        PlayerPrefs.Save();
    }

    public void LockSkill(SkillType skill)
    {
        if (_unlockedSkills.Contains(skill))
        {
            _unlockedSkills.Remove(skill);
        }
        string key = PlayerPrefsPrefix + skill.ToString();
        PlayerPrefs.SetInt(key, 0);
        PlayerPrefs.Save();
    }

    public bool IsSkillUnlocked(SkillType skill)
    {
        return _unlockedSkills.Contains(skill);
    }

    public void ResetAllSkills()
    {
        _unlockedSkills.Clear();
        foreach (SkillType skill in Enum.GetValues(typeof(SkillType)))
        {
            string key = PlayerPrefsPrefix + skill.ToString();
            PlayerPrefs.DeleteKey(key);
        }
        PlayerPrefs.Save();
        LoadAllSkills(); // Reload default configuration after reset
    }
}

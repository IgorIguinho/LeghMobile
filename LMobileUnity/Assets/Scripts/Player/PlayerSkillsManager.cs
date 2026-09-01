using UnityEngine;
using System;
using System.Collections.Generic;

public class PlayerSkillsManager : MonoBehaviour
{
    private const string PlayerPrefsPrefix = "LeghSkill_";

    public static PlayerSkillsManager Instance { get; private set; }

    public enum WeaponType
    {
        Sword,
        FireBall
    }

    public event Action<WeaponType> OnWeaponChanged;

    [Header("Default Unlocked Skills")]
    [SerializeField] private List<SkillType> defaultUnlockedSkills = new List<SkillType> { SkillType.Sword, SkillType.Dash };

    private HashSet<SkillType> _unlockedSkills = new HashSet<SkillType>();
    private WeaponType _currentWeapon = WeaponType.Sword;

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

        LoadSelectedWeapon();
    }

    private void LoadSelectedWeapon()
    {
        string weaponKey = PlayerPrefsPrefix + "SelectedWeapon";
        if (PlayerPrefs.HasKey(weaponKey))
        {
            int savedValue = PlayerPrefs.GetInt(weaponKey, (int)WeaponType.Sword);
            if (Enum.IsDefined(typeof(WeaponType), savedValue))
            {
                WeaponType savedWeapon = (WeaponType)savedValue;
                SkillType correspondingSkill = GetSkillTypeForWeapon(savedWeapon);
                if (IsSkillUnlocked(correspondingSkill))
                {
                    _currentWeapon = savedWeapon;
                }
                else
                {
                    _currentWeapon = WeaponType.Sword;
                }
            }
            else
            {
                _currentWeapon = WeaponType.Sword;
            }
        }
        else
        {
            _currentWeapon = WeaponType.Sword;
        }
    }

    public SkillType GetSkillTypeForWeapon(WeaponType weapon)
    {
        switch (weapon)
        {
            case WeaponType.FireBall:
                return SkillType.FireBall;
            case WeaponType.Sword:
            default:
                return SkillType.Sword;
        }
    }

    public bool TrySelectWeapon(WeaponType weapon)
    {
        SkillType requiredSkill = GetSkillTypeForWeapon(weapon);
        if (!IsSkillUnlocked(requiredSkill))
        {
            return false;
        }

        _currentWeapon = weapon;
        string key = PlayerPrefsPrefix + "SelectedWeapon";
        PlayerPrefs.SetInt(key, (int)weapon);
        PlayerPrefs.Save();

        OnWeaponChanged?.Invoke(_currentWeapon);
        return true;
    }

    public WeaponType GetCurrentWeapon()
    {
        return _currentWeapon;
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

        // Se a skill bloqueada for a arma atual, reverte para Sword se desbloqueada
        if (GetSkillTypeForWeapon(_currentWeapon) == skill)
        {
            TrySelectWeapon(WeaponType.Sword);
        }
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
        string weaponKey = PlayerPrefsPrefix + "SelectedWeapon";
        PlayerPrefs.DeleteKey(weaponKey);
        PlayerPrefs.Save();
        LoadAllSkills(); // Reload default configuration after reset
    }
}


using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq; // <- necessário para o .ToList() usado abaixo

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

        // NOTA: mantive o carregamento via PlayerPrefs como fallback (útil se
        // o SaveManager ainda não existir na cena, ex: durante testes rápidos
        // no editor). Quando o SaveManager estiver presente, ele vai chamar
        // LoadSaveData() logo em seguida e SOBRESCREVER esses valores com o
        // conteúdo do arquivo de save — que passa a ser a fonte da verdade.
        LoadAllSkills();
    }

    private void LoadAllSkills()
    {
        _unlockedSkills.Clear();

        if (defaultUnlockedSkills != null)
        {
            foreach (SkillType defaultSkill in defaultUnlockedSkills)
            {
                _unlockedSkills.Add(defaultSkill);
            }
        }

        foreach (SkillType skill in Enum.GetValues(typeof(SkillType)))
        {
            string key = PlayerPrefsPrefix + skill.ToString();
            if (PlayerPrefs.HasKey(key))
            {
                if (PlayerPrefs.GetInt(key, 0) == 1)
                    _unlockedSkills.Add(skill);
                else
                    _unlockedSkills.Remove(skill);
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
                _currentWeapon = IsSkillUnlocked(correspondingSkill) ? savedWeapon : WeaponType.Sword;
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
            return false;

        _currentWeapon = weapon;
        string key = PlayerPrefsPrefix + "SelectedWeapon";
        PlayerPrefs.SetInt(key, (int)weapon);
        PlayerPrefs.Save();

        OnWeaponChanged?.Invoke(_currentWeapon);
        return true;
    }

    public WeaponType GetCurrentWeapon() => _currentWeapon;

    public void UnlockSkill(SkillType skill)
    {
        _unlockedSkills.Add(skill);
        string key = PlayerPrefsPrefix + skill.ToString();
        PlayerPrefs.SetInt(key, 1);
        PlayerPrefs.Save();
    }

    public void LockSkill(SkillType skill)
    {
        _unlockedSkills.Remove(skill);
        string key = PlayerPrefsPrefix + skill.ToString();
        PlayerPrefs.SetInt(key, 0);
        PlayerPrefs.Save();

        if (GetSkillTypeForWeapon(_currentWeapon) == skill)
        {
            TrySelectWeapon(WeaponType.Sword);
        }
    }

    public bool IsSkillUnlocked(SkillType skill) => _unlockedSkills.Contains(skill);

    public void ResetAllSkills()
    {
        _unlockedSkills.Clear();
        foreach (SkillType skill in Enum.GetValues(typeof(SkillType)))
        {
            PlayerPrefs.DeleteKey(PlayerPrefsPrefix + skill.ToString());
        }
        PlayerPrefs.DeleteKey(PlayerPrefsPrefix + "SelectedWeapon");
        PlayerPrefs.Save();
        LoadAllSkills();
    }

    // ---------------------------------------------------------------
    // INTEGRAÇÃO COM O SAVE SYSTEM (novo)
    // ---------------------------------------------------------------

    /// <summary>Exporta o estado atual para o SaveManager gravar em disco.</summary>
    public SkillsSaveData GetSaveData()
    {
        return new SkillsSaveData
        {
            unlockedSkills = _unlockedSkills.ToList(),
            currentWeapon = _currentWeapon
        };
    }

    /// <summary>Aplica o estado lido do arquivo de save (chamado pelo SaveManager).</summary>
    public void LoadSaveData(SkillsSaveData data)
    {
        if (data == null) return;

        _unlockedSkills.Clear();
        if (data.unlockedSkills != null)
        {
            foreach (var skill in data.unlockedSkills)
                _unlockedSkills.Add(skill);
        }

        bool weaponValido = Enum.IsDefined(typeof(WeaponType), (int)data.currentWeapon)
                             && IsSkillUnlocked(GetSkillTypeForWeapon(data.currentWeapon));

        _currentWeapon = weaponValido ? data.currentWeapon : WeaponType.Sword;

        OnWeaponChanged?.Invoke(_currentWeapon);
    }
}
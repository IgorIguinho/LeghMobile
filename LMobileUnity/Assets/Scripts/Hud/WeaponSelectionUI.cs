using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WeaponSelectionUI : MonoBehaviour
{
    [System.Serializable]
    public class WeaponSlotUI
    {
        [Tooltip("Tipo de arma associado a este slot")]
        public PlayerSkillsManager.WeaponType weaponType;

        [Header("Elementos de UI")]
        public GameObject slotRoot;
        public Button selectButton;
        public Image weaponIcon;
        public TextMeshProUGUI weaponNameText;
        public TextMeshProUGUI weaponDescText;

        [Header("Feedback Visual")]
        public GameObject selectedIndicator;
        public GameObject lockedIndicator;
        public Image backgroundImage;
    }

    [Header("Painel Principal")]
    [SerializeField] private GameObject selectionPanel;
    [SerializeField] private Button closeButton;

    [Header("Slots de Armas")]
    [SerializeField] private List<WeaponSlotUI> weaponSlots = new List<WeaponSlotUI>();

    [Header("Cores de Estado")]
    [SerializeField] private Color selectedColor = new Color(0.2f, 0.8f, 0.2f, 1f);
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color lockedColor = new Color(0.5f, 0.5f, 0.5f, 0.6f);

    private void Awake()
    {
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseWeaponSelection);
        }

        // Configura os listeners dos botões sem alocação dinâmica em Update
        for (int i = 0; i < weaponSlots.Count; i++)
        {
            int index = i;
            if (weaponSlots[index].selectButton != null)
            {
                weaponSlots[index].selectButton.onClick.AddListener(() => OnSlotClicked(index));
            }
        }
    }

    private void Start()
    {
        if (selectionPanel != null)
        {
            selectionPanel.SetActive(false);
        }
    }

    private void OnEnable()
    {
        if (PlayerSkillsManager.Instance != null)
        {
            PlayerSkillsManager.Instance.OnWeaponChanged += HandleWeaponChanged;
        }
    }

    private void OnDisable()
    {
        if (PlayerSkillsManager.Instance != null)
        {
            PlayerSkillsManager.Instance.OnWeaponChanged -= HandleWeaponChanged;
        }
    }

    private void HandleWeaponChanged(PlayerSkillsManager.WeaponType weapon)
    {
        if (selectionPanel != null && selectionPanel.activeSelf)
        {
            UpdateUI();
        }
    }

    public void OpenWeaponSelection()
    {
        if (selectionPanel != null)
        {
            selectionPanel.SetActive(true);
            UpdateUI();
        }
    }

    public void CloseWeaponSelection()
    {
        // Só fecha se houver uma arma selecionada no PlayerSkillsManager (por segurança)
        if (PlayerSkillsManager.Instance != null)
        {
            _ = PlayerSkillsManager.Instance.GetCurrentWeapon();
        }

        if (selectionPanel != null)
        {
            selectionPanel.SetActive(false);
        }
    }

    public void OnSlotClicked(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= weaponSlots.Count) return;
        if (PlayerSkillsManager.Instance == null) return;

        WeaponSlotUI slot = weaponSlots[slotIndex];
        bool success = PlayerSkillsManager.Instance.TrySelectWeapon(slot.weaponType);
        if (success)
        {
            UpdateUI();
        }
    }

    public void UpdateUI()
    {
        if (PlayerSkillsManager.Instance == null) return;

        PlayerSkillsManager.WeaponType currentSelected = PlayerSkillsManager.Instance.GetCurrentWeapon();

        for (int i = 0; i < weaponSlots.Count; i++)
        {
            WeaponSlotUI slot = weaponSlots[i];
            SkillType skillType = PlayerSkillsManager.Instance.GetSkillTypeForWeapon(slot.weaponType);
            bool isUnlocked = PlayerSkillsManager.Instance.IsSkillUnlocked(skillType);
            bool isSelected = isUnlocked && (currentSelected == slot.weaponType);

            if (slot.selectButton != null)
            {
                slot.selectButton.interactable = isUnlocked;
            }

            if (slot.selectedIndicator != null)
            {
                slot.selectedIndicator.SetActive(isSelected);
            }

            if (slot.lockedIndicator != null)
            {
                slot.lockedIndicator.SetActive(!isUnlocked);
            }

            if (slot.backgroundImage != null)
            {
                if (!isUnlocked)
                {
                    slot.backgroundImage.color = lockedColor;
                }
                else if (isSelected)
                {
                    slot.backgroundImage.color = selectedColor;
                }
                else
                {
                    slot.backgroundImage.color = normalColor;
                }
            }
        }
    }
}

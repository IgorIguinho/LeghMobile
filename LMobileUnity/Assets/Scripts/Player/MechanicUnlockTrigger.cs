using UnityEngine;

public class MechanicUnlockTrigger : MonoBehaviour
{
    [Header("Skill Settings")]
    [Tooltip("The skill/mechanic that will be unlocked when the player triggers this.")]
    [SerializeField] private SkillType skillToUnlock;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (PlayerSkillsManager.Instance != null)
            {
                PlayerSkillsManager.Instance.UnlockSkill(skillToUnlock);
            }
            else
            {
                Debug.LogWarning("PlayerSkillsManager Instance is null. Make sure it is present in the scene.");
            }

            gameObject.SetActive(false);
        }
    }
}

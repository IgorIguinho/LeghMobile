using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Animator))]
public class Fase7NPC : MonoBehaviour
{
    [Header("NPC Configuration")]
    public int maxHealth = 100;
    public int actualHealth;
    public Slider healthSlider;

    [Header("Level 2 & 3 Support settings")]
    [Tooltip("Intervalo em segundos para o ataque de suporte.")]
    public float supportInterval = 8f;
    [Tooltip("Dano infligido pelo ataque de suporte.")]
    public int supportDamage = 5;
    [Tooltip("Tempo que o NPC fica visivel para atacar.")]
    public float supportVisibleDuration = 1.2f;

    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private Collider2D npcCollider;
    private bool isDead = false;
    private float supportTimer = 0f;
    private int currentStage = 1;

    private static readonly int HashPrep = Animator.StringToHash("Preparing");
    private static readonly int HashAttack = Animator.StringToHash("Attack");

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        npcCollider = GetComponent<Collider2D>();
    }

    private void Start()
    {
        actualHealth = maxHealth;
        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = actualHealth;
            healthSlider.gameObject.SetActive(true);
        }
    }

    public void InitializeStage(int stage)
    {
        currentStage = stage;
        isDead = false;

        if (stage == 1)
        {
            // Level 1: Active, vulnerable, preparing
            gameObject.SetActive(true);
            spriteRenderer.enabled = true;
            if (npcCollider != null) npcCollider.enabled = true;
            if (healthSlider != null) healthSlider.gameObject.SetActive(true);

            if (animator != null)
            {
                animator.SetBool(HashPrep, true);
            }
        }
        else if (stage == 2 || stage == 3)
        {
            // Level 2 & 3: Support mode, hidden/invisible by default, enemies ignore it
            gameObject.SetActive(true);
            spriteRenderer.enabled = false;
            if (npcCollider != null) npcCollider.enabled = false;
            if (healthSlider != null) healthSlider.gameObject.SetActive(false);

            if (animator != null)
            {
                animator.SetBool(HashPrep, false);
            }
            supportTimer = 0f;
        }
        else
        {
            // Level 4 (Boss): Completely inactive/hidden
            gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (currentStage == 2 || currentStage == 3)
        {
            supportTimer += Time.deltaTime;
            if (supportTimer >= supportInterval)
            {
                supportTimer = 0f;
                StartCoroutine(PerformSupportAttackRoutine());
            }
        }
    }

    private IEnumerator PerformSupportAttackRoutine()
    {
        // Find a random active enemy
        EnemyStats targetEnemy = FindRandomActiveEnemy();
        if (targetEnemy == null) yield break;

        // Teleport near the target enemy or simply stay in place and attack
        // Let's position the NPC slightly to the left/right of the enemy to look natural
        Vector3 attackPos = targetEnemy.transform.position + new Vector3(Mathf.Sign(transform.position.x - targetEnemy.transform.position.x) * 1.5f, 0f, 0f);
        transform.position = attackPos;

        // Appear
        spriteRenderer.enabled = true;
        if (animator != null)
        {
            animator.SetTrigger(HashAttack);
        }

        // Apply a color flash of light or magic effect
        Color originalColor = spriteRenderer.color;
        spriteRenderer.color = Color.cyan;

        // Deal damage
        targetEnemy.TakeDamage(supportDamage);

        yield return new WaitForSeconds(supportVisibleDuration);

        // Disappear
        spriteRenderer.color = originalColor;
        spriteRenderer.enabled = false;
    }

    private EnemyStats FindRandomActiveEnemy()
    {
        EnemyStats[] enemies = FindObjectsByType<EnemyStats>(FindObjectsSortMode.None);
        if (enemies == null || enemies.Length == 0) return null;

        List<EnemyStats> validEnemies = new List<EnemyStats>();
        foreach (var enemy in enemies)
        {
            if (enemy != null && enemy.gameObject.activeInHierarchy)
            {
                validEnemies.Add(enemy);
            }
        }

        if (validEnemies.Count == 0) return null;
        return validEnemies[Random.Range(0, validEnemies.Count)];
    }

    public void TakeDamage(int damage)
    {
        if (currentStage != 1 || isDead) return;

        actualHealth -= damage;
        if (healthSlider != null)
        {
            healthSlider.value = actualHealth;
        }

        // Damage flash
        StartCoroutine(DamageFlashRoutine());

        if (actualHealth <= 0)
        {
            Die();
        }
    }

    private IEnumerator DamageFlashRoutine()
    {
        spriteRenderer.color = Color.red;
        yield return new WaitForSeconds(0.15f);
        spriteRenderer.color = Color.white;
    }

    private void Die()
    {
        isDead = true;
        // NPC is dead, restart phase immediately
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BossFase8Controller : MonoBehaviour, IDamageable
{
    [System.Serializable]
    public struct BossPhaseData
    {
        public int healthThreshold;    // Fase ativa quando a vida atual do boss for <= esse valor
        public int caixasPerSequence;  // Quantas caixas spawnam em sequência nessa fase
        public float caixaSpeed;       // Velocidade da caixa nessa fase
        public float timeBetweenCaixas; // Intervalo entre cada caixa da sequência, nessa fase
    }

    [Header("Referências")]
    public GameObject caixaPrefab;
    public Transform[] spawnPoints;
    public Transform arenaLeftBound;
    public Transform arenaRightBound;
    public Slider healthSlider;
    public GameObject player;
    public Transform playerPosition;
    public DialogueData dialogue;

    [Header("Vida")]
    public int maxHealth = 100;

    [Header("Fases de dificuldade (baseadas na vida)")]
    [Tooltip("Configure do healthThreshold mais baixo (fase mais difícil) para o mais alto (fase mais fácil, tipicamente = maxHealth)")]
    public BossPhaseData[] phases;

    [Tooltip("Trava de segurança para performance mobile")]
    public int maxActiveCaixas = 6;

    [Header("Recovery ao tomar dano")]
    public float damageRecoveryTime = 1.5f;

    [Header("Knockback Pendular do Player")]
    [Tooltip("Ângulo máximo de abertura do pêndulo em graus")]
    [Range(5f, 85f)]
    public float pendulumAngle = 45f;

    [Tooltip("Gravidade do pêndulo usada para cálculo físico da curva")]
    public float pendulumGravity = 9.8f;

    [Tooltip("Duração total do trajeto de knockback em segundos")]
    public float knockbackDuration = 0.8f;

    [Tooltip("Tempo adicional de recuperação com movimento travado após o knockback")]
    public float knockbackRecoveryTime = 0.5f;

    public event System.Action<int> OnDamaged;
    public event System.Action OnDied;

    private int currentHealth;
    private bool isRecovering = false;
    private bool isDead = false;

    private SpriteRenderer spriteRenderer;
    private Color originalColor = Color.white;
    private Coroutine spawnCoroutine;
    private Coroutine recoveryCoroutine;
    private Coroutine flashCoroutine;
    private Coroutine knockbackCoroutine;

    // Cache para evitar alocação de GC no FixedUpdate
    private static readonly WaitForFixedUpdate waitForFixedUpdate = new WaitForFixedUpdate();

    // Dados para pré-visualização no OnDrawGizmos
    private Vector2 lastKnockbackStart;
    private Vector2 lastKnockbackTarget;
    private bool hasKnockbackCalculated = false;

    private readonly List<GameObject> activeCaixas = new List<GameObject>();

    public int CurrentHealth => currentHealth;
    public bool IsRecovering => isRecovering;
    public bool IsDead => isDead;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        player = GameObject.FindGameObjectWithTag("Player");
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
    }

    private void Start()
    {
        // StartBoss() chamado aqui por padrão/inicialização direta.
        // Futuramente, outro sistema gerenciador de fase/cutscene pode acionar StartBoss() externamente.
        StartBoss();
    }

    public void StartBoss()
    {
        currentHealth = maxHealth;
        isDead = false;
        isRecovering = false;

        if (healthSlider != null)
        {
            healthSlider.gameObject.SetActive(true);
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
        }

        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
        }
        spawnCoroutine = StartCoroutine(SpawnLoop());
    }

    public BossPhaseData GetCurrentPhase()
    {
        if (phases == null || phases.Length == 0)
        {
            return new BossPhaseData
            {
                healthThreshold = maxHealth,
                caixasPerSequence = 2,
                caixaSpeed = 5f,
                timeBetweenCaixas = 1f
            };
        }

        // Percorrido na ordem configurada (healthThreshold crescente: mais difícil para mais fácil)
        foreach (var phase in phases)
        {
            if (currentHealth <= phase.healthThreshold)
            {
                return phase;
            }
        }

        // Fallback: última fase configurada
        return phases[phases.Length - 1];
    }

    private IEnumerator SpawnLoop()
    {
        while (!isDead)
        {
            while (isRecovering && !isDead)
            {
                yield return null;
            }

            if (isDead) yield break;

            BossPhaseData currentPhase = GetCurrentPhase();
            int sequenceCount = Mathf.Max(1, currentPhase.caixasPerSequence);

            for (int i = 0; i < sequenceCount; i++)
            {
                if (isDead) yield break;

                while (isRecovering && !isDead)
                {
                    yield return null;
                }

                if (isDead) yield break;

                // Aguarda até haver espaço disponível no teto de caixas ativas
                CleanActiveCaixas();
                while (activeCaixas.Count >= maxActiveCaixas && !isDead)
                {
                    yield return null;
                    CleanActiveCaixas();
                }

                if (isDead) yield break;

                SpawnSingleCaixa(currentPhase);

                // Intervalo entre caixas dentro da sequência
                float timer = 0f;
                float interval = Mathf.Max(0.05f, currentPhase.timeBetweenCaixas);
                while (timer < interval && !isDead)
                {
                    if (isRecovering)
                    {
                        while (isRecovering && !isDead)
                        {
                            yield return null;
                        }
                    }
                    timer += Time.deltaTime;
                    yield return null;
                }
            }

            yield return null;
        }
    }

    private void SpawnSingleCaixa(BossPhaseData phase)
    {
        if (caixaPrefab == null || spawnPoints == null || spawnPoints.Length == 0) return;

        int randomIndex = Random.Range(0, spawnPoints.Length);
        Transform spawnPoint = spawnPoints[randomIndex];
        if (spawnPoint == null) return;

        Vector3 spawnPos = spawnPoint.position;

        // Limites da arena
        float minX = arenaLeftBound != null ? arenaLeftBound.position.x : (transform.position.x - 20f);
        float maxX = arenaRightBound != null ? arenaRightBound.position.x : (transform.position.x + 20f);
        if (minX > maxX)
        {
            float temp = minX;
            minX = maxX;
            maxX = temp;
        }

        float centerX = (minX + maxX) * 0.5f;

        // Ponto à direita do centro -> caixa vai para esquerda (-1). Ponto à esquerda -> vai para direita (1).
        int direction = spawnPos.x >= centerX ? -1 : 1;

        GameObject caixaObj = null;
        if (PoolManager.Instance != null)
        {
            caixaObj = PoolManager.Instance.Get(caixaPrefab, spawnPos, Quaternion.identity);
        }
        else
        {
            caixaObj = Instantiate(caixaPrefab, spawnPos, Quaternion.identity);
        }

        if (caixaObj != null)
        {
            activeCaixas.Add(caixaObj);

            CaixaController controller = caixaObj.GetComponent<CaixaController>();
            if (controller == null)
            {
                controller = caixaObj.AddComponent<CaixaController>();
            }

            controller.Init(direction, phase.caixaSpeed, minX, maxX);
        }
    }

    private void CleanActiveCaixas()
    {
        for (int i = activeCaixas.Count - 1; i >= 0; i--)
        {
            if (activeCaixas[i] == null || !activeCaixas[i].activeInHierarchy)
            {
                activeCaixas.RemoveAt(i);
            }
        }
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        CleanActiveCaixas();
        if (player != null && playerPosition != null)
        {
            ApplyPendulumKnockback(player, playerPosition.position);
        }

        if (currentHealth < 0) currentHealth = 0;

        if (healthSlider != null)
        {
            healthSlider.value = currentHealth;
        }

        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
        }
        flashCoroutine = StartCoroutine(DamageFlashRoutine());

        OnDamaged?.Invoke(damage);

        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        if (recoveryCoroutine != null)
        {
            StopCoroutine(recoveryCoroutine);
        }
        recoveryCoroutine = StartCoroutine(DamageRecoveryRoutine());
    }

    /// <summary>
    /// Inicia o knockback do player em trajetória de pêndulo físico via Rigidbody2D.
    /// </summary>
    public void ApplyPendulumKnockback(GameObject targetPlayer, Vector3 targetPosition)
    {
        if (targetPlayer == null) return;

        if (knockbackCoroutine != null)
        {
            StopCoroutine(knockbackCoroutine);
            knockbackCoroutine = null;
        }

        knockbackCoroutine = StartCoroutine(PendulumKnockbackRoutine(targetPlayer, targetPosition));
    }

    private IEnumerator PendulumKnockbackRoutine(GameObject targetPlayer, Vector3 targetPosition)
    {
        if (targetPlayer == null) yield break;

        Rigidbody2D playerRb = targetPlayer.GetComponent<Rigidbody2D>();
        PlayerMovements playerMovements = targetPlayer.GetComponent<PlayerMovements>();

        // Trava o movimento do player antes de iniciar o trajeto
        if (playerMovements != null)
        {
            playerMovements.canMove = false;
        }

        Vector2 startPos = playerRb != null ? playerRb.position : (Vector2)targetPlayer.transform.position;
        Vector2 targetPos = targetPosition;

        lastKnockbackStart = startPos;
        lastKnockbackTarget = targetPos;
        hasKnockbackCalculated = true;

        float duration = Mathf.Max(0.05f, knockbackDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            yield return waitForFixedUpdate;

            if (targetPlayer == null || playerRb == null) yield break;

            elapsed += Time.fixedDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);

            Vector2 nextPos = CalculatePendulumPosition(startPos, targetPos, progress, pendulumAngle);

            // Aplica velocidade contínua ao Rigidbody2D sem teleporte
            Vector2 requiredVelocity = (nextPos - playerRb.position) / Time.fixedDeltaTime;
            playerRb.linearVelocity = requiredVelocity;
        }

        // Zera velocidade ao fim do trajeto
        if (playerRb != null)
        {
            playerRb.linearVelocity = Vector2.zero;
        }

        // Mantém canMove travado pelo tempo de recuperação adicional
        if (knockbackRecoveryTime > 0f)
        {
            yield return new WaitForSeconds(knockbackRecoveryTime);
        }

        // Libera a movimentação do player
        if (playerMovements != null)
        {
            playerMovements.canMove = true;
        }

        knockbackCoroutine = null;
    }

    /// <summary>
    /// Calcula a posição no arco de pêndulo real com aceleração harmônica.
    /// </summary>
    public static Vector2 CalculatePendulumPosition(Vector2 startPos, Vector2 targetPos, float progress, float maxAngle)
    {
        Vector2 diff = targetPos - startPos;
        float dist = diff.magnitude;

        if (dist < 0.001f)
        {
            return Vector2.Lerp(startPos, targetPos, progress);
        }

        float angleRad = Mathf.Clamp(maxAngle, 5f, 85f) * Mathf.Deg2Rad;
        float sinHalf = Mathf.Sin(angleRad);
        float tanHalf = Mathf.Tan(angleRad);

        float radius = dist / (2f * sinHalf);
        float height = dist / (2f * tanHalf);

        Vector2 dir = diff / dist;
        Vector2 normal = new Vector2(-dir.y, dir.x);

        // Garante que o pivô do pêndulo fique posicionado abaixo da corda/trajetória,
        // fazendo a curva do arco projetar o player para cima
        if (normal.y > 0f)
        {
            normal = -normal;
        }
        if (Mathf.Abs(normal.y) < 0.0001f)
        {
            normal = Vector2.down;
        }

        Vector2 mid = (startPos + targetPos) * 0.5f;
        Vector2 pivot = mid + normal * height;

        Vector2 v0 = startPos - pivot;
        Vector2 v1 = targetPos - pivot;

        float angle0 = Mathf.Atan2(v0.y, v0.x);
        float angle1 = Mathf.Atan2(v1.y, v1.x);
        float deltaAngle = Mathf.DeltaAngle(angle0 * Mathf.Rad2Deg, angle1 * Mathf.Rad2Deg) * Mathf.Deg2Rad;

        // Progressão harmônica do pêndulo (maior velocidade no ponto inferior do arco)
        float harmonicProgress = (1f - Mathf.Cos(progress * Mathf.PI)) * 0.5f;
        float currentAngle = angle0 + deltaAngle * harmonicProgress;

        return pivot + new Vector2(Mathf.Cos(currentAngle), Mathf.Sin(currentAngle)) * radius;
    }

    private IEnumerator DamageFlashRoutine()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.red;
            yield return new WaitForSeconds(0.15f);
            if (!isDead && spriteRenderer != null)
            {
                spriteRenderer.color = originalColor;
            }
        }
    }

    private IEnumerator DamageRecoveryRoutine()
    {
        isRecovering = true;
        yield return new WaitForSeconds(damageRecoveryTime);
        isRecovering = false;
        recoveryCoroutine = null;
    }

    private void Die()
    {
        isDead = true;
     
        if (knockbackCoroutine != null)
        {
            StopCoroutine(knockbackCoroutine);
            knockbackCoroutine = null;
        }

        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }

        if (recoveryCoroutine != null)
        {
            StopCoroutine(recoveryCoroutine);
            recoveryCoroutine = null;
        }

        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
            flashCoroutine = null;
        }
        player.GetComponent<PlayerInteract>().CanOpenDialogue(true, dialogue);
        player.GetComponent<PlayerInteract>().OpenDialogue();

        ReleaseAllActiveCaixas();
        OnDied?.Invoke();

        gameObject.SetActive(false);
    }

    private void ReleaseAllActiveCaixas()
    {
        for (int i = 0; i < activeCaixas.Count; i++)
        {
            GameObject caixa = activeCaixas[i];
            if (caixa != null && caixa.activeInHierarchy)
            {
                if (PoolManager.Instance != null)
                {
                    PoolManager.Instance.Release(caixa);
                }
                else
                {
                    caixa.SetActive(false);
                }
            }
        }
        activeCaixas.Clear();
    }

    private void OnDisable()
    {
        if (knockbackCoroutine != null)
        {
            StopCoroutine(knockbackCoroutine);
            knockbackCoroutine = null;
        }
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }
        if (recoveryCoroutine != null)
        {
            StopCoroutine(recoveryCoroutine);
            recoveryCoroutine = null;
        }
        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
            flashCoroutine = null;
        }
    }

    private void OnDrawGizmos()
    {
        Vector2 start;
        Vector2 target;

        if (hasKnockbackCalculated)
        {
            start = lastKnockbackStart;
            target = lastKnockbackTarget;
        }
        else if (playerPosition != null)
        {
            start = player != null ? (Vector2)player.transform.position : (Vector2)transform.position;
            target = playerPosition.position;
        }
        else
        {
            return;
        }

        DrawPendulumGizmo(start, target);
    }

    private void DrawPendulumGizmo(Vector2 startPos, Vector2 targetPos)
    {
        if ((targetPos - startPos).sqrMagnitude < 0.001f) return;

        Gizmos.color = Color.cyan;
        int segments = 30;
        Vector2 prevPoint = CalculatePendulumPosition(startPos, targetPos, 0f, pendulumAngle);

        for (int i = 1; i <= segments; i++)
        {
            float t = (float)i / segments;
            Vector2 currentPoint = CalculatePendulumPosition(startPos, targetPos, t, pendulumAngle);
            Gizmos.DrawLine(prevPoint, currentPoint);
            prevPoint = currentPoint;
        }

        // Desenha marcadores no início e no destino
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(startPos, 0.25f);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(targetPos, 0.25f);
    }
}

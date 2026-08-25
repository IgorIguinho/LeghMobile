using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class BossController : MonoBehaviour, IDamageable
{
    public enum BossState
    {
        AttackSequence,
        Vulnerable,
        HitReaction,
        Dead
    }

    [System.Serializable]
    public struct BossPhaseData
    {
        public int healthThreshold;
        public int shotsPerSequence;
        public float moveSpeed;
        public float timeBetweenShots;
    }

    [Header("Health & State")]
    [SerializeField] private int maxHealth = 10;
    [SerializeField] private Slider bossHealthSlider;
    [SerializeField] private BossState state = BossState.AttackSequence;

    [Header("Waypoints & Movement")]
    [SerializeField] private Transform[] waypoints;
    [SerializeField] private float defaultFlySpeed = 4f;
    [SerializeField] private float fleeSpeed = 8f;

    [Header("Vulnerability & Visuals")]
    [SerializeField] private float vulnerabilityDuration = 2.5f;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color vulnerableColor = Color.yellow;
    [SerializeField] private Behaviour bossLight;

    [Header("Combat & Phases")]
    [SerializeField] private BossPhaseData[] phaseSettings;
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private float playerDetectRadius = 25f;

    [Header("Death / Finish Level")]
    [SerializeField] private DialogueData deathDialogue;
    [SerializeField] private GameObject finishLevelObject;

    private int actualHealth;
    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Animator animator;
    private Transform playerTransform;
    private int facingDirection = -1;
    private Coroutine currentRoutine;
    private int currentWaypointIndex = -1;

    // Current phase parameters
    private int currentShotsPerSequence = 2;
    private float currentMoveSpeed = 4f;
    private float currentTimeBetweenShots = 0.8f;

    // Mobile zero-allocation physics cache
    private ContactFilter2D playerFilter;
    private readonly Collider2D[] detectResults = new Collider2D[1];

    public BossState CurrentState => state;
    public int ActualHealth => actualHealth;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();

        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        playerFilter = new ContactFilter2D();
        playerFilter.useLayerMask = true;
        playerFilter.SetLayerMask(playerLayer);
        playerFilter.useTriggers = true;
    }

    private void Start()
    {
        InitializeBoss();
    }

    public void InitializeBoss()
    {
        actualHealth = maxHealth;
        currentMoveSpeed = defaultFlySpeed;

        ApplyCurrentPhaseSettings();

        if (bossHealthSlider != null)
        {
            bossHealthSlider.gameObject.SetActive(true);
            bossHealthSlider.maxValue = maxHealth;
            bossHealthSlider.value = actualHealth;
        }

        if (sr != null)
        {
            sr.color = normalColor;
        }

        if (bossLight != null)
        {
            bossLight.enabled = false;
        }

        FindPlayer();

        if (currentRoutine != null)
        {
            StopCoroutine(currentRoutine);
        }
        currentRoutine = StartCoroutine(BossFsmLoop());
    }

    private void Update()
    {
        if (state == BossState.Dead) return;

        if (playerTransform == null || !playerTransform.gameObject.activeInHierarchy)
        {
            FindPlayer();
        }
    }

    private void FindPlayer()
    {
        int count = Physics2D.OverlapCircle(transform.position, playerDetectRadius, playerFilter, detectResults);
        if (count > 0 && detectResults[0] != null)
        {
            playerTransform = detectResults[0].transform;
        }
        else
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                playerTransform = playerObj.transform;
            }
        }
    }

    private void ApplyCurrentPhaseSettings()
    {
        if (phaseSettings == null || phaseSettings.Length == 0)
        {
            currentShotsPerSequence = 2;
            currentMoveSpeed = defaultFlySpeed;
            currentTimeBetweenShots = 0.8f;
            return;
        }

        // Search for the matching phase configured for current health threshold
        // Expected order: descending health thresholds or matching active tier
        BossPhaseData selectedPhase = phaseSettings[0];
        bool matched = false;

        for (int i = 0; i < phaseSettings.Length; i++)
        {
            if (actualHealth <= phaseSettings[i].healthThreshold)
            {
                selectedPhase = phaseSettings[i];
                matched = true;
            }
        }

        if (!matched && phaseSettings.Length > 0)
        {
            selectedPhase = phaseSettings[0];
        }

        currentShotsPerSequence = selectedPhase.shotsPerSequence > 0 ? selectedPhase.shotsPerSequence : 2;
        currentMoveSpeed = selectedPhase.moveSpeed > 0 ? selectedPhase.moveSpeed : defaultFlySpeed;
        currentTimeBetweenShots = selectedPhase.timeBetweenShots > 0 ? selectedPhase.timeBetweenShots : 0.8f;
    }

    private IEnumerator BossFsmLoop()
    {
        while (state != BossState.Dead)
        {
            // 1. Attack Sequence State
            state = BossState.AttackSequence;
            if (sr != null) sr.color = normalColor;
            if (bossLight != null) bossLight.enabled = false;

            Transform nextWaypoint = GetNextAttackWaypoint();
            if (nextWaypoint != null)
            {
                yield return FlyToPosition(nextWaypoint.position, currentMoveSpeed);
            }

            // Aim and Face Player
            FacePlayer();

            // Fire Projectile Sequence
            for (int i = 0; i < currentShotsPerSequence; i++)
            {
                if (state == BossState.Dead) yield break;

                FacePlayer();
                ShootProjectile();

                float shootTimer = 0f;
                while (shootTimer < currentTimeBetweenShots)
                {
                    if (state != BossState.AttackSequence) yield break;
                    shootTimer += Time.deltaTime;
                    yield return null;
                }
            }

            yield return new WaitForSeconds(0.3f);

            // 2. Vulnerable State
            state = BossState.Vulnerable;
            if (sr != null) sr.color = vulnerableColor;
            if (bossLight != null) bossLight.enabled = true;

            float vulnTimer = 0f;
            while (vulnTimer < vulnerabilityDuration)
            {
                if (state != BossState.Vulnerable)
                {
                    // Interrupted by TakeDamage -> HitReaction
                    yield break;
                }
                vulnTimer += Time.deltaTime;
                yield return null;
            }

            // If not attacked during vulnerability, reset visual and loop
            if (sr != null) sr.color = normalColor;
            if (bossLight != null) bossLight.enabled = false;
        }
    }

    private Transform GetNextAttackWaypoint()
    {
        if (waypoints == null || waypoints.Length == 0) return null;
        if (waypoints.Length == 1) return waypoints[0];

        int newIndex = Random.Range(0, waypoints.Length);
        if (newIndex == currentWaypointIndex)
        {
            newIndex = (newIndex + 1) % waypoints.Length;
        }

        currentWaypointIndex = newIndex;
        return waypoints[currentWaypointIndex];
    }

    private Transform GetFurthestWaypointFromPlayer()
    {
        if (waypoints == null || waypoints.Length == 0) return null;

        Vector3 comparePos = playerTransform != null ? playerTransform.position : transform.position;
        Transform furthest = waypoints[0];
        float maxDistSq = -1f;

        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] == null) continue;
            float distSq = (waypoints[i].position - comparePos).sqrMagnitude;
            if (distSq > maxDistSq)
            {
                maxDistSq = distSq;
                furthest = waypoints[i];
                currentWaypointIndex = i;
            }
        }

        return furthest;
    }

    private IEnumerator FlyToPosition(Vector3 targetPos, float speed)
    {
        while (Vector3.Distance(transform.position, targetPos) > 0.08f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPos, speed * Time.deltaTime);
            yield return null;
        }
        transform.position = targetPos;
    }

    private void FacePlayer()
    {
        if (playerTransform == null) return;
        int dir = (int)Mathf.Sign(playerTransform.position.x - transform.position.x);
        if (dir != facingDirection && dir != 0)
        {
            facingDirection = dir;
            transform.rotation = Quaternion.Euler(0, facingDirection > 0 ? 180f : 0f, 0);
        }
    }

    private void ShootProjectile()
    {
        if (projectilePrefab == null) return;

        Vector2 spawnPos = firePoint != null ? (Vector2)firePoint.position : (Vector2)transform.position;
        GameObject proj;

        if (Fase7PoolManager.Instance != null)
        {
            proj = Fase7PoolManager.Instance.Get(projectilePrefab, spawnPos, Quaternion.identity);
        }
        else
        {
            proj = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
        }

        if (proj == null) return;

        ProjectileEnemyComplex projEnemy = proj.GetComponent<ProjectileEnemyComplex>();
        if (projEnemy != null && playerTransform != null)
        {
            Vector2 directionVector = ((Vector2)playerTransform.position - spawnPos).normalized;
            projEnemy.directionPlayer = directionVector;
        }

        SpriteRenderer projSr = proj.GetComponent<SpriteRenderer>();
        if (projSr != null)
        {
            projSr.flipX = facingDirection > 0;
        }
    }

    public void TakeDamage(int damage)
    {
        // 100% immune if not in Vulnerable state
        if (state != BossState.Vulnerable) return;

        actualHealth -= damage;
        if (bossHealthSlider != null)
        {
            bossHealthSlider.value = actualHealth;
        }

        StartCoroutine(DamageFlashRoutine());

        if (actualHealth <= 0)
        {
            Die();
            return;
        }

        // Trigger Hit Reaction State
        if (currentRoutine != null)
        {
            StopCoroutine(currentRoutine);
        }
        currentRoutine = StartCoroutine(HitReactionRoutine());
    }

    private IEnumerator DamageFlashRoutine()
    {
        if (sr != null)
        {
            Color current = sr.color;
            sr.color = Color.red;
            yield return new WaitForSeconds(0.15f);
            if (state == BossState.Vulnerable)
            {
                sr.color = vulnerableColor;
            }
            else
            {
                sr.color = normalColor;
            }
        }
    }

    private IEnumerator HitReactionRoutine()
    {
        state = BossState.HitReaction;

        // Reset visual cues
        if (sr != null) sr.color = normalColor;
        if (bossLight != null) bossLight.enabled = false;

        // Apply new phase progression based on updated health
        ApplyCurrentPhaseSettings();

        // High-speed flight retreat to the furthest waypoint from player
        Transform escapeWaypoint = GetFurthestWaypointFromPlayer();
        if (escapeWaypoint != null)
        {
            yield return FlyToPosition(escapeWaypoint.position, fleeSpeed);
        }

        yield return new WaitForSeconds(0.2f);

        // Resume main FSM attack loop
        currentRoutine = StartCoroutine(BossFsmLoop());
    }

    private void Die()
    {
        state = BossState.Dead;

        if (currentRoutine != null)
        {
            StopCoroutine(currentRoutine);
            currentRoutine = null;
        }

        if (sr != null) sr.color = normalColor;
        if (bossLight != null) bossLight.enabled = false;

        if (playerTransform != null && deathDialogue != null)
        {
            PlayerInteract playerInteract = playerTransform.GetComponent<PlayerInteract>();
            if (playerInteract != null)
            {
                playerInteract.CanOpenDialogue(true, deathDialogue);
            }
        }

        if (finishLevelObject != null)
        {
            finishLevelObject.SetActive(true);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, playerDetectRadius);

        if (waypoints != null)
        {
            Gizmos.color = Color.cyan;
            for (int i = 0; i < waypoints.Length; i++)
            {
                if (waypoints[i] != null)
                {
                    Gizmos.DrawWireSphere(waypoints[i].position, 0.5f);
                    if (i < waypoints.Length - 1 && waypoints[i + 1] != null)
                    {
                        Gizmos.DrawLine(waypoints[i].position, waypoints[i + 1].position);
                    }
                }
            }
        }
    }
}

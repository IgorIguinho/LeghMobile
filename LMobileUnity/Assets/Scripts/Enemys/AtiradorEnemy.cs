using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(EnemyStats))]
public class AtiradorEnemy : MonoBehaviour
{
    enum EnemyState { Idle, Moving, Charging, Shooting, Cooldown }

    [Header("State (Debug)")]
    [SerializeField] private EnemyState state = EnemyState.Idle;

    [Header("Movement")]
    public float speed = 2f;
    [Tooltip("Distancia minima para parar de mover e comecar a atirar.")]
    public float minDistance = 5f;

    [Header("Targeting")]
    public Transform targetOverride;
    public LayerMask layerPlayer;
    public float playerDetectRadius = 15f;

    [Header("Attack")]
    public GameObject projectileObj;
    public Transform firePoint;
    public float chargeTime = 0.6f;
    public float 
        shootRecover = 0.3f;
    public float attackCooldown = 1.5f;

    private Rigidbody2D rb;
    private Animator animator;
    private Transform target;
    private int direction = -1;
    private bool busy = false;

    private WaitForSeconds waitCharge;
    private WaitForSeconds waitRecover;
    private WaitForSeconds waitCooldown;

    // Contact filter for zero GC player detection when override is not set
    private ContactFilter2D playerFilter;
    private readonly Collider2D[] detectResults = new Collider2D[1];

    private static readonly int HashSpeed = Animator.StringToHash("Speed");
    private static readonly int HashCharge = Animator.StringToHash("Charge");
    private static readonly int HashShoot = Animator.StringToHash("Shoot");

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();

        playerFilter = new ContactFilter2D();
        playerFilter.useLayerMask = true;
        playerFilter.SetLayerMask(layerPlayer);
        playerFilter.useTriggers = true;

        waitCharge = new WaitForSeconds(chargeTime);
        waitRecover = new WaitForSeconds(shootRecover);
        waitCooldown = new WaitForSeconds(attackCooldown);
    }

    private void OnEnable()
    {
        state = EnemyState.Idle;
        busy = false;
        target = null;
    }

    private void Update()
    {
        FindTarget();

        if (busy) return;

        if (target == null)
        {
            state = EnemyState.Idle;
            if (rb != null) rb.linearVelocity = Vector2.zero;
            UpdateAnimator(0f);
            return;
        }

        float distance = Vector3.Distance(transform.position, target.position);
        int targetDir = (int)Mathf.Sign(target.position.x - transform.position.x);

        // Check if direction needs flipping
        if (targetDir != direction && targetDir != 0)
        {
            Flip(targetDir);
        }

        if (distance > minDistance)
        {
            // Move towards player
            state = EnemyState.Moving;
            if (rb != null)
            {
                Vector2 moveDirection = ((Vector2)target.position - rb.position).normalized;
                rb.MovePosition(rb.position + moveDirection * speed * Time.deltaTime);
            }
            UpdateAnimator(speed);
        }
        else
        {
            // Stop and Attack
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
            }
            UpdateAnimator(0f);
            StartCoroutine(AttackRoutine());
        }
    }

    private void FindTarget()
    {
        if (targetOverride != null)
        {
            target = targetOverride.gameObject.activeInHierarchy ? targetOverride : null;
        }
        else
        {
            int count = Physics2D.OverlapCircle(transform.position, playerDetectRadius, playerFilter, detectResults);
            target = count > 0 ? detectResults[0].transform : null;
        }
    }

    private void Flip(int targetDir)
    {
        direction = targetDir;
        transform.rotation = Quaternion.Euler(0, direction > 0 ? 180f : 0f, 0);
    }

    private void UpdateAnimator(float currentSpeed)
    {
        if (animator != null)
        {
            animator.SetFloat(HashSpeed, Mathf.Abs(currentSpeed));
        }
    }

    private IEnumerator AttackRoutine()
    {
        busy = true;
        state = EnemyState.Charging;

        if (animator != null) animator.SetTrigger(HashCharge);
        yield return waitCharge;

        if (target != null)
        {
            state = EnemyState.Shooting;
            if (animator != null) animator.SetTrigger(HashShoot);

            Shoot();
            yield return waitRecover;
        }

        state = EnemyState.Cooldown;
        yield return waitCooldown;

        state = EnemyState.Idle;
        busy = false;
    }

    private void Shoot()
    {
        Vector2 spawn = firePoint != null ? firePoint.position : (Vector2)transform.position;
        GameObject projectile;

        if (Fase7PoolManager.Instance != null && projectileObj != null)
        {
            projectile = Fase7PoolManager.Instance.Get(projectileObj, spawn, Quaternion.identity);
        }
        else if (projectileObj != null)
        {
            projectile = Instantiate(projectileObj, spawn, Quaternion.identity);
        }
        else
        {
            return;
        }

        ProjectileEnemyComplex projEnemy = projectile.GetComponent<ProjectileEnemyComplex>();
        if (projEnemy != null)
        {
            Vector2 directionVector = (target.position - transform.position).normalized;
            projEnemy.directionPlayer = directionVector;
        }

        SpriteRenderer sr = projectile.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.flipX = direction > 0;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, minDistance);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, playerDetectRadius);
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(EnemyStats))]
public class ObserverEnemy : MonoBehaviour
{
    enum EnemyState { Moving, Telegraphing, Dashing, Done }

    [Header("States")]
    [SerializeField] private EnemyState state = EnemyState.Moving;

    public bool seePlayer;
    public int direction;
    private Rigidbody2D rb;
    private ColliderDmgEnemy dmgEnemyScript;
    [SerializeField] private LayerMask enemyLayer;

    public float speed;

    [Header("Detecção e Ataque")]
    public Vector2 lenghtCheckPlayer;
    public Transform tranformCheckerPlayer;
    public LayerMask layerPlayer;

    [Tooltip("Distancia para iniciar o ataque carregado.")]
    public float chargeDistance = 2.5f;
    [Tooltip("Tempo de espera na preparacao (telegraph).")]
    public float telegraphDuration = 0.5f;
    [Tooltip("Forca do impulso de dash.")]
    public float dashForce = 12f;

    [Header("Targeting Override")]
    public Transform targetOverride;

    private Transform currentTarget;
    private bool isAttacking = false;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        seePlayer = false;
        dmgEnemyScript = GetComponent<ColliderDmgEnemy>();
        if (dmgEnemyScript != null)
        {
            dmgEnemyScript.direction = direction;
        }
    }

    private void OnEnable()
    {
        state = EnemyState.Moving;
        seePlayer = false;
        isAttacking = false;
        currentTarget = null;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (state == EnemyState.Done) return;

        FindTarget();

        if (state == EnemyState.Moving)
        {
            if (seePlayer) 
            { 
                Moviment();
                CheckAttackTrigger();
            }
            CheckSeePlayer();
        }
    }

    void Moviment()
    {
        float targetPosx = currentTarget ? Mathf.Abs(currentTarget.position.x) : 0;
        float myPosx = Mathf.Abs(transform.position.x);
       
        float distance = Mathf.Abs(targetPosx - myPosx);
        if (rb != null && distance >1)
        {
            rb.linearVelocity = new Vector2(speed * direction, rb.linearVelocity.y);
        }
        else if (currentTarget.tag != "Player")
        {
            rb.linearVelocity = new Vector2(speed * direction, rb.linearVelocity.y);
        }
        else
        { rb.linearVelocityX = 0; }
    }

    void FindTarget()
    {
        if (targetOverride != null)
        {
            currentTarget = targetOverride.gameObject.activeInHierarchy ? targetOverride : null;
        }
        else
        {
            Collider2D col = Physics2D.OverlapBox(tranformCheckerPlayer.position, lenghtCheckPlayer, 0, layerPlayer);
            currentTarget = col != null ? col.transform : null;
            
        }

        if (currentTarget == null) return;
        bool ignore = currentTarget.tag != "Player";
        var targetCol = currentTarget.GetComponent<Collider2D>();
        if (targetCol) Physics2D.IgnoreCollision(GetComponent<Collider2D>(), targetCol, ignore);

    }

    void CheckSeePlayer()
    {
        if (currentTarget != null)
        {
            seePlayer = true;
            int directionPlayer = (int)Mathf.Sign(currentTarget.position.x - transform.position.x);
            if (directionPlayer != direction && directionPlayer != 0) 
            { 
                Flip(); 
            }
        }
        else
        {
            seePlayer = false;
        }
    }

    void CheckAttackTrigger()
    {
        if (currentTarget != null && !isAttacking)
        {
            float dist = Vector3.Distance(transform.position, currentTarget.position);
            if (dist <= chargeDistance)
            {
                StartCoroutine(TelegraphAndDashRoutine());
            }
        }
    }

    IEnumerator TelegraphAndDashRoutine()
    {
        isAttacking = true;
        state = EnemyState.Telegraphing;

        // Stop movement for telegraph
        if (rb != null)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }

        // Visual telegraph: flashing red color
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        Color originalColor = sr != null ? sr.color : Color.white;
        if (sr != null) sr.color = Color.red;

        yield return new WaitForSeconds(telegraphDuration);

        if (sr != null) sr.color = originalColor;

        // Start Dash
        state = EnemyState.Dashing;

        // Ensure we are facing the target
        if (currentTarget != null)
        {
            int targetDir = (int)Mathf.Sign(currentTarget.position.x - transform.position.x);
            if (targetDir != direction && targetDir != 0)
            {
                Flip();
            }
        }

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            // Apply impulse along facing direction
            rb.AddForce(new Vector2(direction * dashForce, 2f), ForceMode2D.Impulse);
        }

        // Wait a short duration to let the physical dash complete and potentially hit
        yield return new WaitForSeconds(0.3f);

        state = EnemyState.Done;

        // Die/Return to pool
        EnemyStats enemyStats = GetComponent<EnemyStats>();
        if (enemyStats != null)
        {
            enemyStats.Death();
        }
        else if (PoolManager.Instance != null)
        {
            PoolManager.Instance.Release(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Flip()
    {
        direction *= -1;
        if (dmgEnemyScript != null)
        {
            dmgEnemyScript.direction = direction;
        }
        transform.Rotate(0, 180f, 0);
    }

    private void OnDrawGizmos()
    {
        if (tranformCheckerPlayer != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(tranformCheckerPlayer.position, lenghtCheckPlayer);
        }
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, chargeDistance);
    }
}

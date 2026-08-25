using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAttack : MonoBehaviour
{

    bool canAttack;
    public float attackSpeed;
    public int dmg;


    public Animator swordAnimator;

    public Transform areaAttack;
    public Vector2 lengthAreaAttack;
    public LayerMask enemyLayer;
    [SerializeField] LayerMask boxLayer;
    [SerializeField] LayerMask singleBoxLayer;
    [SerializeField] LayerMask projectileLayer;

    Rigidbody2D rb;
    InputReader input;
    Animator animator;

    // Buffer reutilizado para a deteccao de caixas e projeteis sem alocacao (mobile).
    readonly Collider2D[] singleBoxHitBuffer = new Collider2D[1];
    ContactFilter2D singleBoxFilter;

    readonly Collider2D[] boxHitsBuffer = new Collider2D[8];
    ContactFilter2D boxFilter;

    readonly Collider2D[] projectileHitsBuffer = new Collider2D[8];
    ContactFilter2D projectileFilter;

    private void OnEnable()
    {
        if (input != null) input.AttackTriggered += OnAttackInput;
    }

    private void OnDisable()
    {
        if (input != null) input.AttackTriggered -= OnAttackInput;
    }

    private void Awake()
    {
        input = GetComponent<InputReader>();

        // Filtro sem alocação para detectar apenas colliders na layer das caixas.
        boxFilter = new ContactFilter2D();
        boxFilter.useTriggers = true;
        boxFilter.SetLayerMask(boxLayer);
        boxFilter.useLayerMask = true;

        singleBoxFilter = new ContactFilter2D();
        singleBoxFilter.useTriggers = true;
        singleBoxFilter.SetLayerMask(singleBoxLayer);
        singleBoxFilter.useLayerMask = true;

        projectileFilter = new ContactFilter2D();
        projectileFilter.useTriggers = true;
        projectileFilter.SetLayerMask(projectileLayer);
        projectileFilter.useLayerMask = true;
    }

    void OnAttackInput()
    {
        if (canAttack) { StartCoroutine(Attack()); }
    }

    // Start is called before the first frame update
    void Start()
    {
        rb = gameObject.GetComponent<Rigidbody2D>();
        animator = gameObject.GetComponent<Animator>();
        canAttack = true;
    }

    IEnumerator Attack()
    {
        canAttack = false;
        rb.linearVelocity = Vector2.zero;
        swordAnimator.Play("Player_Attack");

        Collider2D[] hitEnemies = Physics2D.OverlapBoxAll(areaAttack.position, lengthAreaAttack, 0, enemyLayer);

        foreach (Collider2D enemy in hitEnemies)
        {
            // Tenta pegar qualquer componente que implemente IDamageable
            IDamageable damageable = enemy.GetComponent<IDamageable>();

            if (damageable != null)
            {
                damageable.TakeDamage(dmg);
            }
        }

        
        if (singleBoxLayer != 0)
        {        
            int singleBoxCount = Physics2D.OverlapBox(areaAttack.position, lengthAreaAttack, 0f, singleBoxFilter, singleBoxHitBuffer);
            for (int i = 0; i < singleBoxCount; i++)
            {
                GameObject breakableSingle = singleBoxHitBuffer[i].gameObject;
                if (breakableSingle != null)
                {
                    breakableSingle.SetActive(false);
                }
            }
        }


        // Detecção de caixas quebráveis (sem alocação) na mesma área de ataque.
        if (boxLayer.value != 0)
        {
            int boxCount = Physics2D.OverlapBox(areaAttack.position, lengthAreaAttack, 0f, boxFilter, boxHitsBuffer);
            for (int i = 0; i < boxCount; i++)
            {
                BreakableBoxTilemap breakable = boxHitsBuffer[i].GetComponent<BreakableBoxTilemap>();
                if (breakable != null)
                {
                    breakable.TryBreakInArea(areaAttack.position, lengthAreaAttack);
                }
            }
        }

        // Destruição / Parry de projéteis inimigos (sem alocação).
        if (projectileLayer.value != 0)
        {
            int projCount = Physics2D.OverlapBox(areaAttack.position, lengthAreaAttack, 0f, projectileFilter, projectileHitsBuffer);
            for (int i = 0; i < projCount; i++)
            {
                GameObject proj = projectileHitsBuffer[i].gameObject;
                if (proj != null)
                {
                    if (Fase7PoolManager.Instance != null)
                    {
                        Fase7PoolManager.Instance.Release(proj);
                    }
                    else
                    {
                        Destroy(proj);
                    }
                }
            }
        }


        yield return new WaitForSeconds(attackSpeed);

        canAttack = true;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(areaAttack.position, lengthAreaAttack);
    }
}

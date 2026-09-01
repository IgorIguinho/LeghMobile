using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    [Header("Sword Attack Settings")]
    public bool canAttack = true;
    public float attackSpeed;
    public int dmg;

    public Animator swordAnimator;

    public Transform areaAttack;
    public Vector2 lengthAreaAttack;
    public LayerMask enemyLayer;
    [SerializeField] LayerMask boxLayer;
    [SerializeField] LayerMask singleBoxLayer;
    [SerializeField] LayerMask projectileLayer;

    [Header("Fireball Settings")]
    public GameObject fireballPrefab;
    public Transform firePoint;
    public int fireballDmg;
    public int fireballSpeed;
    public float fireballAttackSpeed;
    private bool canFireball = true;
    private PlayerSkillsManager.WeaponType currentWeapon = PlayerSkillsManager.WeaponType.Sword;

    Rigidbody2D rb;
    InputReader input;
    Animator animator;
    PlayerMovements playerMovements;

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
        if (PlayerSkillsManager.Instance != null)
        {
            PlayerSkillsManager.Instance.OnWeaponChanged += HandleWeaponChanged;
        }
    }

    private void OnDisable()
    {
        if (input != null) input.AttackTriggered -= OnAttackInput;
        if (PlayerSkillsManager.Instance != null)
        {
            PlayerSkillsManager.Instance.OnWeaponChanged -= HandleWeaponChanged;
        }
    }

    private void HandleWeaponChanged(PlayerSkillsManager.WeaponType newWeapon)
    {
        currentWeapon = newWeapon;
    }

    private void Awake()
    {
        input = GetComponent<InputReader>();
        playerMovements = GetComponent<PlayerMovements>();

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
        if (currentWeapon == PlayerSkillsManager.WeaponType.Sword)
        {
            if (canAttack) { StartCoroutine(Attack()); }
        }
        else if (currentWeapon == PlayerSkillsManager.WeaponType.FireBall)
        {
            if (canFireball) { StartCoroutine(FireballAttack()); }
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        rb = gameObject.GetComponent<Rigidbody2D>();
        animator = gameObject.GetComponent<Animator>();
        if (playerMovements == null)
        {
            playerMovements = gameObject.GetComponent<PlayerMovements>();
        }

        if (PlayerSkillsManager.Instance != null)
        {
            currentWeapon = PlayerSkillsManager.Instance.GetCurrentWeapon();
            PlayerSkillsManager.Instance.OnWeaponChanged -= HandleWeaponChanged;
            PlayerSkillsManager.Instance.OnWeaponChanged += HandleWeaponChanged;
        }

        canAttack = true;
        canFireball = true;
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

    public IEnumerator FireballAttack()
    {
        canFireball = false;

        if (firePoint != null && fireballPrefab != null)
        {
            GameObject projObj = null;
            if (Fase7PoolManager.Instance != null)
            {
                projObj = Fase7PoolManager.Instance.Get(fireballPrefab, firePoint.position, Quaternion.identity);
            }
            else
            {
                projObj = Instantiate(fireballPrefab, firePoint.position, Quaternion.identity);
            }

            if (projObj != null)
            {
                projObj.transform.position = firePoint.position;
                ProjectilePlayer proj = projObj.GetComponent<ProjectilePlayer>();
                if (proj != null)
                {
                    proj.projectileSpeed = fireballSpeed;
                    proj.projectileDmg = fireballDmg;
                    int dir = (playerMovements != null) ? playerMovements.direction : 1;
                    proj.direction = dir;
                }
            }
        }

        yield return new WaitForSeconds(fireballAttackSpeed);

        canFireball = true;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(areaAttack.position, lengthAreaAttack);
    }
}

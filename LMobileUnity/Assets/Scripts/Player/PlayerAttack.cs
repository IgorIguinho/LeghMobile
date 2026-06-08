using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAttack : MonoBehaviour
{

    bool canAttack;
    public float attackSpeed;


    public Animator swordAnimator;

    public Transform areaAttack;
    public Vector2 lengthAreaAttack;
    public LayerMask enemyLayer;

    Rigidbody2D rb;
    InputReader input;
    Animator animator;

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
            if (enemy.GetComponent<EnemyStats>() != null) { enemy.GetComponent<EnemyStats>().Death(); }
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

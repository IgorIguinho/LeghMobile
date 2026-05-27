using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAttack : MonoBehaviour
{

    bool canAttack;
    public float attackSpeed;


    public ParticleSystem swordAnimator;

    public Transform areaAttack;
    public Vector2 lengthAreaAttack;
    public LayerMask enemyLayer;

    Rigidbody2D rb;
    PlayerControl control;
    Animator animator;

    private void Awake()
    {
        control = new PlayerControl();
        control.Enable();

        control.Land.Attack.performed += ctx =>
        { if (canAttack) { StartCoroutine(Attack()); } };
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
        swordAnimator.Play();

        if (Physics2D.OverlapBox(areaAttack.position, lengthAreaAttack, 0, enemyLayer))
        {
            GameObject objTouched;
            objTouched = Physics2D.OverlapBox(areaAttack.position, lengthAreaAttack, 0, enemyLayer).gameObject;
            objTouched.GetComponent<EnemyStats>().Death();

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

using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class RangedEnemy : MonoBehaviour
{
    [Header("Ataque")]
    public float attackSpeed;
    public GameObject projectileObj;
    int direction;
    bool canAttack;

    [Header("Verificar se ve o player")]
    public Vector2 lenghtCheckPlayer;
    public Transform tranformCheckerPlayer;
    public LayerMask layerPlayer;
    // Start is called before the first frame update
    void Start()
    {
        canAttack = true;
    }

    // Update is called once per frame
    void Update()
    {
        CheckSeePlayer();
    }

    void CheckSeePlayer()
    {
        if (Physics2D.OverlapBox(tranformCheckerPlayer.position, lenghtCheckPlayer, 0, layerPlayer) && canAttack)
        {
           
                StartCoroutine(AttackPlayer());
        }
    }

    IEnumerator AttackPlayer()
    {
        canAttack = false;
        GameObject projectile = Instantiate(projectileObj,transform);
        projectile.GetComponent<ProjectileEnemy>().direction = direction;
        yield return new WaitForSeconds(attackSpeed);
        canAttack = true;
    
        yield break;
    }

    void Flip()
    {
        direction *= -1;
        transform.Rotate(0, 180f, 0);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(tranformCheckerPlayer.position, lenghtCheckPlayer);
    }
}

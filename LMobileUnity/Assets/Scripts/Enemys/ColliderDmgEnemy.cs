using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColliderDmgEnemy : MonoBehaviour
{
    public Collider2D dmgCollider;
    public int dmg;
    public int direction;
    public int forceImpulseDmg;
    Rigidbody2D rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.IsTouching(dmgCollider) && collision.gameObject.tag == "Player")
        {
            collision.GetComponent<PlayerStats>().TakeDmg(dmg);
            Destroy(this.gameObject);
            //ImpulseDmg(collision.gameObject);
            
        }
    }

    void ImpulseDmg(GameObject obj)
    {
        
       
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(new Vector2(forceImpulseDmg * direction, forceImpulseDmg ), ForceMode2D.Impulse);

    }
}

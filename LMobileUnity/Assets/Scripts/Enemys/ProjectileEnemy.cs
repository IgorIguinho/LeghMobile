using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectileEnemy : MonoBehaviour
{
    public int projectileSpeed;
    public int projectileDmg;
    public int direction;

    Rigidbody2D rb;
    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        Destroy(this.gameObject, 10f);
    }

    // Update is called once per frame
    void Update()
    {
        Moviment(); 
    }

    void Moviment()
    {
        rb.linearVelocity = new Vector2(projectileSpeed , 0);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.tag == "Player") 
        {
            collision.GetComponent<PlayerStats>().TakeDmg(projectileDmg);
            Destroy(this.gameObject);
        }

        if (collision.gameObject.tag == "Ground")
        {
            Destroy(this.gameObject);
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpikeDmg : MonoBehaviour
{
    public int dmg;
    GameObject player;
    // Start is called before the first frame update
    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
    }

    public void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.tag == "Player")
        {
            player.GetComponent<PlayerStats>().TakeDmg(dmg);
            player.GetComponent<Rigidbody2D>().linearVelocity = new Vector2(0, 10);
        }
    }
}

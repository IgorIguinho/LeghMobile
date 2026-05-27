using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FallPlataform : MonoBehaviour
{
    Rigidbody2D rb;
    public BoxCollider2D colliderCheckPlayer;
    BoxCollider2D colliderBox;
    float timeFall = 0.5f;
    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        colliderBox = GetComponent<BoxCollider2D>();   
        rb.bodyType = RigidbodyType2D.Kinematic;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    IEnumerator StartFall()
    {
        rb.bodyType = RigidbodyType2D.Dynamic;
        yield return new WaitForSeconds(timeFall);
        colliderBox.enabled = false;
        yield return new WaitForSeconds(timeFall);
        Destroy(this.gameObject);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.IsTouching(colliderCheckPlayer) && collision.gameObject.tag == "Player")
        {            
                StartCoroutine(StartFall()); 
                colliderCheckPlayer.enabled = false;
        }
    }
}

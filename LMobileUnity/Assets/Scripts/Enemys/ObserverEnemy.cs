using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

public class ObserverEnemy : MonoBehaviour
{
    public bool seePlayer;
    public int direction;
    Rigidbody2D rb;
    ColliderDmgEnemy dmgEnemyScript;

    public float speed;

    public Vector2 lenghtCheckPlayer;
    public Transform tranformCheckerPlayer;
    public LayerMask layerPlayer;
    // Start is called before the first frame update
    void Start()
    {
        rb=GetComponent<Rigidbody2D>();
        seePlayer = false;
        dmgEnemyScript = GetComponent<ColliderDmgEnemy>();
        dmgEnemyScript.direction = direction;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (seePlayer) { Moviment(); }
        CheckSeePlayer();
    }

    void Moviment()
    {
        rb.linearVelocity = new Vector2(speed * direction, rb.linearVelocity.y);
        Debug.Log(rb.linearVelocity.magnitude);
    }

    void CheckSeePlayer()
    {
        if ( Physics2D.OverlapBox(tranformCheckerPlayer.position,lenghtCheckPlayer,0,layerPlayer))
        {
            GameObject playerObj;
            playerObj = Physics2D.OverlapBox(tranformCheckerPlayer.position, lenghtCheckPlayer, 0, layerPlayer).gameObject;
            seePlayer = true;
            int directionPlayer = (int)Mathf.Sign((playerObj.transform.position.x - transform.position.x).ConvertTo<int>());
            Debug.Log(directionPlayer);
            if (directionPlayer != direction) { Flip(); }
        }
    }

    void Flip()
    {
        direction *= -1;
        dmgEnemyScript.direction = direction;
        transform.Rotate(0, 180f, 0);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(tranformCheckerPlayer.position,lenghtCheckPlayer);
    }
}

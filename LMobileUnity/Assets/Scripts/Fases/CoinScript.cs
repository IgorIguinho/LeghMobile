using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoinScript : MonoBehaviour
{
    public int id;
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.gameObject.tag == "Player")
        {
            FaseManager.Instance.colectedCoin++;
            
            FaseManager.Instance.colectedIDCoin.Add(id);
            Destroy(this.gameObject);
        }
    }
}

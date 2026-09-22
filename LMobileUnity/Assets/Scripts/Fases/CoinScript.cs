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
            if(FaseManager.Instance == null || FaseProgressSaveManager.Instance == null)
            {
                Destroy(this.gameObject);
                return;
            }
            FaseManager.Instance.colectedCoin++;
            FaseProgressSaveManager.Instance.RegisterCoinCollected(FaseManager.Instance.actualFase.sceneFase, id);
            FaseManager.Instance.colectedIDCoin.Add(id);
            FaseManager.Instance.UptadeInfosUI(-1);
            Destroy(this.gameObject);
        }
    }
}

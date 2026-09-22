using UnityEngine;

public class RecoverHP : MonoBehaviour
{
    public int recoverAmount = 1; // Amount of HP to recover

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            PlayerStats playerStats = collision.gameObject.GetComponent<PlayerStats>();
            if (playerStats != null)
            {
                playerStats.Heal(recoverAmount);

                if (PoolManager.Instance != null)
                {
                    PoolManager.Instance.Release(gameObject);
                }
                else
                {
                    Destroy(this.gameObject);
                }
            }
        }
    }
}

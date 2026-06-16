using UnityEngine;

public class SwitchSpeed : MonoBehaviour
{
    public float switchSpeed ;
    public bool switchSpeedSlow;
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            collision.GetComponent<PlayerMovements>().speedSwitch = switchSpeed;
            collision.GetComponent<PlayerMovements>().switchSpeedSlow = switchSpeedSlow;
        }
    }
}

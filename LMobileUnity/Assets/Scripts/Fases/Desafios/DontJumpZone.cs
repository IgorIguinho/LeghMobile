using UnityEngine;

public class DontJumpZone : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;
        var pm = collision.GetComponent<PlayerMovements>();
        if (pm != null) pm.canJump = false;   // bloqueia o pulo (gravidade continua)
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;
        var pm = collision.GetComponent<PlayerMovements>();
        if (pm != null) pm.canJump = true;     // reabilita o pulo ao sair
    }
}

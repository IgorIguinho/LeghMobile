using UnityEngine;

public class InvertGravityZone : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;

        var pm = collision.GetComponent<PlayerMovements>();
        if (pm != null) pm.ToggleGravity();
    }
}

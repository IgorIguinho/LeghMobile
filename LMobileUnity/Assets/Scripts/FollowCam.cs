using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using Unity.VisualScripting.Antlr3.Runtime.Tree;
using UnityEngine;

public class FollowCam : MonoBehaviour
{
    public Transform player;
    public float MinX, MaxX;
    public float MinY, MaxY;
    public float timelarp;

    [Header("Camera Shake")]
    private float shakeDuration = 0f;
    private float shakeMagnitude = 0.1f;
    private Vector3 shakeOffset = Vector3.zero;

    [Header("Boss Fight")]
    public bool isBossFight = false;
    public float MinXBoss, MaxXBoss;
    public float MinYBoss, MaxYBoss;

    public void TriggerShake(float duration, float magnitude)
    {
        shakeDuration = duration;
        shakeMagnitude = magnitude;
    }

    private void FixedUpdate()
    {
        if (player != null)
        {
            Vector3 newPosition = player.position + new Vector3(0, 0, -10);
            newPosition = Vector3.Lerp(transform.position, newPosition, timelarp);
        
            if (shakeDuration > 0)
            {
                shakeOffset = (Vector3)Random.insideUnitCircle * shakeMagnitude;
                newPosition += shakeOffset;
                shakeDuration -= Time.fixedDeltaTime;
            }
            else
            {
                shakeOffset = Vector3.zero;
            }

            transform.position = newPosition;

            if (shakeDuration > 0) return;
          
            if (isBossFight)
            {
                FollowPlayer(MinXBoss, MaxXBoss, MinYBoss, MaxYBoss);
            }
            else
            {
                FollowPlayer(MinX, MaxX, MinY, MaxY);
            }

        }
    }

    public void FollowPlayer(float min_x, float max_x, float min_y, float max_y)
    {
        transform.position = new Vector3(Mathf.Clamp(transform.position.x, min_x, max_x), Mathf.Clamp(transform.position.y, min_y, max_y), transform.position.z);
    }


    private void OnDrawGizmosSelected()
    {
        // Limites normais (verde)
        DrawBoundsGizmo(MinX, MaxX, MinY, MaxY, Color.green);

        // Limites do Boss Fight (vermelho)
        if (MaxXBoss != 0 || MinXBoss != 0 || MaxYBoss != 0 || MinYBoss != 0)
        {
            DrawBoundsGizmo(MinXBoss, MaxXBoss, MinYBoss, MaxYBoss, Color.red);
        }
    }

    private void DrawBoundsGizmo(float min_x, float max_x, float min_y, float max_y, Color color)
    {
        Gizmos.color = color;

        Vector3 center = new Vector3((min_x + max_x) / 2f, (min_y + max_y) / 2f, transform.position.z);
        Vector3 size = new Vector3(max_x - min_x, max_y - min_y, 0.1f);

        Gizmos.DrawWireCube(center, size);
    }
}


using System.Collections;
using System.Collections.Generic;
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
            transform.position = new Vector3(Mathf.Clamp(transform.position.x, MinX, MaxX),Mathf.Clamp(transform.position.y, MinY, MaxY), transform.position.z);
        }
    }
}


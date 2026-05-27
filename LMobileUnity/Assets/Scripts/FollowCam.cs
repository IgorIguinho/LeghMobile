using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowCam : MonoBehaviour
{
    public Transform player;
    public float MinX, MaxX;
    public float MinY, MaxY;
    public float timelarp;



    private void FixedUpdate()
    {
        if (player != null)
        {
            Vector3 newPosition = player.position + new Vector3(0, 0, -10);
        ;
            newPosition = Vector3.Lerp(transform.position, newPosition, timelarp);
            transform.position = newPosition;

            transform.position = new Vector3(Mathf.Clamp(transform.position.x, MinX, MaxX),Mathf.Clamp(transform.position.y, MinY, MaxY), transform.position.z);
        }
        
      
    }


}


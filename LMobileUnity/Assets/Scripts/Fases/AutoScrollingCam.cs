using UnityEngine;

public class AutoScrollingCam : MonoBehaviour
{
    public Transform player;
    public float speedScrolling;
    public float MinX, MaxX;
    public float MinY, MaxY;
    public float timelarp;
    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;

    }
    private void FixedUpdate()
    {
        transform.Translate(Vector3.right * speedScrolling * Time.deltaTime);
    }

    void MovNormal()
    {

        Vector3 newPosition = player.position + new Vector3(0, 0, -10);

        newPosition = Vector3.Lerp(transform.position, newPosition, timelarp);
        transform.position = newPosition;

        //transform.position = new Vector3(Mathf.Clamp(transform.position.x, MinX, MaxX), Mathf.Clamp(transform.position.y, MinY, MaxY), transform.position.z);
    }
}

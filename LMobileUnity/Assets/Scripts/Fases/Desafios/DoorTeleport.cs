using System.Collections.Generic;
using UnityEngine;

public class DoorTeleport : MonoBehaviour
{
    GameObject player;
    InputReader input; // Reference to the InputManager script
    bool inputAtualizado;
    public GameObject teleportDestination;
    [Header("Detecção (alcance único)")]
    public Vector2 detectRadius;
    public LayerMask layerPlayer;

    // --- Detecção sem alocação (zero GC) ---
    private ContactFilter2D playerFilter;
    private readonly Collider2D[] detectResults = new Collider2D[1];


   
    private void OnDisable()
    {
        if (input != null) input.EnterDoorTriggered -= Teleport;
    }


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        input = GameObject.FindGameObjectWithTag("Player").GetComponent<InputReader>();
        player = GameObject.FindGameObjectWithTag("Player");
    }

    private void Start()
    {
        playerFilter = new ContactFilter2D();
        playerFilter.useLayerMask = true;
        playerFilter.SetLayerMask(layerPlayer);
        playerFilter.useTriggers = true;
    }
    private void Update()
    {
        int count = Physics2D.OverlapBox(transform.position, detectRadius, 0f, playerFilter, detectResults);
        if (count > 0 && !inputAtualizado)
        {
            if (input != null) input.EnterDoorTriggered += Teleport;
            inputAtualizado = true;
        }
        else if (count == 0 && inputAtualizado)
        {
            if (input != null) input.EnterDoorTriggered -= Teleport;
            inputAtualizado = false;
        }
    }

    public void Teleport()
    {
        Collider2D hit = Physics2D.OverlapBox(transform.position, detectRadius, 0, layerPlayer);
        hit.transform.position = teleportDestination.transform.position;
    }


    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(transform.position, detectRadius);
    }
}



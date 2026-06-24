using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputReader : MonoBehaviour
{
    public static InputReader Instance { get; private set; }
    public PlayerControl controls;

    public float Direction { get; private set; }
    
    public event Action JumpTriggered;
    public event Action DashTriggered;
    public event Action AttackTriggered;

    public event Action RewindStarted;
    public event Action RewindCanceled;

    public event Action EnterDoorTriggered;

    public event Action OpenDialogueTriggered;
    public event Action InteractDialogueTriggered;



    private void Awake()
    {
        // If there is an instance, and it's not me, delete myself.

        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }
        controls = new PlayerControl();

        // Movimento (Leitura contínua com reset no cancelamento)
        controls.Land.Move.performed += ctx => Direction = ctx.ReadValue<float>();
        controls.Land.Move.canceled += ctx => Direction = 0f;

        // Gatilhos para ações discretas
        controls.Land.Jump.performed += _ => JumpTriggered?.Invoke();
        controls.Land.Dash.performed += _ => DashTriggered?.Invoke();
        controls.Land.Attack.performed += _ => AttackTriggered?.Invoke();

        // Rewind (Hold behavior)
        controls.Land.Rewind.started += _ => RewindStarted?.Invoke();
        controls.Land.Rewind.canceled += _ => RewindCanceled?.Invoke();

        // Interação com portas
        controls.Land.EnterDoor.performed += _ => EnterDoorTriggered?.Invoke();

        //Dialogos
        controls.Land.Interact.performed += _ => OpenDialogueTriggered?.Invoke();
        controls.Dialogue.Interact.performed += _ => InteractDialogueTriggered?.Invoke();
    }

    private void OnEnable()
    {
        if (controls != null) controls.Enable();
    }

    private void OnDisable()
    {
        if (controls != null) controls.Disable();
    }

    public void TradeActionMap(InputActionMap enableMap, InputActionMap disableMap)
    {
        enableMap.Enable();
        disableMap.Disable();
    }
}

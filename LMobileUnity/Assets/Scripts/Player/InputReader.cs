using UnityEngine;
using UnityEngine.InputSystem;
using System;

public class InputReader : MonoBehaviour
{
    private PlayerControl controls;

    public float Direction { get; private set; }
    
    public event Action JumpTriggered;
    public event Action DashTriggered;
    public event Action AttackTriggered;
    public event Action RewindStarted;
    public event Action RewindCanceled;
    public event Action EnterDoorTriggered;

    private void Awake()
    {
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
    }

    private void OnEnable()
    {
        if (controls != null) controls.Enable();
    }

    private void OnDisable()
    {
        if (controls != null) controls.Disable();
    }
}

using UnityEngine;

[CreateAssetMenu(fileName = "NewFase7BossConfig", menuName = "ScriptableObjects/Fase7BossConfig")]
public class Fase7BossConfig : ScriptableObject
{
    [Header("Base Stats")]
    public int maxHealth = 300;
    public float speed = 4f;
    public DialogueData dialogue;

    [Header("Ataques Gerais")]
    public float actionCooldown = 2f;
    public float meleeReach = 2.2f;
    public int meleeDamage = 15;
    public float projectileSpeed = 10f;
    public int projectileDamage = 10;

    [Header("Probabilidades HP > 50%")]
    [Range(0, 100)] public int phase1MeleeWeight = 40;
    [Range(0, 100)] public int phase1ProjectileWeight = 40;
    [Range(0, 100)] public int phase1ChargeWeight = 20;

    [Header("Probabilidades HP <= 50%")]
    [Range(0, 100)] public int phase2MeleeWeight = 35;
    [Range(0, 100)] public int phase2ProjectileWeight = 35;
    [Range(0, 100)] public int phase2ChargeWeight = 25;
    [Range(0, 100)] public int phase2SequenceWeight = 5;

    [Header("Platform Behavior")]
    public float walkDurationDifferentPlatform = 2f;
    public int platform4ChargeSection = 0;
    public float jumpForceY = 12f;
    public float jumpForceX = 5f;
    public float platformThresholdY = 0.5f;

    [Header("Contact & Chase Jump")]
    public int contactDamage = 10;
    public float chaseJumpForce = 8f;

    [Tooltip("Distância horizontal máxima para o Boss pular para a plataforma do player (Melee). Acima disso ele anda e atira.")]
    public float jumpChaseDistance = 4f;
}
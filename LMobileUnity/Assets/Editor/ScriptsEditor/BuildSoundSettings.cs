using UnityEngine;

/// <summary>
/// Asset de configuração para o BuildSoundNotifier.
/// Crie um via: botão direito na Project window > Create > Build Tools > Build Sound Settings
/// Coloque este script dentro de uma pasta "Editor" (ex: Assets/Editor/).
/// </summary>
[CreateAssetMenu(fileName = "BuildSoundSettings", menuName = "Build Tools/Build Sound Settings")]
public class BuildSoundSettings : ScriptableObject
{
    [Header("Sons de Build")]
    [Tooltip("Som tocado quando o build termina com sucesso.")]
    public AudioClip successSound;

    [Tooltip("Som tocado quando o build falha.")]
    public AudioClip failSound;

    [Range(0f, 1f)]
    public float volume = 1f;
}

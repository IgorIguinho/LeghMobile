using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System.Reflection;

/// <summary>
/// Toca um AudioClip escolhido pelo usuário quando o build termina.
/// Coloque este script dentro de uma pasta "Editor" (ex: Assets/Editor/).
/// Requer um asset BuildSoundSettings criado em algum lugar do projeto
/// (Create > Build Tools > Build Sound Settings).
/// </summary>
public class BuildSoundNotifier : IPostprocessBuildWithReport
{
    public int callbackOrder => 0;

    public void OnPostprocessBuild(BuildReport report)
    {
        bool success = report.summary.result == BuildResult.Succeeded;
        BuildSoundSettings settings = FindSettings();

        if (settings == null)
        {
            Debug.LogWarning("BuildSoundNotifier: nenhum asset BuildSoundSettings encontrado no projeto. " +
                "Crie um em Assets > Create > Build Tools > Build Sound Settings.");
            return;
        }

        AudioClip clip = success ? settings.successSound : settings.failSound;

        if (clip != null)
        {
            PlayClip(clip);
        }
    }

    // Procura o primeiro asset BuildSoundSettings existente no projeto
    private static BuildSoundSettings FindSettings()
    {
        string[] guids = AssetDatabase.FindAssets("t:BuildSoundSettings");
        if (guids.Length == 0) return null;

        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
        return AssetDatabase.LoadAssetAtPath<BuildSoundSettings>(path);
    }

    // Toca um AudioClip dentro do Editor usando a API interna AudioUtil (via reflection)
    private static void PlayClip(AudioClip clip)
    {
        Assembly editorAssembly = typeof(AudioImporter).Assembly;
        System.Type audioUtilType = editorAssembly.GetType("UnityEditor.AudioUtil");

        // A assinatura do método muda um pouco entre versões do Unity,
        // então tentamos algumas variações conhecidas.
        MethodInfo method = audioUtilType.GetMethod(
            "PlayPreviewClip",
            BindingFlags.Static | BindingFlags.Public,
            null,
            new System.Type[] { typeof(AudioClip), typeof(int), typeof(bool) },
            null
        );

        if (method != null)
        {
            method.Invoke(null, new object[] { clip, 0, false });
            return;
        }

        // Fallback: tenta a assinatura mais simples (apenas o clip)
        method = audioUtilType.GetMethod(
            "PlayPreviewClip",
            BindingFlags.Static | BindingFlags.Public,
            null,
            new System.Type[] { typeof(AudioClip) },
            null
        );

        if (method != null)
        {
            method.Invoke(null, new object[] { clip });
        }
        else
        {
            Debug.LogWarning("BuildSoundNotifier: não foi possível encontrar o método PlayPreviewClip nesta versão do Unity.");
        }
    }
}

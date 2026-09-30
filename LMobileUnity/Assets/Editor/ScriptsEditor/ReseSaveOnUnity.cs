
using System.IO;

using UnityEditor;

using UnityEngine;

public static class ResetSaveOnUnity 
{
    private const string savePath = "C:/Users/rogi0/AppData/LocalLow/DefaultCompany/ProjectLeghMobile\\legh_save.json";


    [MenuItem("Tools/Reset Save")]
    private static void ResetSave()
    {
        if (File.Exists(savePath)) File.Delete(savePath);
    
        
    }
}

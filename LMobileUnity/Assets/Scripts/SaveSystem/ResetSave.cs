using UnityEngine;

public class ResetSave : MonoBehaviour
{
    public void ResetGameSave()
    {
        SaveManager.Instance.DeleteSave();
    }
}

using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;

public class RewindManager : MonoBehaviour
{
    [SerializeField] static List<RewindObj> rewindObjs = new List<RewindObj>();
    public bool isRewindAll;
    InputReader input;

    private void Awake()
    {
        input = Object.FindAnyObjectByType<InputReader>();
    }

    private void OnEnable()
    {
        if (input != null)
        {
            input.RewindStarted += StartAllRewind;
            input.RewindCanceled += StopAllRewind;
        }
    }

    private void OnDisable()
    {
        if (input != null)
        {
            input.RewindStarted -= StartAllRewind;
            input.RewindCanceled -= StopAllRewind;
        }
    }
public static void Register(RewindObj rewindObj)
    {
        if (!rewindObjs.Contains(rewindObj))
        { rewindObjs.Add(rewindObj); }
    }

    public static void UnRegister(RewindObj rewindObj)
    {
        rewindObjs.Remove(rewindObj);
    }
    
    public void StartAllRewind()
    {
        isRewindAll = true;
        foreach (var item in rewindObjs)
        {
            item.StartRewind();
        }
    }

    public void StopAllRewind()
    {
        isRewindAll = false;
        foreach (var item in rewindObjs)
        {
            item.StopRewind();
        }
    }
   
}

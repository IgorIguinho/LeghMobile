using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class HudManagerOnFase : MonoBehaviour
{
    public static HudManagerOnFase Instance { get; private set; }

    public TextMeshProUGUI coinCount;
    public TextMeshProUGUI hpCount;
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
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FaseManager : MonoBehaviour
{
    public static FaseManager Instance { get; private set; }

    public int colectedCoin;
    public int colectedMedal;
    public int enemyDead;

    public FaseScriptable actualFase;

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
        HudManagerOnFase.Instance.coinCount.text = "Moedas " + colectedCoin.ToString();
    }
}

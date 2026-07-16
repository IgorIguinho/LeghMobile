
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FaseManager : MonoBehaviour
{
    public static FaseManager Instance { get; private set; }

    public int colectedCoin;
    public int colectedMedal;
    public int enemyDead;

    public List<int> colectedIDCoin;

    public List<GameObject> coinsList;

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
        foreach (var coinID in actualFase.colectedCoin)
        {
            if(coinID == coinsList[coinID].GetComponent<CoinScript>().id)
            {
                coinsList[coinID].SetActive(false);
            }
        }
        CarregarCoinsJson();
    }

    // Update is called once per frame
    void Update()
    {
        HudManagerOnFase.Instance.coinCount.text = "Moedas " + colectedCoin.ToString();
    }
    public void UptadeFaseScritable()
    {
        actualFase.colectedCoin.AddRange(colectedIDCoin);
    }

    void CarregarCoinsJson()
    {
        if (PlayerPrefs.HasKey("Coins"))
        {
            string json = PlayerPrefs.GetString("Coins");
            colectedIDCoin = JsonConvert.DeserializeObject<List<int>>(json);
        }

        foreach (var coinID in colectedIDCoin)
        {
            if (coinID == coinsList[coinID].GetComponent<CoinScript>().id)
            {
                coinsList[coinID].SetActive(false);
            }
        }
    }
}

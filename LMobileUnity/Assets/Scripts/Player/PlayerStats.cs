using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerStats : MonoBehaviour
{
    public int maxHp;
    public int actualHp;

    public float timeAnimationDmg;
    SpriteRenderer sr;
    // Start is called before the first frame update
    void Start()
    {
        actualHp = maxHp;
        sr = GetComponent<SpriteRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void TakeDmg(int dmg)
    {
        actualHp -= dmg;
        HudManagerOnFase.Instance.hpCount.text = "HP " + actualHp.ToString();
        StartCoroutine(AnimationTakeDmg());

        if (actualHp <= 0) { SceneManager.LoadScene("PrototipoTelaInicial"); }
    }

    IEnumerator AnimationTakeDmg()
    {
        sr.color = Color.red;
        yield return new WaitForSeconds(timeAnimationDmg);
        sr.color = Color.white;
        yield break;
    }
}

using System.Collections.Generic;
using UnityEngine;

public class ParallaxManager : MonoBehaviour
{
    public List<ParallaxGameObject> parallaxList;

    void Start()
    {
        // Proteção para evitar erros de índice vazio no primeiro frame
        if (parallaxList != null && parallaxList.Count >= 2)
        {
            parallaxList[0].isActive = true;
            if (parallaxList[0].parallaxScript != null) parallaxList[0].parallaxScript.enabled = true;

            parallaxList[1].isActive = false;
            if (parallaxList[1].parallaxScript != null) parallaxList[1].parallaxScript.enabled = false;
        }
    }

    // TESTE ADICIONAL: Vamos ver se ele pelo menos detecta a ENTRADA
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log(" -> DETECTOU ENTRADA NO TRIGGER! <- ");
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        // Se NADA aparecer no console, o problema é 100% físico ou de inicialização
        Debug.Log($"Algo saiu do trigger: {other.gameObject.name} na layer {LayerMask.LayerToName(other.gameObject.layer)}");

        if (!other.CompareTag("Player")) return;

        Debug.Log("O Player saiu do Trigger! Atualizando estados...");

        for (int i = 0; i < parallaxList.Count; i++)
        {
            if (parallaxList[i].areaCollison == other)
            {
                parallaxList[i].isActive = !parallaxList[i].isActive;

                if (parallaxList[i].parallaxScript != null)
                {
                    parallaxList[i].parallaxScript.enabled = parallaxList[i].isActive;
                    Debug.Log($"Parallax do índice {i} alterado para: {parallaxList[i].isActive}");
                }
                break;
            }
        }
    }
}

[System.Serializable]
public class ParallaxGameObject
{
    public GameObject parallaxObj;
    public ParallaxScrollingBG parallaxScript;
    public Collider2D areaCollison;
    public bool isActive;
}
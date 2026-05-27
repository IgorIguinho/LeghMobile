using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;

using UnityEngine;
using UnityEngine.SceneManagement;

public class ConfigOnFase : MonoBehaviour
{

    public GameObject configGroup;
    public GameObject buttonGroup;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OpenConfigButton(bool isOpen)
    {
        configGroup.SetActive(isOpen);
        buttonGroup.SetActive(!isOpen);
        Time.timeScale = isOpen ? 0 : 1;
    }

    public void ReturnMenu(string scene)
    {
        Time.timeScale = 1;
        SceneManager.LoadScene(scene);
    }
}

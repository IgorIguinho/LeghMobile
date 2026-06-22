using System;
using System.Collections;
using TMPro;
using Unity.Android.Gradle;
using UnityEngine;

public class TypeTextAnimation : MonoBehaviour
{
    public Action TypeFinished; // Event to notify when typing animation is finished

    public float delay = 0.1f; // Delay between each character
    private WaitForSeconds waitDelay;
    public TextMeshProUGUI textMeshPro; // Reference to the TextMeshProUGUI component

    public string fullText;

    Coroutine coroutine;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        waitDelay = new WaitForSeconds(delay);
    }

    public void StartTyping()
    {
        coroutine = StartCoroutine(TypingAnimation());
    }

    IEnumerator TypingAnimation()
    {
        textMeshPro.text = fullText;
        textMeshPro.maxVisibleCharacters = 0; // Start with no characters visible
        for (int i = 0; i <= fullText.Length; i++)
        {
            textMeshPro.maxVisibleCharacters = i; // Update the number of visible characters
            yield return waitDelay; // Wait for the specified delay before showing the next character
        }

        TypeFinished?.Invoke(); // Invoke the event to notify that typing animation is finished

    }

    public void Skip()
    {
        StopCoroutine(coroutine);
        textMeshPro.maxVisibleCharacters = fullText.Length; // Show all characters immediately
    }
}

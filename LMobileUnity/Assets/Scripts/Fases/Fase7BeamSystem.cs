using System.Collections;
using UnityEngine;

public class Fase7BeamSystem : MonoBehaviour
{
    public static Fase7BeamSystem Instance { get; private set; }

    [System.Serializable]
    public struct BeamSection
    {
        public string name;
        public float minY;
        public float maxY;
        public GameObject warningVisual; // warning indicator
        public GameObject beamVisual;    // damage beam visual
    }

    [Header("Sections")]
    public BeamSection[] sections = new BeamSection[3]; // 0: Cima, 1: Meio, 2: Baixo

    [Header("Timings")]
    public float beamInterval = 6f;
    public float chargeDuration = 1.2f;
    public float dischargeDuration = 0.8f;

    [Header("Damage")]
    public int beamDamage = 20;

    private Coroutine loopCoroutine;
    private bool isRunning = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Ensure all visuals are deactivated at start
        foreach (var sec in sections)
        {
            if (sec.warningVisual != null) sec.warningVisual.SetActive(false);
            if (sec.beamVisual != null) sec.beamVisual.SetActive(false);
        }
    }

    public void StartSystem()
    {
        if (isRunning) return;
        isRunning = true;
        loopCoroutine = StartCoroutine(BeamSystemLoop());
    }

    public void StopSystem()
    {
        isRunning = false;
        if (loopCoroutine != null)
        {
            StopCoroutine(loopCoroutine);
            loopCoroutine = null;
        }

        // Deactivate all visuals
        foreach (var sec in sections)
        {
            if (sec.warningVisual != null) sec.warningVisual.SetActive(false);
            if (sec.beamVisual != null) sec.beamVisual.SetActive(false);
        }
    }

    private IEnumerator BeamSystemLoop()
    {
        yield return new WaitForSeconds(beamInterval * 0.5f); // initial delay

        while (isRunning)
        {
            int randomSection = Random.Range(0, sections.Length);
            yield return StartCoroutine(TriggerSectionRoutine(randomSection));
            yield return new WaitForSeconds(beamInterval);
        }
    }

    public IEnumerator TriggerSectionRoutine(int sectionIndex, System.Action onComplete = null)
    {
        if (sectionIndex < 0 || sectionIndex >= sections.Length)
        {
            onComplete?.Invoke();
            yield break;
        }

        BeamSection section = sections[sectionIndex];

        // 1. Charge (Warning / Signaling)
        if (section.warningVisual != null)
        {
            section.warningVisual.SetActive(true);
            // Quick flash logic using a coroutine helper or material blink can be simulated, 
            // or simply toggling it or letting its own animator handle flashing.
        }

        yield return new WaitForSeconds(chargeDuration);

        if (section.warningVisual != null)
        {
            section.warningVisual.SetActive(false);
        }

        // 2. Discharge (Damage Beam active)
        if (section.beamVisual != null)
        {
            section.beamVisual.SetActive(true);
        }

        // Apply Damage based on Y coordinates
        CheckAndApplyDamage(sectionIndex);

        yield return new WaitForSeconds(dischargeDuration);

        if (section.beamVisual != null)
        {
            section.beamVisual.SetActive(false);
        }

        onComplete?.Invoke();
    }

    private void CheckAndApplyDamage(int sectionIndex)
    {
        float minY = sections[sectionIndex].minY;
        float maxY = sections[sectionIndex].maxY;

        // Player check
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null && playerObj.activeInHierarchy)
        {
            float py = playerObj.transform.position.y;
            if (py >= minY && py <= maxY)
            {
                PlayerStats stats = playerObj.GetComponent<PlayerStats>();
                if (stats != null)
                {
                    stats.TakeDmg(beamDamage);
                }
            }
        }

        // NPC check
        Fase7NPC npc = FindFirstObjectByType<Fase7NPC>();
        if (npc != null && npc.gameObject.activeInHierarchy)
        {
            float ny = npc.transform.position.y;
            if (ny >= minY && ny <= maxY)
            {
                npc.TakeDamage(beamDamage);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
        foreach (var sec in sections)
        {
            float height = sec.maxY - sec.minY;
            float centerY = sec.minY + height * 0.5f;
            Gizmos.DrawWireCube(new Vector3(transform.position.x, centerY, 0f), new Vector3(30f, height, 0.1f));
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Fase7LevelManager : MonoBehaviour
{
    public static Fase7LevelManager Instance { get; private set; }

    [Header("Grids/Terrains")]
    public GameObject grid0Group;
    public GameObject grid1;
    public GameObject grid2;
    public GameObject grid3;
    public GameObject grid4;

    public GameObject spawnGroup;

    [Header("NPC & Player")]
    public Fase7NPC npc;
    public Transform playerTransform;
    public Transform[] playerSpawnPoints = new Transform[4]; // Spawn points for each grid

    [Header("Wave Configs")]
    public Fase7WaveConfig level1WaveConfig;
    public Fase7WaveConfig level2WaveConfig;
    public Fase7WaveConfig level3WaveConfig;
    public SpawnWaveInfo[] spawnWaveInfos; // Spawn locations for enemies

    [Header("Boss Config")]
    public Fase7Boss boss;
    public Slider bossHealthSlider;

    [Header("Transitions")]
    public Image fadeOverlay;
    public float fadeDuration = 0.8f;

    [Header("Current Progress (Debug)")]
    [SerializeField] private int currentLevel = 1;
    [SerializeField] private int currentWaveIndex = 0;

    [Header("UI")]
    public TextMeshProUGUI levelUI;
    public TextMeshProUGUI missingEnemies;
    int enemiesDefeated = 0;

    private List<GameObject> activeEnemies = new List<GameObject>();
    private bool isSpawningWave = false;

    private FollowCam followCam;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // Find player if not assigned
        if (playerTransform == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) playerTransform = p.transform;
        }
        followCam = Camera.main.GetComponent<FollowCam>();

        // Initialize grid activations
        levelUI.text = "";
        missingEnemies.text = "";
        if (grid0Group != null) grid0Group.SetActive(true);
        if (grid1 != null) grid1.SetActive(false);
        if (grid2 != null) grid2.SetActive(false);
        if (grid3 != null) grid3.SetActive(false);
        if (grid4 != null) grid4.SetActive(false);
        if (spawnGroup != null) spawnGroup.SetActive(false);
    }

    public void OnDialogueStartFinished()
    {
        
       StartCoroutine(OnDialogueStartFinishedCoroutine());
    }

    IEnumerator OnDialogueStartFinishedCoroutine()
    {
        StartCoroutine(TransitionRoutine(1));

        yield return new WaitForSeconds(fadeDuration);

        if (grid0Group != null) grid0Group.SetActive(false);
        if (spawnGroup != null) spawnGroup.SetActive(true);
        // Fade in dark overlay
        
    }

    public void StartLevel(int level)
    {
        currentLevel = level;
        currentWaveIndex = 0;
        activeEnemies.Clear();
        enemiesDefeated=0;

        followCam.MaxX = 0.5f;
        // Manage grids
        if (grid1 != null) grid1.SetActive(level == 1);
        if (grid2 != null) grid2.SetActive(level == 2);
        if (grid3 != null) grid3.SetActive(level == 3);
        if (grid4 != null) grid4.SetActive(level == 4);

        // Position player
        if (playerTransform != null && playerSpawnPoints.Length >= level && playerSpawnPoints[level - 1] != null)
        {
            playerTransform.position = playerSpawnPoints[level - 1].position;
        }

        // Initialize NPC
        if (npc != null)
        {
            npc.InitializeStage(level);
        }

        // Manage systems based on Level
        if (level == 3)
        {
            if (Fase7BeamSystem.Instance != null)
            {
                Fase7BeamSystem.Instance.StartSystem();
            }
        }
        else if (level != 4)
        {
            if (Fase7BeamSystem.Instance != null)
            {
                Fase7BeamSystem.Instance.StopSystem();
            }
        }

        // Start gameplay logic
        if (level == 4)
        {
            StartBossFight();
            levelUI.text = null;
            missingEnemies.text = null;
        }
        else
        {
            StartCoroutine(WaveLoopRoutine());
            UpdateHud();
        }
    }

    private IEnumerator WaveLoopRoutine()
    {
        Fase7WaveConfig currentConfig = GetWaveConfigForLevel(currentLevel);
        if (currentConfig == null || currentConfig.waves.Count == 0)
        {
            TransitionToNextLevel();
            yield break;
        }

        while (currentWaveIndex < currentConfig.waves.Count)
        {
            yield return new WaitForSeconds(2.0f); // Rest time before wave start
            yield return StartCoroutine(SpawnWave(currentConfig.waves[currentWaveIndex], currentLevel - 1));

            // Wait for all enemies in the current wave to be defeated
            while (activeEnemies.Count > 0)
            {
                yield return new WaitForSeconds(0.5f);
            }

            currentWaveIndex++;
        }

        // All waves in this level are completed! Transition to next.
        TransitionToNextLevel();
    }

    private Fase7WaveConfig GetWaveConfigForLevel(int level)
    {
        if (level == 1) return level1WaveConfig;
        if (level == 2) return level2WaveConfig;
        if (level == 3) return level3WaveConfig;
        return null;
    }

    private IEnumerator SpawnWave(WaveInfo wave, int currentLevel)
    {
        isSpawningWave = true;
        int spawnedCount = 0;
        Transform[] spawnPoints = (currentLevel < spawnWaveInfos.Length) ? spawnWaveInfos[currentLevel].spawnPoints : null;

        while (spawnedCount < wave.totalEnemiesToSpawn)
        {
            if (wave.enemyPrefabs == null || wave.enemyPrefabs.Count == 0) yield break;
            if (spawnPoints == null || spawnPoints.Length == 0) yield break;

            // Pick random enemy and random spawn point
            GameObject prefab = wave.enemyPrefabs[Random.Range(0, wave.enemyPrefabs.Count)];

            SpriteRenderer sr = prefab.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.color = Color.white; // Reset color to white
            }
      
            Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];

            GameObject enemy = null;
            if (PoolManager.Instance != null)
            {
                enemy = PoolManager.Instance.Get(prefab, spawnPoint.position, Quaternion.identity);
            }
            else
            {
                enemy = Instantiate(prefab, spawnPoint.position, Quaternion.identity);
            }

            if (enemy != null)
            {
                RegisterEnemy(enemy);
                AssignTargetToEnemy(enemy);
            }

            spawnedCount++;
            yield return new WaitForSeconds(wave.spawnInterval);
        }

        isSpawningWave = false;
    }

    private void RegisterEnemy(GameObject enemy)
    {
        EnemyStats stats = enemy.GetComponent<EnemyStats>();
        if (stats != null)
        {
            stats.OnEnemyDeath += HandleEnemyDeath;
        }
        activeEnemies.Add(enemy);
    }

    private void HandleEnemyDeath(EnemyStats stats)
    {
        if (stats == null) return;
        stats.OnEnemyDeath -= HandleEnemyDeath;
        activeEnemies.Remove(stats.gameObject);
        enemiesDefeated++;
        UpdateHud();
        if (PoolManager.Instance != null)
        {
            PoolManager.Instance.Release(stats.gameObject);
        }
        else
        {
            Destroy(stats.gameObject);
        }
    }

    private void AssignTargetToEnemy(GameObject enemy)
    {
        Transform target = playerTransform;

        // In level 1, target is randomly Player or NPC
        if (currentLevel == 1 && npc != null)
        {
            target = Random.value > 0.5f ? playerTransform : npc.transform;
        }

        // Apply target override
        AtiradorEnemy atirador = enemy.GetComponent<AtiradorEnemy>();
        if (atirador != null) atirador.targetOverride = target;

        RangedEnemy ranged = enemy.GetComponent<RangedEnemy>();
        if (ranged != null) ranged.targetOverride = target;

        ObserverEnemy observer = enemy.GetComponent<ObserverEnemy>();
        if (observer != null) observer.targetOverride = target;
    }

    private void TransitionToNextLevel()
    {
        if (currentLevel < 4)
        {
            StartCoroutine(TransitionRoutine(currentLevel + 1));
        }
        else
        {
            // All stages complete
            if (HudManagerOnFase.Instance != null)
            {
                HudManagerOnFase.Instance.OpenWinScreen();
            }
        }
    }

    private IEnumerator TransitionRoutine(int targetLevel)
    {
        // Fade in dark overlay
        if (fadeOverlay != null)
        {
            fadeOverlay.gameObject.SetActive(true);
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                Color col = fadeOverlay.color;
                col.a = Mathf.Lerp(0f, 1f, elapsed / fadeDuration);
                fadeOverlay.color = col;
                yield return null;
            }
        }

        // Initialize the new Level/Grid while screen is dark
        StartLevel(targetLevel);

        yield return new WaitForSeconds(0.5f); // Short wait

        // Fade out dark overlay
        if (fadeOverlay != null)
        {
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                Color col = fadeOverlay.color;
                col.a = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
                fadeOverlay.color = col;
                yield return null;
            }
            fadeOverlay.gameObject.SetActive(false);
        }
    }

    private void StartBossFight()
    {
        if (boss != null)
        {
            boss.gameObject.SetActive(true);
            boss.bossHealthSlider = bossHealthSlider;
            boss.InitializeBoss();
        }

        if (Fase7BeamSystem.Instance != null)
        {
            // The boss uses the beam system for its environmental attacks
            Fase7BeamSystem.Instance.StopSystem(); // ensures it doesn't trigger random auto-beams, only boss manual ones
        }
    }
    
    void UpdateHud()
    {
        if (levelUI != null)
        {
            levelUI.text = $"Level {currentLevel}";
        }
        if (missingEnemies != null)
        {
            Fase7WaveConfig currentConfig = GetWaveConfigForLevel(currentLevel);
            int remainingEnemies = currentConfig.totalEnemiesToSpawnInAllWaves - enemiesDefeated;
            missingEnemies.text = $"Enemies {remainingEnemies}";
        }
    }

    public void OnBossDefeated()
    {
        if (HudManagerOnFase.Instance != null)
        {
            HudManagerOnFase.Instance.OpenWinScreen();
        }
    }
}

[System.Serializable]
public struct SpawnWaveInfo
{
    public Transform[] spawnPoints;
}
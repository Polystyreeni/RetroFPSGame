using System.Collections;
using UnityEngine;
using System;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    // Instance
    public static GameManager Instance = null;

    // Stuff to spawn when level is loaded
    public GameObject playerPrefab = null;
    private string UiScene = "SceneUI";

    // Level information
    private string currentLevel = string.Empty;

    public string CurrentLevel { get { return currentLevel; } }

    public int maxKills { get; private set; }
    public int maxSecrets { get; private set; }

    private float timeScale = 1f;
    public float TimeScale { get { return timeScale; } private set { timeScale = value; Time.timeScale = value; } }

    // GameState information
    private bool bLevelInfoLoaded = false;
    private bool bLoadPlayerFromSave = false;
    public int currentKills { get; private set; }
    public int currentSecrets { get; private set; }

    // Default value of difficulty (corresponds DOOM ulta-violence?)
    [SerializeField] private EnumContainer.DIFFICULTY difficultyLevel = EnumContainer.DIFFICULTY.HARD;
    public EnumContainer.DIFFICULTY DifficultyLevel { get { return difficultyLevel; } set { difficultyLevel = value; } }

    // Events / actions
    public event Action OnLoadObjects;
    public event Action OnGameStateLoaded;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(this.gameObject);
        }

        else if (Instance != this)
        {
            Destroy(this);
            return;
        }
            
        SceneManager.sceneLoaded += GameManagerSceneLoad;
        SceneManager.LoadScene(UiScene, LoadSceneMode.Additive);
    }

    void GameManagerSceneLoad(Scene scene, LoadSceneMode mode)
    {
        Debug.Log("GameManager: Scene loaded!");

        // Load menu scene
        Debug.Log("Loaded scene: " + scene.name);
        if(!IsMenuScene() && scene.name != UiScene)
        {
            currentLevel = SceneManager.GetActiveScene().name;

            Transform spawnPos;
            if (GameObject.FindGameObjectWithTag("Respawn") != null)
                spawnPos = GameObject.FindGameObjectWithTag("Respawn").transform;

            else
                spawnPos = transform;

            Instantiate(playerPrefab, spawnPos.position, spawnPos.rotation);
            InitializeLevelStats();
        }

        else
        {
            bLevelInfoLoaded = true;
        }
    }

    public bool IsMenuScene()
    {
        string level = SceneManager.GetActiveScene().name;
        if (level == "MainMenu" || level == "LevelCompleteMenu")
            return true;

        return false;
    }

    void InitializeLevelStats()
    {
        maxKills = GameObject.FindGameObjectsWithTag("Enemy").Length;
        maxSecrets = GameObject.FindGameObjectsWithTag("Secret").Length;
        bLevelInfoLoaded = true;
        Debug.Log("GameManager: Level info Initialized");
        if(bLoadPlayerFromSave)
        {
            //LoadPlayerStats();
            //Invoke(nameof(LoadPlayerStats), 1f);
            //Debug.Log("GameManager: Player Stats loaded!");
            Invoke(nameof(AutoSave), 1f);
        }
    }

    private void Start()
    {
        SaveManager.Instance.OnGameSaved += SaveGameState;
        SaveManager.Instance.OnPreGameLoaded += LoadStats;
        SaveManager.Instance.OnGameLoaded += LoadObjects;
    }

    private void OnDestroy()
    {
        SaveManager.Instance.OnGameSaved -= SaveGameState;
        SaveManager.Instance.OnGameLoaded -= LoadObjects;
        SaveManager.Instance.OnPreGameLoaded -= LoadStats;
    }

    void SaveGameState()
    {
        SaveManager.Instance.gameState.levelData.level = currentLevel;
        SaveManager.Instance.gameState.levelData.date = DateTime.Now.ToString("MMM ddd d HH:mm yyyy");
        SaveManager.Instance.gameState.levelData.kills = currentKills;
        SaveManager.Instance.gameState.levelData.secrets = currentSecrets;
        SaveManager.Instance.gameState.levelData.gameDifficulty = difficultyLevel;
        Debug.Log("GameManager: Saved gameState information succesfully");
    }

    void LoadObjects()
    {
        var objectList = SaveManager.Instance.gameState.objectList;
        if (objectList.Count <= 0)
            return;

        for(int i = 0; i < objectList.Count; i++)
        {
            GameObject prefab = Resources.Load(objectList[i].prefabName) as GameObject;
            Vector3 pos = objectList[i].transformData.position;
            Quaternion rot = Quaternion.Euler(objectList[i].transformData.rotation);
            Vector3 vel = objectList[i].velocity;
            GameObject spawnedObj = Instantiate(prefab, pos, rot);

            Rigidbody rb = spawnedObj.GetComponent<Rigidbody>();
            if (rb != null)
                rb.velocity = vel;
        }

        OnLoadObjects?.Invoke();
    }

    void LoadStats()
    {
        currentKills = SaveManager.Instance.gameState.levelData.kills;
        currentSecrets = SaveManager.Instance.gameState.levelData.secrets;
        difficultyLevel = SaveManager.Instance.gameState.levelData.gameDifficulty;
        Debug.Log("GameManager: Loaded gameState information succesfully");
    }

    void ResetStats()
    {
        currentKills = 0;
        currentSecrets = 0;
        Debug.Log("GameManager: Reset map information");
    }

    public void LoadGame(string saveName)
    {
        bool loadSuccesful = SaveManager.Instance.LoadGame(saveName);
        if (loadSuccesful)
        {
            string sceneToLoad = SaveManager.Instance.gameState.levelData.level;
            LoadLevel(sceneToLoad, true);
        }
    }

    public void LoadLevel(string level, bool loadSave = false)
    {
        StartCoroutine(LoadSavedLevel(level, loadSave));
    }

    IEnumerator LoadSavedLevel(string sceneToLoad, bool loadSave)
    {
        GameObject loadScreen = UIManager.Instance.loadScreen;
        Slider loadBar = loadScreen.GetComponentInChildren<Slider>();
        UIManager.Instance.EnableLoadScreen(true);

        if (!bLevelInfoLoaded)
        {
            while (!bLevelInfoLoaded)
                yield return null;
        }
   
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneToLoad, LoadSceneMode.Single);
        while (!operation.isDone)
        {
            loadBar.value = operation.progress;
            yield return null;
        }

        if (loadSave)
            SaveManager.Instance.InvokeGameLoad();

        else if(bLoadPlayerFromSave)
        {
            ResetStats();
            LoadPlayerStats();
            bLoadPlayerFromSave = false;
        }         

        else
            ResetStats();

        OnGameStateLoaded?.Invoke();
        Debug.Log("GameManager: Gamestate has been loaded!");

        UIManager.Instance.EnableLoadScreen(false);
    }

    // Public methods
    public void GameManagerSaveGame(int saveIndex, string saveName, string displayName)
    {
        SaveManager.Instance.gameState.levelData.saveSlot = saveIndex;
        SaveManager.Instance.gameState.levelData.displayName = displayName;
        SaveManager.Instance.SaveGame(saveName);
    }

    public void IncrementKillCount()
    {
        currentKills++;
    }

    public void IncrementSecretCount()
    {
        currentSecrets++;
    }

    public void SetTimeScale(float scale)
    {
        Time.timeScale = scale;
    }

    public void SetLoadPlayerFromSave(bool value)
    {
        bLoadPlayerFromSave = value;
    }

    public void SavePlayerStats()
    {
        SaveManager.Instance.SaveGame(string.Empty, false);
    }

    public void LoadPlayerStats()
    {
        if (PlayerContainer.Instance != null)
        {
            PlayerContainer.Instance.LoadPlayerStats();
            Debug.Log("GameManager: Load Player Stats");
        }   
    }

    public void AutoSave()
    {
        UIManager.Instance.AutoSave();
    }
}

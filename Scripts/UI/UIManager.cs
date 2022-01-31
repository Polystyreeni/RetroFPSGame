/* A Class to handle In-game UI and menus, as well as saving
 * 
 *   Used UI PlayerPrefs (Stored in reqistry):
 *   == Graphics ==
 * - HS_GrahpicsQuality
 * 
 *  == Input ==
 * - HS_MouseSpeed
 * - HS_FOV
 * 
 *  == Audio ==
 * - HS_AudioVol
 * - HS_MusicVol
 * 
 * 
 * 
 * 
 */

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance = null;

    // HUD
    [SerializeField] private GameObject HUD = null;
    [SerializeField] private TextMeshProUGUI ammoText = null;
    [SerializeField] private TextMeshProUGUI healthText = null;
    [SerializeField] private GameObject healthBar = null;
    [SerializeField] private TextMeshProUGUI messageBox = null;
    [SerializeField] private Slider throwSlider = null;
    [SerializeField] private GameObject crossHair = null;
    [SerializeField] private GameObject healthIndicator = null;
    [SerializeField] private GameObject inventoryHud = null;
    [SerializeField] private GameObject abilityBar = null;
    [SerializeField] private GameObject abilityIcon = null;
    [SerializeField] private List<Sprite> abilityImages = null;
    [SerializeField] private Sprite[] fullScreenFireFrames = null;
    UnityEngine.Coroutine spriteCoroutine;
    UnityEngine.Coroutine healthIndicatorCoroutine;

    // Menus
    [SerializeField] private GameObject mainMenu = null;
    [SerializeField] private GameObject newGameMenu = null;
    [SerializeField] private GameObject pauseMenu = null;
    [SerializeField] private GameObject saveLoadMenu = null;
    [SerializeField] private GameObject optionsMenu = null;
    [SerializeField] private GameObject eventSystem = null;
    public GameObject loadScreen = null;

    // Options menu items
    [SerializeField] private TextMeshProUGUI sensitivityText = null;
    [SerializeField] private TextMeshProUGUI fieldOfViewText = null;

    // Buttons
    [SerializeField] private GameObject newSaveButton = null;
    [SerializeField] private GameObject saveLoadButton = null;
    [SerializeField] private GameObject newSaveInput = null;
    [SerializeField] private GameObject saveScrollContent = null;

    // Variables
    [SerializeField] private int messageBoxMax = 3;
    [SerializeField] private float messageDuration = 3f;

    // Don't edit these
    private int currentSaveIndex = 0;
    private float inventoryFontSize = 0f;
    private bool abilityFull = false;
    private string selectedLevel = string.Empty;
    private Camera playerCamera = null;
    private AudioSource audioSource = null;

    public bool BGamePaused { get; private set; }
    public bool BSaveMenuOpen { get; set; }
    public bool BLoadMenuOpen { get; set; }

    private bool bPlayerDead = false;
    public bool BPlayerDead
    {
        get { return bPlayerDead; }
        set { bPlayerDead = value; if (value) { DisplayMessage("Press Enter to Restart Level"); } }
    }

    public event Action OnSensitivityChanged;

    private Queue<string> messageQueue = new Queue<string>();
    [SerializeField] private Dictionary<string, string> saveList = new Dictionary<string, string>();
    
    private void Awake()
    {
        if (Instance == null)
            Instance = this;

        else if (Instance != this)
        {
            Debug.Log("Destroying this UI Manager");
            Destroy(this.gameObject);
        }

        audioSource = GetComponent<AudioSource>();

        SceneManager.sceneLoaded += UiManagerSceneLoaded;
        DontDestroyOnLoad(this.gameObject);
        DontDestroyOnLoad(eventSystem);
    }

    private void Start()
    {
        throwSlider.gameObject.SetActive(false);
        pauseMenu.SetActive(false);
        saveLoadMenu.SetActive(false);
        newGameMenu.SetActive(false);
        optionsMenu.SetActive(false);
        newSaveInput.SetActive(false);
        loadScreen.SetActive(false);
        inventoryHud.SetActive(false);
        BSaveMenuOpen = false;
        BLoadMenuOpen = false;

        inventoryFontSize = inventoryHud.GetComponentInChildren<TextMeshProUGUI>().fontSize;

        newSaveInput.GetComponent<TMP_InputField>().onSubmit.AddListener((value) =>
        {
            CreateNewSaveFile(value);
        });

        InitializeMenuItems();

        //InitializeSaveButtons();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= UiManagerSceneLoaded;
    }

    void InitializeMenuItems()
    {
        if (GameManager.Instance.IsMenuScene())
        {
            HUD.SetActive(false);
            mainMenu.SetActive(true);
        }
    }

    void UiManagerSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
    {
        Debug.Log("UiManager: Scene loaded: " + scene.name);
        if (scene.name == "SceneUI")
            return;

        UnPauseGame();
        if (scene.name == "MainMenu")
        {
            Debug.Log("UiManager: MainMenu Loaded, disabling hud");
            HUD.SetActive(false);
            mainMenu.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Invoke(nameof(DisableHud), 0.1f);
        }

        else if(scene.name == "LevelCompleteMenu")
        {
            mainMenu.SetActive(false);
            HUD.SetActive(false);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        else
        {
            mainMenu.SetActive(false);
            BPlayerDead = false;
            ShowThrowBar(false);
            selectedLevel = GameManager.Instance.CurrentLevel;
        }
    }

    void DisableHud()
    {
        HUD.SetActive(false);
    }

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.Escape))
        {
            PauseButtonPressed();
            return;
        }

        if(Input.GetButtonDown("Submit") && BPlayerDead)
        {
            if (string.IsNullOrEmpty(selectedLevel))
                selectedLevel = GameManager.Instance.CurrentLevel;

            StartLevel(selectedLevel);
            return;
        }
    }

    private void PauseButtonPressed()
    {
        // TODO: Proper implementation
        if (SceneManager.GetActiveScene().name == "LevelCompleteMenu")
            return;

        // Don't allow leaving menu, if save input is active
        if (newSaveInput.activeInHierarchy)
            return;

        if (BSaveMenuOpen || BLoadMenuOpen)
        {
            if (GameManager.Instance.IsMenuScene())
            {
                mainMenu.SetActive(true);
            }

            else
            {
                pauseMenu.SetActive(true);
            }

            saveLoadMenu.SetActive(false);
            BSaveMenuOpen = false;
            BLoadMenuOpen = false;
        }

        else if (newGameMenu.activeInHierarchy)
        {
            if (GameManager.Instance.IsMenuScene())
            {
                mainMenu.SetActive(true);
                newGameMenu.SetActive(false);
                GameObject button = mainMenu.GetComponentInChildren<Button>().gameObject;
                EventSystem.current.SetSelectedGameObject(button);
            }

            else
            {
                pauseMenu.SetActive(true);
                newGameMenu.SetActive(false);
                GameObject button = mainMenu.GetComponentInChildren<Button>().gameObject;
                EventSystem.current.SetSelectedGameObject(button);
            }
        }

        else if (optionsMenu.activeInHierarchy)
        {
            if (GameManager.Instance.IsMenuScene())
            {
                mainMenu.SetActive(true);
                optionsMenu.SetActive(false);
            }

            else
            {
                pauseMenu.SetActive(true);
                optionsMenu.SetActive(false);
            }
        }

        else
        {
            if (mainMenu.activeInHierarchy)
                return;

            if (BGamePaused)
                UnPauseGame();

            else
                PauseGame();
        }
    }

    public void PauseGame()
    {
        Time.timeScale = 0;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        pauseMenu.SetActive(true);
        HUD.SetActive(false);
        BGamePaused = true;
    }
    
    public void UnPauseGame()
    {
        // TODO: Store previous timescale, incase we use a different timescale for some abilities
        Time.timeScale = GameManager.Instance.TimeScale;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        pauseMenu.SetActive(false);
        saveLoadMenu.SetActive(false);
        BSaveMenuOpen = false;
        BLoadMenuOpen = false;
        BGamePaused = false;
        HUD.SetActive(true);
    }

    #region Saving & Loading
    public void InitializeLoadButtons()
    {
        string path = GetSavePath(); 

        var info = new DirectoryInfo(path);
        var numSaveFiles = GetSaveFiles(info);

        // Clear old menu
        ClearSaveLoadButtons();
        
        if(BSaveMenuOpen && !BPlayerDead)
        {
            GameObject newButtonObject = Instantiate(newSaveButton);
            newButtonObject.transform.SetParent(saveScrollContent.transform, false);
            newButtonObject.GetComponent<Button>().onClick.AddListener(() =>
            {
                OnNewSaveButtonPressed();
            });
        }

        // Clear saveList, create and load a new one
        UpdateSaveList();
        foreach(KeyValuePair<string, string> kvp in saveList)
        {
            Debug.LogFormat("UIManager: Key = {0}, Value = {1}", kvp.Key, kvp.Value);
        }

        if (numSaveFiles != null)
        {
            foreach(KeyValuePair<string, string> kvp in saveList)
            {
                GameObject buttonObject = Instantiate(saveLoadButton);
                buttonObject.transform.SetParent(saveScrollContent.transform, false);

                buttonObject.GetComponent<Button>().onClick.AddListener(() =>
                {
                    SaveLoadButtonPressed(kvp.Key);
                });

                buttonObject.GetComponentInChildren<TextMeshProUGUI>().text = kvp.Value;

            }
        }
    }

    string GetSavePath()
    {
        string path = Application.dataPath + "/saves/";
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);

        return path;
    }

    public void UpdateSaveList()
    {
        bool bListUpdate = SaveManager.Instance.LoadSaveGameList();
        if (bListUpdate)
        {
            saveList.Clear();
            saveList = SaveManager.Instance.saveGameList;
        }
    }

    static FileInfo[] GetSaveFiles(DirectoryInfo d)
    {
        List<FileInfo> saves = new List<FileInfo>();
        FileInfo[] fis = d.GetFiles();
        for (int i = 0; i < fis.Length; i++)
        {
            if (fis[i].Extension.Contains("xml"))
                saves.Add(fis[i]);
        }

        return saves.ToArray();
    }

    void CreateNewSaveFile(string saveName)
    {
        if (saveName == string.Empty)
        {
            newSaveInput.SetActive(false);
            return;
        }

        if(saveList.ContainsKey(saveName))
        {
            int nameIndex = 1;
            while(nameIndex < 500)  // TODO: Failsafe, 500 saves should be more than enough, but limit saves elsewhere as well
            {
                string newSaveName = saveName + " " + nameIndex.ToString();
                if (saveList.ContainsKey(newSaveName))  
                {
                    nameIndex++;
                    continue;
                }

                else
                {
                    saveName = newSaveName;
                    break;
                }
            }
        }
            
        string path = Application.dataPath + "/saves/";
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);

        var info = new DirectoryInfo(path);
        var numSaveFiles = GetSaveFiles(info);

        int index = 1;
        if (numSaveFiles != null)
            index = numSaveFiles.Length; 

        Debug.Log("New save has been added");
        NewSaveButtonPressed(index, saveName);
        newSaveInput.SetActive(false);

        BSaveMenuOpen = false;
        saveLoadMenu.SetActive(false);
        pauseMenu.SetActive(true);
    }

    void ReWriteSaveFile(string saveDispName, string saveName)
    {
        newSaveInput.SetActive(false);
        saveLoadMenu.SetActive(false);
        pauseMenu.SetActive(true);
        BSaveMenuOpen = false;

        //string saveName = "save" + index.ToString() + ".xml";
        
        // TODO: Possibly obsolete
        int index = 0;
        saveList[saveName] = saveDispName;

        SaveManager.Instance.StoreSaveGameList(saveList);
        GameManager.Instance.GameManagerSaveGame(index, saveName, saveDispName);

        TMP_InputField inputField = newSaveInput.GetComponent<TMP_InputField>();
        inputField.onSubmit.RemoveAllListeners();
        inputField.onSubmit.AddListener((value) =>
        {
            CreateNewSaveFile(value);
        });
    }

    void OnNewSaveButtonPressed()
    {
        newSaveInput.SetActive(true);
        ClearSaveLoadButtons();
    }

    void NewSaveButtonPressed(int numSaveFiles, string displaySaveName)
    {
        int index = numSaveFiles + 1;
        string saveName = "save" + index.ToString() + ".xml";

        saveList.Add(saveName, displaySaveName);
        Debug.Log("Created dictionary entry with key " + saveName + " value " + displaySaveName);
        SaveManager.Instance.StoreSaveGameList(saveList);
        GameManager.Instance.GameManagerSaveGame(index, saveName, displaySaveName);
    }

    public void AutoSave()
    {
        int index = 0;
        string saveName = "saveauto.xml";
        string displayName = "Auto " + DateTime.Now.ToString("MMM ddd d HH:mm yyyy");

        if (!saveList.ContainsKey(saveName))
        {
            saveList.Add(saveName, displayName);
        }

        else
        {
            saveList[saveName] = displayName;
        }

        // Make sure directory exists before autosaving!
        GetSavePath();

        SaveManager.Instance.StoreSaveGameList(saveList);
        GameManager.Instance.GameManagerSaveGame(index, saveName, displayName);
    }

    void SaveLoadButtonPressed(string saveName)
    {
        // Save Game
        //string saveName = "save" + index.ToString() + ".xml";

        if (BSaveMenuOpen)
        {
            // Reserved for autosave, don't allow editing
            if (saveName == "saveauto.xml")
                return;

            // No saving, if player is dead
            if (BPlayerDead)
                return;

            newSaveInput.SetActive(true);
            ClearSaveLoadButtons();
            newSaveInput.GetComponent<TMP_InputField>().text = saveList[saveName];

            TMP_InputField inputField = newSaveInput.GetComponent<TMP_InputField>();
            inputField.onSubmit.RemoveAllListeners();
            inputField.onSubmit.AddListener((value) =>
            {
                ReWriteSaveFile(value, saveName);
            });
        }

        // Load Game
        else if(BLoadMenuOpen)
        {
            UnPauseGame();
            Debug.Log("Attempting to load saveFile: " + saveName);
            GameManager.Instance.LoadGame(saveName);
        }
    }

    void ClearSaveLoadButtons()
    {
        Button[] buttons = saveScrollContent.GetComponentsInChildren<Button>();
        if (buttons != null)
        {
            foreach (Button btn in buttons)
            {
                Destroy(btn.gameObject);
            }
        }
    }

    public void LoadGame(int index)
    {
        string saveName = ("save" + index + ".xml").ToString();
        GameManager.Instance.LoadGame(saveName);
    }
    #endregion

    public void NewGameButtonPressed()
    {
        pauseMenu.SetActive(false);
        mainMenu.SetActive(false);

        newGameMenu.SetActive(true);
    }

    public void EpisodeSelectionPressed(int epIndex)
    {
        // TODO: Proper implementation when more levels are added
        switch(epIndex)
        {
            case 0:
                selectedLevel = "e1m1";
                break;

            default:
                break;
        }
    }

    public void OptionsButtonPressed()
    {
        pauseMenu.SetActive(false);
        mainMenu.SetActive(false);

        optionsMenu.SetActive(true);
        EventSystem.current.SetSelectedGameObject(optionsMenu.GetComponentInChildren<Slider>().gameObject);
    }

    public void SetDifficulty(int diffLevel)
    {
        EnumContainer.DIFFICULTY gameDifficulty = GetDifficulty(diffLevel);
        GameManager.Instance.DifficultyLevel = gameDifficulty;

        mainMenu.SetActive(false);
        pauseMenu.SetActive(false);
        newGameMenu.SetActive(false);

        GameManager.Instance.SetLoadPlayerFromSave(false);

        StartLevel(selectedLevel);
    }

    void StartLevel(string level)
    {
        GameManager.Instance.LoadLevel(level, false);
    }

    EnumContainer.DIFFICULTY GetDifficulty(int diffLevel)
    {
        switch(diffLevel)
        {
            case 0:
                return EnumContainer.DIFFICULTY.EASY;

            case 1:
                return EnumContainer.DIFFICULTY.NORMAL;

            case 2:
                return EnumContainer.DIFFICULTY.MEDIUM;

            case 3:
                return EnumContainer.DIFFICULTY.HARD;

            case 4:
                return EnumContainer.DIFFICULTY.INSANE;

            default:
                return EnumContainer.DIFFICULTY.HARD;
        }
    }

    public void EnableLoadScreen(bool value)
    {
        HUD.SetActive(!value);
        loadScreen.SetActive(value);

        if(value)
        {
            DisableAllMenus();
        }
    }

    void DisableAllMenus()
    {
        mainMenu.SetActive(false);
        pauseMenu.SetActive(false);
        saveLoadMenu.SetActive(false);
        optionsMenu.SetActive(false);
        newGameMenu.SetActive(false);
        BSaveMenuOpen = false;
        BLoadMenuOpen = false;
    }

    public void MainMenuButtonPressed()
    {
        StartLevel("MainMenu");
        HUD.SetActive(false);
    }

    public void QuitGame()
    {
        // TODO: Add a doom-like quote for quitting and confirming exit???
        Application.Quit();
    }
    #region MessageBox
    void UpdateMessageBox()
    {
        messageBox.text = string.Empty;
        CancelInvoke(nameof(ClearMessageBox));

        string[] arr = messageQueue.ToArray();
        for(int i = 0; i < messageQueue.Count; i++)
        {
            string message = arr[i];
            messageBox.text += (message + "\n");
        }

        Invoke(nameof(ClearMessageBox), messageDuration);
    }
    
    void ClearMessageBox()
    {
        messageBox.text = string.Empty;
        messageQueue.Clear();
    }

    public void DisplayMessage(string message)
    {
        messageQueue.Enqueue(message);
        if (messageQueue.Count > messageBoxMax)
        {
            messageQueue.Dequeue();
        }

        UpdateMessageBox();
    }
    #endregion

    public void UpdateAmmo(string text)
    {
        ammoText.text = text;
    }

    public void ShowCurrentWeapon(int index)
    {
        CancelInvoke(nameof(HideInventoryHud));

        TextMeshProUGUI currentWeaponText = null;
        TextMeshProUGUI[] textArray = inventoryHud.GetComponentsInChildren<TextMeshProUGUI>();
        if (textArray.Length <= 0)
            return;

        for(int i = 0; i < textArray.Length; ++i)
        {
            if(textArray[i].text == index.ToString())
            {
                currentWeaponText = textArray[i];
            }

            textArray[i].fontSize = inventoryFontSize;
        }

        if (currentWeaponText == null)
            return;

        currentWeaponText.fontSize *= 2;
        inventoryHud.SetActive(true);

        Invoke(nameof(HideInventoryHud), 2f);
    }

    void HideInventoryHud()
    {
        TextMeshProUGUI[] textArray = inventoryHud.GetComponentsInChildren<TextMeshProUGUI>();
        if (textArray.Length <= 0)
            return;

        for (int i = 0; i < textArray.Length; ++i)
        {
            textArray[i].fontSize = inventoryFontSize;
        }

        inventoryHud.SetActive(false);
    }

    public void UpdateAbilityBar(float value)
    {
        ProgressBar bar = abilityBar.GetComponent<ProgressBar>();
        int barVal = Mathf.RoundToInt(value);
        bar.SetCurrentValue(barVal);
    }

    public void UpdateAbilityIcon(int abilityIndex, float abilityCurrent, bool abilityActive = false)
    {
        Image abilityImg = abilityIcon.GetComponent<Image>();
        abilityImg.sprite = abilityImages[abilityIndex * 3];

        Color color = abilityImg.color;

        // TODO: Begin animation for image
        if(abilityCurrent >= 100)
        {
            color.a = 1;
            abilityFull = true;
            if(spriteCoroutine != null)
                StopCoroutine(spriteCoroutine);

            spriteCoroutine = StartCoroutine(AbilityAnimator(abilityIndex));
        }

        else
        {
            if(!abilityActive)
            {
                color.a = 0.5f;
                abilityFull = false;
            }
        }

        abilityImg.color = color;
    }

    IEnumerator AbilityAnimator(int abilityIndex)
    {
        int minIndex = abilityIndex * 3;
        int maxIndex = minIndex + 2;

        Image abilityImg = abilityIcon.GetComponent<Image>();

        while(abilityFull)
        {
            for(int i = minIndex; i <= maxIndex; i++)
            {
                abilityImg.sprite = abilityImages[i];
                yield return new WaitForSeconds(.3f);
            }

            yield return null;
        }
    }

    public void ShowThrowBar(bool bShow)
    {
        throwSlider.gameObject.SetActive(bShow);
        crossHair.gameObject.SetActive(!bShow);
    }

    public void ThrowBarSetValue(float value)
    {
        throwSlider.value = value;
    }

    public void UpdateHealth(int value)
    {
        healthText.text = value.ToString();
        if(healthBar.TryGetComponent<ProgressBar>(out ProgressBar bar))
        {
            if (value > 100)
                bar.SetMaxValue(200);

            else
                bar.SetMaxValue(100);

            bar.SetCurrentValue(value);
        }
    }

    public void HealthChangeIndication(Color c, float duration, float maxAlpha = 1.0f, EnumContainer.DAMAGETYPE damageType = EnumContainer.DAMAGETYPE.Undefined)
    {
        if (healthIndicatorCoroutine != null)
            return;

        Image img = healthIndicator.GetComponent<Image>();
        c.a = 0;
        img.color = c;
        if(damageType == EnumContainer.DAMAGETYPE.Fire)
        {
            healthIndicatorCoroutine = StartCoroutine(PlayFireEffect(img, duration, maxAlpha));
        }

        else
        {
            healthIndicatorCoroutine = StartCoroutine(IndicateHealthChange(img, duration, maxAlpha));
        }
        
    }

    IEnumerator PlayFireEffect(Image img, float duration, float maxAlpha)
    {  
        float interval = 0.05f;
        int animIndex = 0;

        Color color = img.color;
        color.a = 1;
        img.color = color;
        Sprite defaultSprite = img.sprite;

        float timeElapsed = 0f;
        while (timeElapsed < duration)
        {
            color.a = Mathf.Lerp(0.0f, maxAlpha, timeElapsed / duration);
            img.color = color;
            timeElapsed += Time.deltaTime;
            if (timeElapsed > interval)
            {
                animIndex++;
                if (animIndex >= fullScreenFireFrames.Length)
                    animIndex = 0;
                interval *= 2;
                img.sprite = fullScreenFireFrames[animIndex];
            }
        }

        timeElapsed = 0f;
        interval = 0.05f;
        while (timeElapsed < duration)
        {
            color.a = Mathf.Lerp(maxAlpha, 0.0f, timeElapsed / duration);
            img.color = color;
            timeElapsed += Time.deltaTime;
            if(timeElapsed > interval)
            {
                animIndex++;
                if (animIndex >= fullScreenFireFrames.Length)
                    animIndex = 0;
                interval *= 2;
                img.sprite = fullScreenFireFrames[animIndex];
            }

            yield return null;
        }

        color.a = 0;
        img.color = color;
        img.sprite = defaultSprite;
        healthIndicatorCoroutine = null;
    }

    IEnumerator IndicateHealthChange(Image img, float duration, float maxAlpha)
    {
        Color color = img.color;

        // Fade from transparent to visible
        float timeElapsed = 0f;
        while (timeElapsed < duration)
        {
            color.a = Mathf.Lerp(0.0f, maxAlpha, timeElapsed / duration);
            timeElapsed += Time.deltaTime;
            img.color = color;
            yield return null;
        }

        color.a = 1;
        img.color = color;

        // Fade from transparent to visible
        timeElapsed = 0f;
        while (timeElapsed < duration)
        {
            color.a = Mathf.Lerp(maxAlpha, 0.0f, timeElapsed / duration);
            timeElapsed += Time.deltaTime;
            img.color = color;
            yield return null;
        }

        color.a = 0;
        img.color = color;
        healthIndicatorCoroutine = null;
    }

    public void OnMouseSensitivityChanged(float value)
    {
        PlayerPrefs.SetFloat("HS_MouseSpeed", value);
        OnSensitivityChanged?.Invoke();

        float fc = (float)Math.Round(value * 100f) / 100f;
        sensitivityText.text = fc.ToString();
    }

    public void OnMasterVolumeChanged(float value)
    {
        PlayerPrefs.SetFloat("HS_AudioVol", value);
    }

    public void OnFieldOfViewChanged(float value)
    {
        if (playerCamera == null)
            playerCamera = Camera.main;

        PlayerPrefs.SetFloat("HS_FOV", value);

        playerCamera.fieldOfView = value;

        fieldOfViewText.text = value.ToString();
    }

    public void ButtonSound()
    {
        audioSource.Play();
    }
}

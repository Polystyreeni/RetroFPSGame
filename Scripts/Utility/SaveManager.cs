/*
 *  Saving and loading game information 
 *  Saves use xml serialization and are stored in plain xml for now
 */


using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Runtime.Serialization;
using System.Xml.Serialization;

[System.Serializable]
public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance = null;
    public event Action OnGameSaved;
    public event Action OnGameLoaded;
    public event Action OnPreGameLoaded;

    [XmlRoot("GameData")]
    public class SaveData
    {
        [Serializable]
        public struct TransformData
        {
            public Vector3 position;
            public Vector3 rotation;
            public Vector3 scale;
        }

        [Serializable]
        public struct WeaponData
        {
            public int weaponIndex;
            public int ammoClip;
            public int ammoStock;
            public int fireCounter;
        }

        [Serializable]
        public class PlayerData
        {
            public TransformData transFormData;
            public List<WeaponData> inventory = new List<WeaponData>();
            public Dictionary<int, int> ammoReserve = new Dictionary<int, int>();
            public Dictionary<int, EnumContainer.PLAYERABILITY> abilities = new Dictionary<int, EnumContainer.PLAYERABILITY>();
            public List<int> doorKeys = new List<int>();
            public int activeWeaponIndex;
            public int activeAbilityIndex;
            public int health;
            public float abilityCounter;
            public bool abilityActive;
        }

        [Serializable]
        public class EnemyData
        {
            public TransformData transformData;
            public string objectName;
            public int health;
            public EnemyMovement.ENEMY_STATE enemyState;
            public bool bSeenPlayer;
            public string targetName;
        }

        [Serializable]
        public class ObjectData
        {
            public TransformData transformData;
            public Vector3 velocity;
            public string prefabName;
        }

        [SerializeField]
        public class DoorData
        {
            public Vector3 position;
            public string objectID;
            public bool doorOpen;
            public float doorTimer;
        }

        [Serializable]
        public class LevelData
        {
            public int saveSlot;
            public int kills;
            public int secrets;
            public string level;
            public string date;
            public string displayName;
            public EnumContainer.DIFFICULTY gameDifficulty;
        }

        public List<EnemyData> enemyList = new List<EnemyData>();
        public List<ObjectData> objectList = new List<ObjectData>();
        public HashSet<string> worldObjectList = new HashSet<string>();
        public List<DoorData> doorList = new List<DoorData>(); 
        public PlayerData player = new PlayerData();
        public LevelData levelData = new LevelData();
    }

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
        }
    }

    public SaveData gameState = new SaveData();
    [SerializeField] public Dictionary<string, string> saveGameList = new Dictionary<string, string>();

    public void SaveGame(string saveName, bool writeToFile = true)
    {
        PopulateSaveData();

        if (!writeToFile)
            return;

        string path = Application.dataPath + "/saves/" + saveName;

        DataContractSerializer serializer = new DataContractSerializer(typeof(SaveData));
        FileStream stream = new FileStream(path, FileMode.Create);
        serializer.WriteObject(stream, gameState);
        stream.Close();
    }

    public bool LoadGame(string saveName)
    {
        string path = Application.dataPath + "/saves/" + saveName;
        if(File.Exists(path))
        {
            DataContractSerializer serializer = new DataContractSerializer(typeof(SaveData));
            FileStream stream = new FileStream(path, FileMode.Open);
            gameState = serializer.ReadObject(stream) as SaveData;
            stream.Close();
            return true;
        }

        else
        {
            Debug.LogWarningFormat("Save file '{0}' could not be loaded", saveName);
            return false;
        }
    }

    public void StoreSaveGameList(Dictionary<string, string> newSaveGameList)
    {
        saveGameList = newSaveGameList;

        foreach (KeyValuePair<string, string> kvp in saveGameList)
        {
            Debug.Log("SaveManager: " + kvp.Key + " " + kvp.Value);
        }

        string path = Application.dataPath + "/saves/savelist.lst";

        DataContractSerializer serializer = new DataContractSerializer(typeof(Dictionary<string, string>));
        FileStream stream = new FileStream(path, FileMode.Create);
        serializer.WriteObject(stream, saveGameList);
        stream.Close();
    }

    public bool LoadSaveGameList()
    {
        string path = Application.dataPath + "/saves/savelist.lst";
        if (File.Exists(path))
        {
            DataContractSerializer serializer = new DataContractSerializer(typeof(Dictionary<string, string>));
            FileStream stream = new FileStream(path, FileMode.Open);
            saveGameList = serializer.ReadObject(stream) as Dictionary<string, string>;
            stream.Close();

            return true;
        }

        else
        {
            Debug.LogWarning("Save List could not be loaded!");
            return false;
        }
    }

    void PopulateSaveData()
    {
        gameState.enemyList.Clear();
        gameState.objectList.Clear();
        gameState.worldObjectList.Clear();
        gameState.doorList.Clear();
        gameState.player.inventory.Clear();
        gameState.player.ammoReserve.Clear();
        OnGameSaved?.Invoke();
    }

    public void InvokeGameLoad()
    {
        Debug.Log("Loading saved items");
        OnPreGameLoaded?.Invoke();
        Debug.Log("PreGame Completed");
        OnGameLoaded?.Invoke();
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AmmoPickup : MonoBehaviour, ICollectable
{
    [SerializeField] private int weaponIndex = 0;
    [SerializeField] private int ammoToAdd = 0;
    [SerializeField] private string pickupMessage = string.Empty;
    [SerializeField] private AudioClip pickupSnd = null;

    AudioSource aSource = null;

    private string objectID;

    void Start()
    {
        if(SaveManager.Instance != null)
        {
            SaveManager.Instance.OnGameSaved += SaveObject;
            SaveManager.Instance.OnGameLoaded += LoadObject;
        }

        objectID = gameObject.name + transform.position.ToString();
        aSource = GetComponent<AudioSource>();
    }

    private void OnDestroy()
    {
        SaveManager.Instance.OnGameSaved -= SaveObject;
        SaveManager.Instance.OnGameLoaded -= LoadObject;
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Player"))
        {
            OnObjectCollected(other.gameObject);
        }
    }

    public void OnObjectCollected(GameObject collector)
    {
        PlayerInventory inv = collector.GetComponent<PlayerInventory>();
        if(inv != null)
        {
            // Double the ammo, if easy difficulty
            if (GameManager.Instance.DifficultyLevel < EnumContainer.DIFFICULTY.NORMAL)
                ammoToAdd = Mathf.FloorToInt(ammoToAdd * 1.5f);

            bool success = inv.AddAmmo(ammoToAdd, weaponIndex);
            if(success)
            {
                if (pickupMessage != string.Empty)
                    UIManager.Instance.DisplayMessage(pickupMessage);

                if(pickupSnd != null)
                {
                    aSource.PlayOneShot(pickupSnd);
                }

                Destroy(this.gameObject);
            }
        }
    }

    void SaveObject()
    {
        //SaveManager.SaveData.WorldObjectData objectData = new SaveManager.SaveData.WorldObjectData();
       // objectData.objectID = objectID;

        SaveManager.Instance.gameState.worldObjectList.Add(objectID);
    }

    void LoadObject()
    {
        /*SaveManager.SaveData.WorldObjectData objectData = null;
        for(int i = 0; i < SaveManager.Instance.gameState.worldObjectList.Count; i++)
        {
            if (SaveManager.Instance.gameState.worldObjectList[i].objectID == objectID)
            {
                objectData = SaveManager.Instance.gameState.worldObjectList[i];
                break;
            }
        }

        if (objectData == null)
            Destroy(gameObject);*/

        if (!SaveManager.Instance.gameState.worldObjectList.Contains(objectID))
            Destroy(gameObject);
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponPickup : MonoBehaviour, ICollectable
{
    [SerializeField] private GameObject weaponPrefab = null;
    [SerializeField] private string pickupMessage = string.Empty;

    private string objectID;

    void Start()
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.OnGameSaved += SaveObject;
            SaveManager.Instance.OnGameLoaded += LoadObject;
        }

        objectID = gameObject.name + transform.position.ToString();
    }

    private void OnDestroy()
    {
        SaveManager.Instance.OnGameSaved -= SaveObject;
        SaveManager.Instance.OnGameLoaded -= LoadObject;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            OnObjectCollected(other.gameObject);
        }
    }

    public void OnObjectCollected(GameObject collector)
    {
        PlayerInventory inv = collector.GetComponent<PlayerInventory>();
        inv.AddWeapon(weaponPrefab);

        if (pickupMessage != string.Empty)
            UIManager.Instance.DisplayMessage(pickupMessage);

        Destroy(gameObject);
    }

    void SaveObject()
    {
        //SaveManager.SaveData.WorldObjectData objectData = new SaveManager.SaveData.WorldObjectData();
        //objectData.objectID = objectID;

        SaveManager.Instance.gameState.worldObjectList.Add(objectID);
    }

    void LoadObject()
    {
        /*SaveManager.SaveData.WorldObjectData objectData = null;
        for (int i = 0; i < SaveManager.Instance.gameState.worldObjectList.Count; i++)
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

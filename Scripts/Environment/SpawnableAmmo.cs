using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnableAmmo : MonoBehaviour
{
    [SerializeField] private int weaponIndex = 0;
    [SerializeField] private int ammoToAdd = 0;
    [SerializeField] private string pickupMessage = string.Empty;
    [SerializeField] private string prefabName = string.Empty;

    void Start()
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.OnGameSaved += SaveObject;
        }    
    }

    private void OnDestroy()
    {
        SaveManager.Instance.OnGameSaved -= SaveObject;
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
        if (inv != null)
        {
            // Double the ammo, if easy difficulty
            if (GameManager.Instance.DifficultyLevel < EnumContainer.DIFFICULTY.NORMAL)
                ammoToAdd *= 2;

            bool success = inv.AddAmmo(ammoToAdd, weaponIndex);
            if (success)
            {
                if (pickupMessage != string.Empty)
                    UIManager.Instance.DisplayMessage(pickupMessage);

                Destroy(this.gameObject);
            }
        }
    }

    void SaveObject()
    {
        SaveManager.SaveData.ObjectData objectData = new SaveManager.SaveData.ObjectData();
        objectData.prefabName = prefabName;
        objectData.transformData.position = transform.position;
        objectData.transformData.rotation = transform.rotation.eulerAngles;
        objectData.transformData.scale = transform.localScale;
        objectData.velocity = Vector3.zero;

        SaveManager.Instance.gameState.objectList.Add(objectData);
    }
}

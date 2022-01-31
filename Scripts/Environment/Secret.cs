using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Secret : MonoBehaviour
{
    private string id = string.Empty;

    private void Start()
    {
        id = gameObject.name + transform.position.ToString();

        SaveManager.Instance.OnGameSaved += SaveTrigger;
        SaveManager.Instance.OnGameLoaded += LoadTrigger;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        ActivateSecret();
    }

    private void OnDestroy()
    {
        SaveManager.Instance.OnGameSaved -= SaveTrigger;
        SaveManager.Instance.OnGameLoaded -= LoadTrigger;
    }

    void ActivateSecret()
    {
        GameManager.Instance.IncrementSecretCount();
        UIManager.Instance.DisplayMessage("Found a Secret!");
        Destroy(this.gameObject);
        // TODO: Play Sound
    }

    void SaveTrigger()
    {
        //var objectData = new SaveManager.SaveData.WorldObjectData();
        //objectData.objectID = id;
        SaveManager.Instance.gameState.worldObjectList.Add(id);
    }

    void LoadTrigger()
    {
        /*SaveManager.SaveData.WorldObjectData objectData = null;
        for (int i = 0; i < SaveManager.Instance.gameState.worldObjectList.Count; i++)
        {
            if (SaveManager.Instance.gameState.worldObjectList[i].objectID == id)
            {
                objectData = SaveManager.Instance.gameState.worldObjectList[i];
                break;
            }
        }

        // Event already triggered
        if (objectData == null)
        {
            Destroy(gameObject);
        }*/

        if (!SaveManager.Instance.gameState.worldObjectList.Contains(id))
            Destroy(gameObject);
    }
}

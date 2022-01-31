/*
 * A class to handle various game events mid-game
 * For example: Get trapped in a room, spawn something, when entering a trigger
 * Events can be assigned in the Unity inspector
 * 
 * */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class GameEventTrigger : MonoBehaviour
{
    [SerializeField] private bool playerOnly = true;                 // Can enemies (and other rbs) trigger this event
    [SerializeField] private bool useKeyRequired = false;
    [SerializeField] private bool oneShot = true;                    // Is this a looping event

    [SerializeField] private List<MapEvent> eventsToTrigger = new List<MapEvent>();

    private string id = string.Empty;

    private bool playerInTrigger = false;
    private bool triggerUsed = false;

    void Start()
    {
        id = gameObject.name + transform.position.ToString();

        SaveManager.Instance.OnGameSaved += SaveTrigger;
        SaveManager.Instance.OnGameLoaded += LoadTrigger;
    }

    void Update()
    {
        if (!playerInTrigger)
            return;

        if (oneShot && triggerUsed)
            return;

        if (Input.GetKey(KeyCode.F))
        {
            triggerUsed = true;
            ActivateEvents();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (playerOnly)
        {
            if (!other.CompareTag("Player"))
                return;
        }

        if (useKeyRequired)
        {
            playerInTrigger = true;
        }

        else
        {
            if (triggerUsed)
                return;

            ActivateEvents();
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
            playerInTrigger = false;
    }

    void OnDestroy()
    {
        SaveManager.Instance.OnGameSaved -= SaveTrigger;
        SaveManager.Instance.OnGameLoaded -= LoadTrigger;
    }

    void ActivateEvents()
    {
        for(int i = 0; i < eventsToTrigger.Count; ++i)
        {
            StartCoroutine(ActivateEvent(eventsToTrigger[i]));
        }

        if (oneShot)
            triggerUsed = true;
    }

    IEnumerator ActivateEvent(MapEvent mapEvent)
    {
        Debug.Log("GMT: Activated event");
        yield return new WaitForSeconds(mapEvent.delay);
        mapEvent.ActivateEvent();
    }

    void SaveTrigger()
    {
        // Don't save, if already triggered
        if (triggerUsed && oneShot)
            return;

        SaveManager.Instance.gameState.worldObjectList.Add(id);
    }

    void LoadTrigger()
    {
        if (!SaveManager.Instance.gameState.worldObjectList.Contains(id))
            Destroy(gameObject);
    }
}

[System.Serializable]
public class MapEvent
{
    public float delay;

    public Transform targetObject;

    public UnityEvent eventDefault;
    public UnityEvent<Transform> eventTransform;

    public void ActivateEvent()
    {
        if (targetObject != null)
        {
            eventTransform.Invoke(targetObject);
        }

        eventDefault.Invoke();
    }
}


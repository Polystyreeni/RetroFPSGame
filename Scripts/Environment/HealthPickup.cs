using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealthPickup : MonoBehaviour
{
    [SerializeField] private int healthAmount = 0;
    [SerializeField] private bool fillMaxHealth = false;
    [SerializeField] private string pickupMessage = string.Empty;

    private string objectID;

    private void Start()
    {
        objectID = gameObject.name + transform.position.ToString();
        SaveManager.Instance.OnGameSaved += SavePickup;
        SaveManager.Instance.OnGameLoaded += LoadPickup;
    }

    private void OnDestroy()
    {
        SaveManager.Instance.OnGameSaved -= SavePickup;
        SaveManager.Instance.OnGameLoaded -= LoadPickup;
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Player"))
        {
            PickupObject(other);
        }
    }

    void PickupObject(Collider other)
    {
        //PlayerHealth health = other.GetComponent<PlayerHealth>();
        PlayerContainer pc = other.GetComponentInParent<PlayerContainer>();
        if (CanPickup(pc))
        {
            pc.TakeDamage(-healthAmount, null);
            UIManager.Instance.DisplayMessage(pickupMessage);
            // TODO: Sound, fx, other here
            Destroy(gameObject);
        }
    }

    bool CanPickup(PlayerContainer pc)
    {
        PlayerHealth pH = pc.PlayerHealth;
        if (!pH.IsAlive)
            return false;

        if(pH.Health >= pH.FullHealth)
        {
            if (fillMaxHealth && pH.Health < pH.MaxHealth)
                return true;

            return false;
        }

        else
        {
            // Capping health here, since we can't check this elsewhere
            if (pH.Health + healthAmount > pH.FullHealth && !fillMaxHealth)
                healthAmount = pH.FullHealth - pH.Health;

            return true;
        }
    }

    void SavePickup()
    {
        SaveManager.Instance.gameState.worldObjectList.Add(objectID);
    }

    void LoadPickup()
    {
        if (!SaveManager.Instance.gameState.worldObjectList.Contains(objectID))
            Destroy(gameObject);
    }
}

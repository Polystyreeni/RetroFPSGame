using UnityEngine;

public class KeyPickup : MonoBehaviour, ICollectable
{
    [SerializeField] private int keyIndex = 0;
    [SerializeField] private string pickupMessage = string.Empty;
    [SerializeField] private AudioClip pickupSnd = null;

    AudioSource aSource = null;

    private string objectID;

    void Start()
    {
        if (SaveManager.Instance != null)
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
        if (other.CompareTag("Player"))
        {
            OnObjectCollected(other.gameObject);
        }
    }

    public void OnObjectCollected(GameObject collector)
    {
        PlayerInventory inv = collector.GetComponent<PlayerInventory>();
        inv.AddKey(keyIndex);

        if (pickupMessage != string.Empty)
            UIManager.Instance.DisplayMessage(pickupMessage);

        if (pickupSnd != null)
        {
            aSource.PlayOneShot(pickupSnd);
        }

        Destroy(gameObject, 0.1f);
    }

    void SaveObject()
    {
        SaveManager.Instance.gameState.worldObjectList.Add(objectID);
    }

    void LoadObject()
    {
        if(!SaveManager.Instance.gameState.worldObjectList.Contains(objectID))
            Destroy(gameObject);
    }
}

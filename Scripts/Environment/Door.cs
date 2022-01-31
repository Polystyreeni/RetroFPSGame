using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Door : MonoBehaviour
{
    // Editable variables
    [Header("Opening parameters")]
    [SerializeField] private bool bEnemyCanOpen = false;        // If false, only player can open door
    [SerializeField] private bool bAutoActivate = false;        // Require / don't require use key
    [SerializeField] private bool bUseOnce = false;             // Can't be opened again if true
    [SerializeField] private int keyIndex = 0;                  // 0 = no key required, -1 = locked, other = key id

    [Header("State timers")]
    [SerializeField] private float openDuration = 1f;           // Duration of movement
    [SerializeField] private float autoCloseDuration = 0f;      // If 0, auto-close disabled
    [SerializeField] private float animationSpeed = 1f;

    [Header("Sounds")]
    [SerializeField] private AudioClip openSound = null;        // Sound to play when the door opens
    [SerializeField] private AudioClip closeSound = null;       // Sound to play when the door closes
    [SerializeField] private AudioClip lockedSound = null;      // Sound to play if the door is locked

    [SerializeField] private Vector3 movement = Vector3.zero;   // Movement vector of the door
    private Vector3 maxPos = Vector3.zero;

    // Non-editable variables here
    Animator doorAnimator = null;
    Transform[] doorGeo = null;
    private bool bUseAnimation = false;
    private bool bDoorMoving = false;
    private bool bTriggerActive = false;
    private GameObject currentUser = null;
    private AudioSource aSrc = null;
    private bool bDoorOpen = false;
    private bool bCanUse = true;   // TODO: Disable door trigger instead?
    private float autoCloseElapsed = 0;

    NavMeshObstacle navMeshObstacle = null;

    private string doorID;
    void Start()
    {
        doorAnimator = GetComponent<Animator>();
        if (doorAnimator != null)
        {
            bUseAnimation = true;
            doorAnimator.speed = animationSpeed;
        }

        aSrc = GetComponent<AudioSource>();

        maxPos = transform.position + movement;
        doorID = gameObject.name + transform.position.ToString();

        doorGeo = GetComponentsInChildren<Transform>();
        SaveManager.Instance.OnGameSaved += SaveDoor;
        SaveManager.Instance.OnGameLoaded += LoadDoor;

        navMeshObstacle = GetComponent<NavMeshObstacle>();
        if (bEnemyCanOpen)
        {
            ObstacleEnabled(false);
        }
    }

    private void OnDestroy()
    {
        SaveManager.Instance.OnGameSaved -= SaveDoor;
        SaveManager.Instance.OnGameLoaded -= LoadDoor;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!bCanUse)
            return;

        //Debug.Log("Entered door: " + other.name);

        bool isValid;
        if (bEnemyCanOpen)
            isValid = other.CompareTag("Player") || (other.CompareTag("Enemy") && !bDoorOpen);
        else
            isValid = other.CompareTag("Player");

        if (isValid)
        {
            if(other.CompareTag("Player"))
            {
                currentUser = other.gameObject;
                bTriggerActive = true;
            }
                
            if (bAutoActivate || other.gameObject.layer == LayerMask.NameToLayer("Actor") && !bDoorMoving)
            {
                if(bDoorOpen)
                {
                    StopAllCoroutines();
                    StartCoroutine(CloseDoor());
                }
                    

                else
                    StartCoroutine(OpenDoor());

                if (bUseOnce)
                    bCanUse = false;
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if(other.CompareTag("Player"))
        {
            bTriggerActive = false;
            currentUser = null;
        }   
    }

    void Update()
    {
        // TODO: Switch to Input system
        if(Input.GetKeyDown(KeyCode.F))
        {
            if(bTriggerActive && !bDoorMoving)
            {
                if(keyIndex < 0)    // Key index -1 = door that is always locked / jammed and can only be opened through script
                {
                    PlaySound(lockedSound);
                    UIManager.Instance.DisplayMessage("Door is Locked!");
                    return;
                }

                if (!bDoorOpen)
                {
                    if(keyIndex > 0)
                    {
                        PlayerInventory inv = currentUser.GetComponent<PlayerInventory>();
                        if (inv == null || !inv.HasKey(keyIndex))
                        {
                            UIManager.Instance.DisplayMessage("Requires a Key!");
                            return;
                        }
                            
                    }
                    
                    StartCoroutine(OpenDoor());
                }
                    

                else
                {
                    if (keyIndex > 0)
                    {
                        PlayerInventory inv = currentUser.GetComponent<PlayerInventory>();
                        if (inv == null || !inv.HasKey(keyIndex))
                        {
                            UIManager.Instance.DisplayMessage("Requires a Key!");
                            return;
                        }
                    }

                    StopAllCoroutines();
                    StartCoroutine(CloseDoor());
                }
                    
            }
        }
    }

    IEnumerator OpenDoor()
    {
        bDoorMoving = true;
        bDoorOpen = true;
        ObstacleEnabled(false);

        if (bUseAnimation)
        {
            doorAnimator.SetBool("DoorOpen", true);
        }

        else
        {
            foreach (Transform t in doorGeo)
            {
                if (t == transform)
                    continue;

                StartCoroutine(MoveObject(t, true));
            }
        }

        PlaySound(openSound);

        yield return new WaitForSeconds(openDuration);
        bDoorMoving = false;

        if (autoCloseDuration > 0)
        {
            autoCloseElapsed = 0;
            StartCoroutine(DoorAutoClose());
        }
    }

    IEnumerator CloseDoor()
    {
        bDoorMoving = true;
        bDoorOpen = false;
        autoCloseElapsed = 0;

        if (bUseAnimation)
        {
            doorAnimator.SetBool("DoorOpen", false);
        }

        else
        {
            foreach (Transform t in doorGeo)
            {
                if (t == transform)
                    continue;

                StartCoroutine(MoveObject(t, false));
            }
        }

        PlaySound(closeSound);

        yield return new WaitForSeconds(openDuration);
        bDoorMoving = false;

        ObstacleEnabled(true);
    }

    IEnumerator DoorAutoClose()
    {
        float interval = 0.1f;
        while(autoCloseElapsed < autoCloseDuration)
        {
            autoCloseElapsed += interval;
            yield return new WaitForSeconds(interval);
        }

        StartCoroutine(CloseDoor());
        yield break;
    }

    IEnumerator MoveObject(Transform trans, bool bOpen)
    {
        Vector3 finalPos;
        if(bOpen)
            finalPos = trans.position + movement;
        else
            finalPos = trans.position - movement;

        Vector3 startPosition = trans.position;
        float timeElapsed = 0f;

        while (timeElapsed < openDuration)
        {
            trans.position = Vector3.Lerp(startPosition, finalPos, timeElapsed / openDuration);
            timeElapsed += Time.deltaTime;
            yield return null;
        }

        trans.position = finalPos;
    }

    void PlaySound(AudioClip snd)
    {
        if (aSrc == null)
            return;

        if(snd != null)
        {
            aSrc.PlayOneShot(snd);
        }
    }

    void SaveDoor()
    {
        var doorData = new SaveManager.SaveData.DoorData();
        doorData.objectID = doorID;
        doorData.doorOpen = bDoorOpen;
        doorData.doorTimer = autoCloseElapsed;

        SaveManager.Instance.gameState.doorList.Add(doorData);
    }

    void LoadDoor()
    {
        // TODO: Auto closing feature, maybe save doors position as well?
        SaveManager.SaveData.DoorData doorData = null;
        for(int i = 0; i < SaveManager.Instance.gameState.doorList.Count; i++)
        {
            if(SaveManager.Instance.gameState.doorList[i].objectID == doorID)
            {
                doorData = SaveManager.Instance.gameState.doorList[i];
                break;
            }
        }

        if (doorData == null)
            return;

        bDoorOpen = doorData.doorOpen;
        if(doorData.doorOpen)
        {
            if (bUseOnce)
                bCanUse = false;

            if (bUseAnimation)
                StartCoroutine(OpenDoor());

            else
            {
                foreach (Transform t in doorGeo)
                {
                    if (t == transform)
                        continue;
                    t.position += movement;
                }
            }

            if (autoCloseDuration > 0)
            {
                autoCloseElapsed = doorData.doorTimer;
                if (autoCloseElapsed > 0)
                {
                    StartCoroutine(DoorAutoClose());
                }
            }
        }
    }

    void ObstacleEnabled(bool value)
    {
        if(navMeshObstacle != null)
            navMeshObstacle.enabled = value;
    }
}

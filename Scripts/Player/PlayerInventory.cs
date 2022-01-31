using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    // Assing through Inspector
    public GameObject defaultWeapon = null;
    public GameObject activeWeapon = null;
    public Transform gunContainer = null;
    [SerializeField] private GameObject abilityActivateWeapon = null;
    [SerializeField] private GameObject abilityPunchWeapon = null;
    [SerializeField] private AudioClip weaponSound = null;
    [SerializeField] private float abilityDecay = 5f;
    [SerializeField] private AudioClip sndAbilityActivate = null;
    [SerializeField] private AudioClip sndAbilityDeactivate = null;

    // Inventory stats
    private int inventoryMaxSize = 10;
    private Dictionary<int, GameObject> weaponInventory;
    private Dictionary<int, int> ammoReserve = new Dictionary<int, int>();
    private Dictionary<int, EnumContainer.PLAYERABILITY> abilities = new Dictionary<int, EnumContainer.PLAYERABILITY>();
    private List<int> doorKeys = new List<int>();
    private int meleeWeaponIndex = 1;
    private int abilityIndex = 0;

    // Abilities
    private float abilityMaxValue = 100f;
    private float abilityMinValue = 0f;
    private float abilityCurrent = 0f;
    private float abilityTimer = 0f;
    private float abilityDrainRate = 5f;
    private bool abilityActive = false;
    private bool abilityDecayed = false;

    AudioSource aSource = null;
    private bool bSwitchingWeapons = false;

    // Getters / setters
    public bool AllowWeaponSwitching { get; set; }

    // Input
    const int NOINPUT = -1;
    const int MELEE_INPUT = 10;
    const int ABILITY_INPUT = 11;
    const int ABILITY_CHANGE_INPUT = 12;
    private int playerInputCurrent = NOINPUT;
    private int playerInputQueue = NOINPUT;

    // Parent component needs info for saving / loading
    PlayerContainer playerContainer = null;

    private void Start()
    {
        weaponInventory = new Dictionary<int, GameObject>();

        SetDefaultWeapon();

        GiveAbilityWeapon();

        // TODO: Remove
        GiveAbilities();

        playerContainer = GetComponentInParent<PlayerContainer>();
        playerContainer.OnPlayerSave += SaveInventory;
        playerContainer.OnPlayerLoad += LoadInventory;
        playerContainer.OnPlayerDeath += DeathHolsterWeapon;
        playerContainer.OnAbilityActivate += AbilityActivated;
        playerContainer.OnAbilityDeactivate += AbilityDeactivated;

        aSource = GetComponentInParent<AudioSource>();
    }

    void SetDefaultWeapon()
    {
        // Give default weapon to player
        GameObject gun = Instantiate(defaultWeapon, gunContainer.position, gunContainer.rotation);
        gun.transform.SetParent(gunContainer);

        PlayerWeapon wpn = gun.GetComponent<PlayerWeapon>();

        int weaponIndex = wpn.inventorySlot;
        wpn.ammoClip = wpn.clipDefault;
        wpn.ammoStock = wpn.stockDefault;

        weaponInventory.Add(weaponIndex, gun);
        gun.SetActive(true);

        activeWeapon = weaponInventory[weaponIndex];

        wpn.SwitchToWeapon();
        AllowWeaponSwitching = true;

        Invoke(nameof(ResetAbilityBar), 0f);
    }

    void ResetAbilityBar()
    {
        UIManager.Instance.UpdateAbilityBar(abilityCurrent);
        UIManager.Instance.UpdateAbilityIcon(abilityIndex, abilityCurrent, abilityActive);
    }

    void GiveAbilityWeapon()
    {
        AddWeapon(abilityActivateWeapon, false);
        AddWeapon(abilityPunchWeapon, false);    
    }

    void GiveAbilities()
    {
        AddAbility(0, EnumContainer.PLAYERABILITY.DAMAGE);
        AddAbility(1, EnumContainer.PLAYERABILITY.SPEED);
        AddAbility(2, EnumContainer.PLAYERABILITY.TIME);
    }

    private void OnDestroy()
    {
        playerContainer.OnPlayerSave -= SaveInventory;
        playerContainer.OnPlayerLoad -= LoadInventory;
        playerContainer.OnPlayerDeath -= DeathHolsterWeapon;
        playerContainer.OnAbilityActivate -= AbilityActivated;
        playerContainer.OnAbilityDeactivate -= AbilityDeactivated;
    }

    // Update is called once per frame
    void Update()
    {
        if (UIManager.Instance.BGamePaused)
            return;

        UpdateAbilityCounter();
        QueueInput();

        HandleInput();
    }

    void HandleInput()
    {
        if (bSwitchingWeapons)
            return;

        if (playerInputQueue == NOINPUT)
            return;

        if (!AllowWeaponSwitching)
            return;

        if (playerInputQueue == ABILITY_INPUT)
        {
            if (abilityActive)
            {
                // No instant cancelling of ability
                if (abilityCurrent <= 95)
                {
                    DeactivateAbility();
                    abilityCurrent /= 2;
                    UIManager.Instance.UpdateAbilityBar(abilityCurrent);
                }
            }

            else if (abilityCurrent >= abilityMaxValue && !bSwitchingWeapons)
            {
                ActivateAbility();
            }

            playerInputQueue = NOINPUT;
        }

        else if (playerInputQueue == MELEE_INPUT)
        {
            StartCoroutine(QuickMelee());
        }

        else if (playerInputQueue == ABILITY_CHANGE_INPUT)
        {
            ChangeActiveAbility();
        }

        else if (weaponInventory.ContainsKey(playerInputQueue))
        {
            if (weaponInventory[playerInputQueue] == activeWeapon)
                return;

            StartCoroutine(SetActiveWeapon(playerInputQueue));
        }
    }

    void UpdateAbilityCounter()
    {
        if(abilityTimer > abilityDecay)
        {
            if (abilityCurrent < abilityMaxValue)
            {
                if (!abilityDecayed)
                {
                    abilityMinValue = abilityCurrent / 2;
                    abilityDecayed = true;
                }

                if (abilityCurrent > abilityMinValue)
                {
                    abilityCurrent -= Time.deltaTime;
                }

                UIManager.Instance.UpdateAbilityBar(abilityCurrent);
                UIManager.Instance.UpdateAbilityIcon(abilityIndex, abilityCurrent, abilityActive);
            }
        }

        else if(abilityActive)
        {
            //Debug.Log("Ability Active");
            abilityCurrent -= Time.deltaTime * abilityDrainRate;
            if(abilityCurrent <= abilityMinValue)
            {
                DeactivateAbility();
            }

            UIManager.Instance.UpdateAbilityBar(abilityCurrent);
            //UIManager.Instance.UpdateAbilityIcon(abilityIndex, abilityCurrent, abilityActive);
        }

        else
        {
            if(abilityCurrent <= abilityMaxValue)
                abilityTimer += Time.deltaTime;
        }
    }

    void ChangeActiveAbility()
    {
        if (abilities.Keys == null || abilities.Keys.Count < 0 || abilityActive)
            return;

        bool foundKey = false;

        int newKey = abilityIndex + 1;
        if (abilities.ContainsKey(newKey))
        {
            abilityIndex = newKey;
            foundKey = true;
        }
           
        else
        {
            for(int i = newKey; i < abilities.Count; ++i)
            {
                if (abilities.ContainsKey(i))
                {
                    abilityIndex = i;
                    foundKey = true;
                }  
            }
        }

        if(!foundKey)
        {
            abilityIndex = 0;
        }

        playerInputQueue = NOINPUT;
        //Debug.Log("Active Ability Is: " + abilityIndex);

        // TODO: Update HUD
        UIManager.Instance.UpdateAbilityIcon(abilityIndex, abilityCurrent, abilityActive);
    }

    void QueueInput()
    {
        // TODO: To be moved to new input system, current implementation a demo 
        if (Input.GetKey(KeyCode.Alpha0))
            playerInputCurrent = 0;
        else if (Input.GetKeyDown(KeyCode.Alpha1))
            playerInputCurrent = 1;
        else if (Input.GetKeyDown(KeyCode.Alpha2))
            playerInputCurrent = 2;
        else if (Input.GetKeyDown(KeyCode.Alpha3))
            playerInputCurrent = 3;
        else if (Input.GetKeyDown(KeyCode.Alpha4))
            playerInputCurrent = 4;
        else if (Input.GetKeyDown(KeyCode.Alpha5))
            playerInputCurrent = 5;
        else if (Input.GetKeyDown(KeyCode.Alpha6))
            playerInputCurrent = 6;
        else if (Input.GetKeyDown(KeyCode.Alpha7))
            playerInputCurrent = 7;
        else if (Input.GetKeyDown(KeyCode.Alpha8))
            playerInputCurrent = 8;
        else if (Input.GetKeyDown(KeyCode.Alpha9))
            playerInputCurrent = 9;

        // Mouse wheel support
        else if (Input.GetAxis("Mouse ScrollWheel") > 0)
        {
            playerInputCurrent = GetNextValidWeapon(true);
        }

        else if (Input.GetAxis("Mouse ScrollWheel") < 0)
        {
            playerInputCurrent = GetNextValidWeapon(false);
        }

        else if (Input.GetKeyDown(KeyCode.E))
            playerInputCurrent = MELEE_INPUT;
        else if (Input.GetKeyDown(KeyCode.Q))
            playerInputCurrent = ABILITY_INPUT;
        else if (Input.GetKeyDown(KeyCode.Tab))
            playerInputCurrent = ABILITY_CHANGE_INPUT;
        else
            playerInputCurrent = NOINPUT;

        if (playerInputCurrent != NOINPUT)
            playerInputQueue = playerInputCurrent;

    }

    // TODO: Check weapon indexes (do we start from 0 or 1)
    int GetNextValidWeapon(bool increasing)
    {
        if (activeWeapon == null)
            return 0;

        int currentWeapon = activeWeapon.GetComponentInChildren<PlayerWeapon>().inventorySlot;

        int nextWeapon = currentWeapon;
        int jump = 1;
        if (!increasing)
            jump *= -1;

        while(true)
        {
            if(weaponInventory.ContainsKey(nextWeapon + jump))
            {
                //Debug.Log("WeaponInventory contains weapon: " + (nextWeapon + jump));
                var playerWeapon = weaponInventory[nextWeapon + jump].GetComponentInChildren<PlayerWeapon>();
                if(playerWeapon.ammoClip <= 0 && playerWeapon.ammoStock <= 0)
                {
                    nextWeapon += jump;
                    if (nextWeapon >= inventoryMaxSize)
                        nextWeapon = 0;

                    if (nextWeapon < 0)
                        nextWeapon = inventoryMaxSize - 1;
                }

                else
                {
                    nextWeapon = nextWeapon + jump;
                    break;
                }
                
            }

            else
            {
                nextWeapon += jump;
                if(nextWeapon >= inventoryMaxSize)
                    nextWeapon = 0;

                if(nextWeapon < 0)
                    nextWeapon = inventoryMaxSize - 1;
            }
        }

        return nextWeapon;
    }

    /// <summary>
    /// Sets a new active weapon for the player
    /// </summary>
    /// <param name="index">New weapon index (from inventory)</param>
    /// <returns></returns>
    IEnumerator SetActiveWeapon(int index)
    {
        // No weapon to change to, so do nothing
        if (!weaponInventory.ContainsKey(index))
            yield break;

        PlayerWeapon wpn = weaponInventory[index].GetComponentInChildren<PlayerWeapon>();
        if (wpn.ammoClip <= 0 && wpn.ammoStock <= 0)
        {
            playerInputQueue = NOINPUT;
            yield break;
        }
            
        WeaponAnimator animator;
        float wait;
        if (activeWeapon != null)
        {
            PlayerWeapon weapon = activeWeapon.GetComponentInChildren<PlayerWeapon>();
            //Debug.Log("Player inv: can Switch weapon: " + weapon.CanSwitchWeapon);

            if (!weapon.CanSwitchWeapon || weapon.IsFiringAny())
            {
                StopAllCoroutines();
                yield break;
            }

            bSwitchingWeapons = true;

            // Reset Queue for weapon changing
            playerInputQueue = NOINPUT;

            weapon.SwitchFromWeapon();

            animator = activeWeapon.GetComponentInChildren<WeaponAnimator>();
            wait = animator.GetAnimLenght(4);

            yield return new WaitForSeconds(wait);
        }

        SetCurrentWeapon(index, false);

        // Display new weapon in hud
        UIManager.Instance.ShowCurrentWeapon(index);
        if (weaponSound != null)
            aSource.PlayOneShot(weaponSound);

        animator = activeWeapon.GetComponentInChildren<WeaponAnimator>();
        wait = animator.GetAnimLenght(4);
        yield return new WaitForSeconds(wait);
        bSwitchingWeapons = false;
        AddReserveAmmo(index);
    }

    /// <summary>
    /// Perform a quick melee attack with any weapon
    /// </summary>
    /// <returns></returns>
    IEnumerator QuickMelee()
    {
        if (!activeWeapon.GetComponentInChildren<PlayerWeapon>().bCanShoot)
            yield break;

        int previousWeapon = activeWeapon.GetComponentInChildren<PlayerWeapon>().inventorySlot;
        if (previousWeapon == 1 && !abilityActive)    // Melee regularly, if knife is held
        {
            //activeWeapon.GetComponentInChildren<PlayerWeapon>().FireWeapon();
            //yield return new WaitForSeconds(activeWeapon.GetComponentInChildren<WeaponAnimator>().GetAnimLenght(1));
            yield break;
        }

        SwitchToWeaponInstant(meleeWeaponIndex);
        bSwitchingWeapons = true;
        float animLenght = activeWeapon.GetComponentInChildren<WeaponAnimator>().GetAnimLenght(1);

        if(abilityActive)
        {
            activeWeapon.GetComponentInChildren<PlayerWeapon>().FireSecondary();
        }

        else
        {
            activeWeapon.GetComponentInChildren<PlayerWeapon>().FireWeapon();
        }

        yield return new WaitForSeconds(animLenght);
        bSwitchingWeapons = false;
        StartCoroutine(SetActiveWeapon(previousWeapon));
    }

    /// <summary>
    /// Instantly set player new weapon (no animations)
    /// </summary>
    /// <param name="index">weapon index to change to</param>
    void SwitchToWeaponInstant(int index)
    {
        if (!weaponInventory.ContainsKey(index))
            return;

        PlayerWeapon wpn = weaponInventory[index].GetComponentInChildren<PlayerWeapon>();
        if (wpn.ammoClip <= 0 && wpn.ammoStock <= 0)
        {
            playerInputQueue = NOINPUT;
            return;
        }

        if (activeWeapon != null)
        {
            PlayerWeapon weapon = activeWeapon.GetComponentInChildren<PlayerWeapon>();
            if (!weapon.CanSwitchWeapon || weapon.IsFiringAny())
            {
                return;
            }

            bSwitchingWeapons = true;

            // Reset Queue for weapon changing
            playerInputQueue = NOINPUT;
        }

        SetCurrentWeapon(index, true);
        bSwitchingWeapons = false;
    }

    void SetCurrentWeapon(int index, bool isInstant = false)
    {
        for (int i = 0; i <= inventoryMaxSize + 2; i++)
        {
            if (weaponInventory.ContainsKey(i))
            {
                if (i == index)
                {
                    activeWeapon = weaponInventory[i];
                    activeWeapon.SetActive(true);
                    PlayerWeapon currentWeapon = activeWeapon.GetComponentInChildren<PlayerWeapon>();
                    currentWeapon.SwitchToWeapon(isInstant);         
                }

                else
                {
                    weaponInventory[i].SetActive(false);
                }
            }
        }
    }

    void ActivateAbility()
    {
        Debug.Log("Ability Active");
        aSource.PlayOneShot(sndAbilityActivate);
        StartCoroutine(AbilityActivated());
    }

    void DeactivateAbility()
    {
        Debug.Log("Ability Deactive");
        abilityActive = false;
        abilityDecayed = false;
        abilityMinValue = 0;
        abilityTimer = 0;
        playerContainer.PlayerAbilityActive = false;
        playerContainer.DisableAbility(abilities[abilityIndex]);
        UIManager.Instance.UpdateAbilityIcon(abilityIndex, abilityCurrent, abilityActive);
        aSource.PlayOneShot(sndAbilityDeactivate);
    }

    IEnumerator AbilityActivated()
    {
        PlayerWeapon previousWeapon = activeWeapon.GetComponentInChildren<PlayerWeapon>();
        PlayerWeapon abilWeapon = weaponInventory[11].GetComponentInChildren<PlayerWeapon>();   // TODO: Make this a non magic number
        SwitchToWeaponInstant(abilWeapon.inventorySlot);

        bSwitchingWeapons = true;
        abilWeapon.SwitchToWeapon();

        float wait = abilWeapon.gameObject.GetComponentInChildren<WeaponAnimator>().GetAnimLenght(3);
        yield return new WaitForSeconds(wait);

        bSwitchingWeapons = false;
        StartCoroutine(SetActiveWeapon(previousWeapon.inventorySlot));

        AbilitySetActive();

        yield break;
    }

    void AbilitySetActive()
    {
        abilityActive = true;
        playerContainer.PlayerAbilityActive = true;
        playerContainer.EnableAbility(abilities[abilityIndex]);
        abilityMinValue = 0;
        abilityTimer = 0;
        abilityDecayed = false;
    }

    void AddReserveAmmo(int weaponIndex)
    {
        if (!weaponInventory.ContainsKey(weaponIndex))
            return;

        if (!ammoReserve.ContainsKey(weaponIndex))
            return;

        GameObject gun = weaponInventory[weaponIndex];
        PlayerWeapon gunInfo = gun.GetComponentInChildren<PlayerWeapon>();

        gunInfo.AddAmmo(ammoReserve[weaponIndex]);
        ammoReserve[weaponIndex] = 0;
    }

    void SaveInventory()
    {
        for(int i = 0; i < inventoryMaxSize; i++)
        {
            if(weaponInventory.ContainsKey(i))
            {
                SaveManager.SaveData.WeaponData weaponData = new SaveManager.SaveData.WeaponData();
                PlayerWeapon wpnInfo = weaponInventory[i].GetComponentInChildren<PlayerWeapon>();
                weaponData.weaponIndex = wpnInfo.inventorySlot;
                weaponData.ammoClip = wpnInfo.ammoClip;
                weaponData.ammoStock = wpnInfo.ammoStock;
                weaponData.fireCounter = wpnInfo.fireCounter;

                SaveManager.Instance.gameState.player.inventory.Add(weaponData);
            }

            else
            {
                if (ammoReserve.ContainsKey(i))
                    SaveManager.Instance.gameState.player.ammoReserve.Add(i, ammoReserve[i]);
            }
        }

        if(activeWeapon != null)
            SaveManager.Instance.gameState.player.activeWeaponIndex = activeWeapon.GetComponentInChildren<PlayerWeapon>().inventorySlot;
        else
            SaveManager.Instance.gameState.player.activeWeaponIndex = 1;

        SaveManager.Instance.gameState.player.doorKeys = doorKeys;
        SaveManager.Instance.gameState.player.abilities = abilities;
        SaveManager.Instance.gameState.player.activeAbilityIndex = abilityIndex;
        SaveManager.Instance.gameState.player.abilityActive = abilityActive;
        SaveManager.Instance.gameState.player.abilityCounter = abilityCurrent;
    }

    public void LoadInventory()
    {
        ClearInventory();
        SaveManager.SaveData.PlayerData playerData = SaveManager.Instance.gameState.player;

        // Give weapons to player
        for (int i = 0; i < playerData.inventory.Count; i++)
        {
            GameObject weaponPrefab = InventoryInfo.Instance.GetWeaponByIndex(playerData.inventory[i].weaponIndex);
            
            GameObject gun = Instantiate(weaponPrefab, gunContainer.position, gunContainer.rotation);
            gun.transform.SetParent(gunContainer);

            PlayerWeapon wpn = gun.GetComponentInChildren<PlayerWeapon>();
            wpn.ammoClip = playerData.inventory[i].ammoClip;
            wpn.ammoStock = playerData.inventory[i].ammoStock;
            wpn.fireCounter = playerData.inventory[i].fireCounter;

            weaponInventory.Add(playerData.inventory[i].weaponIndex, gun);
            gun.SetActive(false);
        }

        for (int j = 0; j < inventoryMaxSize; j++)
        {
            if (playerData.ammoReserve.ContainsKey(j))
            {
                ammoReserve.Add(j, playerData.ammoReserve[j]);
            }
        }

        //Debug.Log("Active weapon is: " + playerData.activeWeaponIndex);
        doorKeys = playerData.doorKeys;
        abilities = SaveManager.Instance.gameState.player.abilities;
        abilityCurrent = SaveManager.Instance.gameState.player.abilityCounter;
        abilityIndex = SaveManager.Instance.gameState.player.activeAbilityIndex;
        abilityActive = SaveManager.Instance.gameState.player.abilityActive;
        StartCoroutine(SetActiveWeapon(playerData.activeWeaponIndex));

        GiveAbilityWeapon();

        UIManager.Instance.UpdateAbilityIcon(abilityIndex, abilityCurrent, abilityActive);
        if(abilityActive)
        {
            AbilitySetActive();
        }   
    }

    public void RemoveKeys()
    {
        doorKeys.Clear();
    }

    void DeathHolsterWeapon(EnumContainer.DamageInflictor damageInflictor)
    {
        AllowWeaponSwitching = false;
        StartCoroutine(HolsterWeapon());
    }

    /// <summary>
    /// Hides current weapon from player
    /// </summary>
    /// <returns></returns>
    IEnumerator HolsterWeapon()
    {
        if (activeWeapon == null)
            yield break;

        bSwitchingWeapons = true;
        PlayerWeapon wpn = activeWeapon.GetComponentInChildren<PlayerWeapon>();
        float waitTime = wpn.weaponAnimator.GetAnimLenght(4);
        wpn.SwitchFromWeapon();
        yield return new WaitForSeconds(waitTime);
        activeWeapon.SetActive(false);
        activeWeapon = null;
        bSwitchingWeapons = false;
    }

    void ClearInventory()
    {
        for(int i = 0; i < inventoryMaxSize; i++)
        {
            if(weaponInventory.ContainsKey(i))
            {
                Destroy(weaponInventory[i]);
            }
        }

        weaponInventory.Clear();
        doorKeys.Clear();
    }

    /// <summary>
    /// Called when player ability gets activated
    /// </summary>
    /// <param name="ability"></param>
    void AbilityActivated(EnumContainer.PLAYERABILITY ability)
    {
        if (ability == EnumContainer.PLAYERABILITY.DAMAGE)
            meleeWeaponIndex = 12;

        if (ability == EnumContainer.PLAYERABILITY.TIME)
            abilityDrainRate *= 2;
    }

    /// <summary>
    /// Called when player ability gets deactivated
    /// </summary>
    /// <param name="ability"></param>
    void AbilityDeactivated(EnumContainer.PLAYERABILITY ability)
    {
        if (ability == EnumContainer.PLAYERABILITY.DAMAGE)
            meleeWeaponIndex = 1;

        if(ability == EnumContainer.PLAYERABILITY.TIME)
            abilityDrainRate /= 2;
    }

    // Public methods

    /// <summary>
    /// Add a weapon to the player inventory
    /// </summary>
    /// <param name="weaponPrefab">weapon prefab to spawn on player</param>
    /// <param name="changeToWeapon">should we change to this weapon on pickup?</param>
    public void AddWeapon(GameObject weaponPrefab, bool changeToWeapon = true)
    {
        PlayerWeapon weapon = weaponPrefab.GetComponentInChildren<PlayerWeapon>();
        int inventorySlot = weapon.inventorySlot;

        // Weapon already exists, just give ammo
        if (weaponInventory.ContainsKey(inventorySlot))
        {
            //Debug.Log("Weapon inventory contains weapon");
            int defaultReserve = weapon.clipDefault + weapon.stockDefault;
            AddAmmo(defaultReserve, inventorySlot);
            return;
        }

        GameObject gun = Instantiate(weaponPrefab, gunContainer.position, gunContainer.rotation);
        gun.transform.SetParent(gunContainer);
        weaponInventory.Add(inventorySlot, gun);
        gun.SetActive(false);

        PlayerWeapon wpn = gun.GetComponentInChildren<PlayerWeapon>();
        wpn.ammoClip = weapon.clipDefault;
        wpn.ammoStock = weapon.stockDefault;

        // If already switching weapons, don't change weapon
        if(bSwitchingWeapons || !changeToWeapon)
            AddReserveAmmo(inventorySlot);
        else
            StartCoroutine(SetActiveWeapon(inventorySlot));
    }

    /// <summary>
    /// Add ammo for player inventory (for example ammo pickups)
    /// </summary>
    /// <param name="amount">the amount of ammo to add</param>
    /// <param name="weaponIndex">what weapon is this ammo for</param>
    /// <returns>If ammo adding was succesful or not</returns>
    public bool AddAmmo(int amount, int weaponIndex)
    {
        if(weaponInventory.ContainsKey(weaponIndex))
        {
            GameObject weapon = weaponInventory[weaponIndex];
            if(weapon == null)
            {
                return false;
            }

            PlayerWeapon weaponInfo = weapon.GetComponentInChildren<PlayerWeapon>();
            if(weaponInfo.clipOnly)
            {
                if (weaponInfo.ammoClip >= weaponInfo.clipSize)
                    return false;
            }
            
            else
            {
                if (weaponInfo.ammoStock >= weaponInfo.stockSize)
                    return false;
            }       
        }

        if (ammoReserve.ContainsKey(weaponIndex))
            ammoReserve[weaponIndex] += amount;
        else
            ammoReserve[weaponIndex] = amount;

        if (weaponSound != null)
            aSource.PlayOneShot(weaponSound);

        AddReserveAmmo(weaponIndex);
        return true;
    }

    public bool AddKey(int keyID)
    {
        for(int i = 0; i < doorKeys.Count; ++i)
        {
            if (doorKeys[i] == keyID)
                return false;
        }

        doorKeys.Add(keyID);
        return true;
    }

    public bool HasKey(int keyID)
    {
        if (doorKeys.Contains(keyID))
            return true;

        return false;
    }

    /// <summary>
    /// Add an ability to for the player
    /// </summary>
    /// <param name="index">Ability index</param>
    /// <param name="ability"></param>
    public void AddAbility(int index, EnumContainer.PLAYERABILITY ability)
    {
        if(abilities.ContainsKey(index))
        {
            ChargeAbility(abilityMaxValue / 2);
            return;
        }

        abilities.Add(index, ability);
        UIManager.Instance.UpdateAbilityIcon(abilityIndex, abilityCurrent, abilityActive);
    }

    /// <summary>
    /// Charge ability with given amount
    /// </summary>
    /// <param name="amount">the amount to add to ability (brtween 0 to 100)</param>
    public void ChargeAbility(float amount)
    {
        if (abilityActive)
            return;

        if (abilityCurrent >= abilityMaxValue)
            return;

        // No abilities obtained yet, don't update counter
        if (abilities.Keys == null || abilities.Keys.Count < 0)
            return;

        abilityCurrent += amount;
        if (abilityCurrent > abilityMaxValue)
            abilityCurrent = abilityMaxValue;

        abilityTimer = 0;
        abilityDecayed = false;

        UIManager.Instance.UpdateAbilityBar(abilityCurrent);
        UIManager.Instance.UpdateAbilityIcon(abilityIndex, abilityCurrent, abilityActive);
    }
}

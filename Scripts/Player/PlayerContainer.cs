using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

/// <summary>
/// A parent class that contains player specific information, in order
/// to make saving / loading less cluttered
/// </summary>
public class PlayerContainer : MonoBehaviour
{
    public static PlayerContainer Instance;

    PlayerInventory playerInventory = null;
    PlayerMovement playerMovement = null;
    PlayerHealth playerHealth = null;
    CameraShake playerCamShake = null;

    // Getters / setters
    public PlayerHealth PlayerHealth { get { return playerHealth; } private set { playerHealth = value; } }
    public PlayerInventory PlayerInventory { get { return playerInventory; } private set { playerInventory = value; } }
    public PlayerMovement PlayerMovement { get { return playerMovement; } private set { playerMovement = value; } }
    public bool PlayerAbilityActive { get; set; }

    public event Action OnPlayerSave;
    public event Action OnPlayerLoad;

    public event Action<EnumContainer.PLAYERABILITY> OnAbilityActivate;
    public event Action<EnumContainer.PLAYERABILITY> OnAbilityDeactivate;

    public event Action<EnumContainer.DamageInflictor> OnPlayerTakeDamage;
    public event Action<EnumContainer.DamageInflictor> OnPlayerDeath;

    Coroutine burningCoroutine;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        playerInventory = GetComponentInChildren<PlayerInventory>();
        playerMovement = GetComponentInChildren<PlayerMovement>();
        playerHealth = GetComponentInChildren<PlayerHealth>();
        playerCamShake = GetComponentInChildren<CameraShake>();

        SaveManager.Instance.OnGameSaved += SavePlayer;
        SaveManager.Instance.OnGameLoaded += LoadPlayer;
    }

    private void OnDestroy()
    {
        SaveManager.Instance.OnGameLoaded -= SavePlayer;
        SaveManager.Instance.OnGameLoaded -= LoadPlayer;
    }

    public void SavePlayer()
    {
        SaveManager.Instance.gameState.player = new SaveManager.SaveData.PlayerData();
        OnPlayerSave?.Invoke();
    }

    public void LoadPlayer()
    {
        OnPlayerLoad?.Invoke();
    }

    public void LoadPlayerStats()
    {
        playerInventory.LoadInventory();
        playerInventory.RemoveKeys();
        playerHealth.LoadHealth();
    }

    // Public methods
    public void TakeDamage(int damage, Transform attacker, bool shakeCamera = false, EnumContainer.DAMAGETYPE damageType = EnumContainer.DAMAGETYPE.Undefined)
    {
        if (!playerHealth.IsAlive)
            return;

        EnumContainer.DamageInflictor damageInflictor = new EnumContainer.DamageInflictor(damage, attacker, damageType);

        OnPlayerTakeDamage?.Invoke(damageInflictor);
        if(!playerHealth.IsAlive)
            OnPlayerDeath?.Invoke(damageInflictor);

        if(shakeCamera)
        {
            playerCamShake.ShakeCamera(damage / 10, damage / 50);
        }
    }

    public void UpdateAbility(float amount)
    {
        playerInventory.ChargeAbility(amount);
    }

    public void EnableAbility(EnumContainer.PLAYERABILITY ability)
    {
        OnAbilityActivate?.Invoke(ability);
        if(ability == EnumContainer.PLAYERABILITY.TIME)
        {
            GameManager.Instance.SetTimeScale(0.5f);
        }
    }

    public void DisableAbility(EnumContainer.PLAYERABILITY ability)
    {
        OnAbilityDeactivate?.Invoke(ability);
        if (ability == EnumContainer.PLAYERABILITY.TIME)
        {
            GameManager.Instance.SetTimeScale(1);
        }
    }

    public void SetPlayerBurning(bool burning, int damage = 0, float duration = 0.5f)
    {
        if(burning)
        {
            if(burningCoroutine == null)
                burningCoroutine = StartCoroutine(PlayerBurning(damage, duration));
        }

        else
        {
            if (burningCoroutine != null)
            {
                StopCoroutine(burningCoroutine);
                burningCoroutine = null;
            }
                
        }
    }

    IEnumerator PlayerBurning(int damage, float burnDuration)
    {
        float interval = 0.1f;
        while(burnDuration > 0)
        {
            TakeDamage(damage, null, false, EnumContainer.DAMAGETYPE.Fire);
            yield return new WaitForSeconds(interval);
            burnDuration -= interval;
            // TODO: It burns, aahh, aahh, aaahhh
        }

        burningCoroutine = null;
        yield return null;
    }
}

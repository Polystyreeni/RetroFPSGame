using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    private int health = 100;
    private int fullHealth = 100;
    private int maxHealth = 200;

    AudioSource aSource = null;

    [SerializeField]
    private AudioClip hurtSound = null;

    [SerializeField]
    private AudioClip deathSound = null;

    private PlayerContainer playerContainer = null;

    public int Health { get { return health; }}
    public int FullHealth { get { return fullHealth; } }
    public int MaxHealth { get { return maxHealth; } }

    private bool bIsAlive = true;
    public bool IsAlive { get { return bIsAlive; } private set { bIsAlive = value; } }

    private void Start()
    {
        aSource = GetComponentInParent<AudioSource>();
        playerContainer = GetComponentInParent<PlayerContainer>();
        playerContainer.OnPlayerTakeDamage += ChangeHealth;
        playerContainer.OnPlayerSave += SaveHealth;
        playerContainer.OnPlayerLoad += LoadHealth;

        UIManager.Instance.UpdateHealth(health);
    }

    private void OnDestroy()
    {
        playerContainer.OnPlayerTakeDamage -= ChangeHealth;
        playerContainer.OnPlayerSave -= SaveHealth;
        playerContainer.OnPlayerLoad -= LoadHealth;
    }

    public void ChangeHealth(EnumContainer.DamageInflictor damageInflictor)
    {
        // Player already dead, no damage added
        if (health <= 0)
            return;

        health -= damageInflictor.damage;
        if (health < 0)
            health = 0;

        UIManager.Instance.UpdateHealth(health);
        
        if (damageInflictor.damage > 0)
        {
            if(damageInflictor.damageType == EnumContainer.DAMAGETYPE.Fire)
            {
                UIManager.Instance.HealthChangeIndication(Color.white, 1f, .8f, damageInflictor.damageType);
            }

            else
            {
                UIManager.Instance.HealthChangeIndication(Color.red, .2f, 0.5f, damageInflictor.damageType);
                FxManager.Instance.PlayFX("fx_impact_blood_small", transform.position, Quaternion.identity);
            }
            
            if (health <= 0)
            {
                IsAlive = false;
                aSource.PlayOneShot(deathSound);
                UIManager.Instance.BPlayerDead = true;
                return;
            }

            aSource.PlayOneShot(hurtSound);
        }

        // Damage is negative, meaning add health instead
        else
        {
            UIManager.Instance.HealthChangeIndication(Color.white, .1f, 0.3f);
            if (health > maxHealth)
                health = maxHealth;

        }
    }

    void SaveHealth()
    {
        SaveManager.Instance.gameState.player.health = health;
    }

    public void LoadHealth()
    {
        health = SaveManager.Instance.gameState.player.health;
        UIManager.Instance.UpdateHealth(health);
    }
}

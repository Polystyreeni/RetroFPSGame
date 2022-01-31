using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Enemy", menuName = "Enemy", order = 52)]
public class Enemy : ScriptableObject
{
    [Header("State Timers")]
    [SerializeField] private float enemySeeDistance = 1f;
    [SerializeField] private float enemyReactionTime = 0.5f;
    [SerializeField] private float enemySpeed = 1f;
    [SerializeField] private float enemyHeight = 2f;
    [SerializeField] private float enemyRadius = 0.5f;

    [Header("Shooting")]
    [SerializeField] private float shootRange = 1f;
    [SerializeField] private float shootTime = 1f;
    [SerializeField] private float shootCooldown = 1f;
    [SerializeField] private float accuratyRange = 0f;
    [SerializeField] private float projectileSpeed = 1f;
    [SerializeField] private int shootCount = 1;
    [SerializeField] private int baseDamage = 10;
    [SerializeField] private int enemyMultiShotChance = 0;

    [Header("Health & Pain")]
    [SerializeField] private int health = 1;
    [SerializeField] private int painChance = 1;
    [SerializeField] private int gibChance = 1;
    [SerializeField] private bool fleeOnSight = false;

    [Header("Assignables")]
    [SerializeField] private GameObject projectile = null;
    [SerializeField] private GameObject deathModel = null;
    [SerializeField] private GameObject headModel = null;

    [Header("FX")]
    [SerializeField] private string hitFX = string.Empty;
    [SerializeField] private string gibFX = string.Empty;

    [Header("Audio")]
    [SerializeField] private AudioClip[] vox_notice = null;
    [SerializeField] private AudioClip[] vox_shoot = null;
    [SerializeField] private AudioClip[] vox_hurt = null;
    [SerializeField] private AudioClip[] vox_death = null;
    [SerializeField] private AudioClip[] vox_search = null;
    
    public float GetEnemySeeDistance()
    {
        return enemySeeDistance;
    }

    public float GetEnemyReactionTime()
    {
        return enemyReactionTime;
    }

    public float GetEnemySpeed()
    {
        return enemySpeed;
    }

    public float GetEnemyHeight()
    {
        return enemyHeight;
    }

    public float GetEnemyRadius()
    {
        return enemyRadius;
    }

    public float GetShootRange()
    {
        return shootRange;
    }

    public float GetAccuratyRange()
    {
        return accuratyRange;
    }

    public float GetShootTime()
    {
        return shootTime;
    }

    public float GetShootCooldown()
    {
        return shootCooldown;
    }

    public float GetProjectileSpeed()
    {
        return projectileSpeed;
    }

    public int GetShootCount()
    {
        return shootCount;
    }

    public int GetMultiShotChance()
    {
        return enemyMultiShotChance;
    }

    public int GetHealth()
    {
        return health;
    }

    public int GetPainChance()
    {
        return painChance;
    }

    public int GetGibChance()
    {
        return gibChance;
    }

    public int GetDamage()
    {
        return baseDamage;
    }

    public bool GetFleeOnSight()
    {
        return fleeOnSight;
    }

    public GameObject GetProjectile()
    {
        return projectile;
    }

    public GameObject GetDeathModel()
    {
        return deathModel;
    }

    public GameObject GetHeadModel()
    {
        return headModel;
    }

    public string GetHitFX()
    {
        return hitFX;
    }

    public string GetGibFX()
    {
        return gibFX;
    }

    public AudioClip[] GetNoticeVox()
    {
        return vox_notice;
    }

    public AudioClip[] GetShootVox()
    {
        return vox_shoot;
    }

    public AudioClip[] GetHurtVox()
    {
        return vox_hurt;
    }

    public AudioClip[] GetDeathVox()
    {
        return vox_death;
    }

    public AudioClip[] GetSearchVox()
    {
        return vox_search;
    }
}


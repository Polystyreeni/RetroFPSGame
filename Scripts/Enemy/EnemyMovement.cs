using System.Collections;
using System;
using UnityEngine;
using UnityEngine.AI;

public class EnemyMovement : MonoBehaviour
{
    [System.Serializable]
    public enum ENEMY_STATE
    {
        Idle,
        Patrol,
        Chase,
        Fire,
        Pain,
        Cover,
        Frozen,
        Burning,
    }

    public enum ENEMY_TYPE
    {
        HitScan,
        Grenadier,
        Civilian
    }

    [Header("States, Type & Appearance")]
    [SerializeField] private ENEMY_STATE currentState = ENEMY_STATE.Idle;
    [SerializeField] private ENEMY_STATE defaultState = ENEMY_STATE.Idle;
    [SerializeField] private ENEMY_TYPE enemyType = ENEMY_TYPE.HitScan;
    [SerializeField] private EnumContainer.DAMAGETYPE damageImmunity = EnumContainer.DAMAGETYPE.Undefined;
    private ENEMY_STATE savedState;

    public ENEMY_STATE CurrentState { get { return currentState; } set { currentState = value; } }

    public ENEMY_TYPE EnemyType { get { return enemyType; } private set { enemyType = value; } }
    [SerializeField] private EnumContainer.DIFFICULTY[] levelOfAppearance;  // Assing through inspector

    // Assing through inspector
    [Header("Assignables")]
    [SerializeField] private LayerMask whatIsWall;
    [SerializeField] private Enemy enemyData = null;
    [SerializeField] private Animator animator = null;
    [SerializeField] private Transform target = null;
    [SerializeField] private Transform headPosition = null;
    [SerializeField] private GameObject[] dropItems = null;
    [SerializeField] private GameObject burningObject = null;
    [SerializeField] private Transform slopeCheck = null;
    [SerializeField] private Transform groundCheck = null;

    public Animator EAnimator { get { return animator; } private set { animator = value; } }
    public Transform Target { get { return target; } set { target = value; } }
    public LayerMask WhatIsWall { get { return whatIsWall; } }
    public float EnemyReactionTime { get { return enemyReactionTime; } private set { enemyReactionTime = value; } }
    public float EnemySpeed { get { return enemySpeed; } set { enemySpeed = value; } }
    public bool CanShoot { get { return bCanShoot; } private set { bCanShoot = value; } }

    public bool EnableStateChange { get; set; }

    // ========= Will be loaded from above file ========
    private float enemySeeDistance = 0f;
    private float enemyShootDistance = 0f;
    private float enemyReactionTime = 0.5f;
    private float enemySpeed = 1f;
    private float enemyHeight = 2f;
    private float enemyRadius = 0.5f;
    private float enemyShootTime = 1f;
    private float enemyShootCooldown = 1f;
    private int enemyShootCount = 1;
    private int enemyMultiShotChance = 0;
    private int enemyPainChance = 1;
    private int enemyGibChance = 1;
    private int maxHealth = 100;
    private int enemyHealth = 100;
    private GameObject deathModel = null;
    private GameObject headModel = null;
    private string hitFX = string.Empty;
    private string gibFX = string.Empty;
    [HideInInspector] public int EnemyHealth { get { return enemyHealth; } }

    [Header("Variables")]
    [SerializeField] private float enemyFov = 45f;
    [SerializeField] private float targetUpdateRate = 1f;
    [SerializeField] private float navigationAreaRadius = 0f;   // When > 0, enemies don't path directly towards target for more randomized movement

    private bool bCanShoot = true;
    private bool bSeenPlayer = false;
    private bool onRamp = false;
    private Transform killedBy = null;
    private string enemyID;

    private GameObject dropItem = null;

    AudioSource aSource = null;
    private Billboard spriteAnimator = null;

    // Events
    public event Action OnEnemyDamage;  // Called when enemy takes damage, after other damaging action
    public event Action OnEnemyFire;    // Called at the start of firing

    private Coroutine burningCoroutine;
    private GameObject fireModel = null;
    private Rigidbody rb = null;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        spriteAnimator = GetComponentInChildren<Billboard>();
        aSource = GetComponent<AudioSource>();
    }
    // Start is called before the first frame update
    void Start()
    {
        if(GameManager.Instance != null)
            GameManager.Instance.OnGameStateLoaded += EnemyAppearance;

        if(SaveManager.Instance != null)
        {
            SaveManager.Instance.OnGameSaved += SaveEnemy;
            SaveManager.Instance.OnGameLoaded += LoadEnemy;
        }

        InitializeData();

        EnableStateChange = true;
        if (target != null)
            ChangeState(ENEMY_STATE.Chase);

        else
            ChangeState(defaultState);
    }

    /// <summary>
    /// Determines if the enemy should be spawned when on current difficulty
    /// </summary>
    void EnemyAppearance()
    {
        bool shouldAppear = false;
        foreach (var difficultyLevel in levelOfAppearance)
        {
            if (difficultyLevel == GameManager.Instance.DifficultyLevel)
            {
                shouldAppear = true;
                break;
            }
        }

        if (!shouldAppear)
        {
            Destroy(gameObject);
            return;
        }
    }

    public virtual void OnDestroy()
    {
        SaveManager.Instance.OnGameSaved -= SaveEnemy;
        SaveManager.Instance.OnGameLoaded -= LoadEnemy;
        GameManager.Instance.OnGameStateLoaded -= EnemyAppearance;
    }

    /// <summary>
    /// Load data from the enemy file
    /// </summary>
    void InitializeData()
    {
        if (enemyData != null)
        {
            enemySeeDistance = enemyData.GetEnemySeeDistance();
            enemyShootDistance = enemyData.GetShootRange();
            enemySpeed = enemyData.GetEnemySpeed();
            enemyHeight = enemyData.GetEnemyHeight();
            enemyRadius = enemyData.GetEnemyRadius();
            enemyReactionTime = enemyData.GetEnemyReactionTime();
            enemyShootTime = enemyData.GetShootTime();
            enemyShootCooldown = enemyData.GetShootCooldown();
            enemyShootCount = enemyData.GetShootCount();
            enemyMultiShotChance = enemyData.GetMultiShotChance();
            enemyPainChance = enemyData.GetPainChance();
            enemyGibChance = enemyData.GetGibChance();
            maxHealth = enemyData.GetHealth();
            enemyHealth = enemyData.GetHealth();
            deathModel = enemyData.GetDeathModel();
            headModel = enemyData.GetHeadModel();
            hitFX = enemyData.GetHitFX();
            gibFX = enemyData.GetGibFX();
        }

        InitializeStats();

        enemyID = gameObject.name + transform.position.ToString();
        if(dropItems.Length > 0)
            dropItem = dropItems[UnityEngine.Random.Range(0, dropItems.Length)];
    }

    /// <summary>
    /// Set difficulty level specific stats for enemy
    /// </summary>
    void InitializeStats()
    {
        if (GameManager.Instance.DifficultyLevel < EnumContainer.DIFFICULTY.MEDIUM)
        {
            enemyShootDistance *= 0.8f;
            enemyReactionTime *= 1.25f;
            enemyHealth = (int)(enemyHealth * 0.8f);
        }

        else if(GameManager.Instance.DifficultyLevel == EnumContainer.DIFFICULTY.MEDIUM)
        {
            enemyShootDistance *= 0.9f;
            enemyReactionTime *= 1.05f;
        }

        else if (GameManager.Instance.DifficultyLevel > EnumContainer.DIFFICULTY.HARD)
        {
            enemyReactionTime = 0.05f;
            enemyShootDistance *= 1.5f;
            enemyShootCooldown *= 0.2f;
            enemySpeed *= 1.3f;
        }
    }

    /// <summary>
    /// Set the current state for enemy in FSM
    /// </summary>
    /// <param name="state"></param>
    public void ChangeState( ENEMY_STATE state )
    {
        //if (currentState == state && (state != ENEMY_STATE.Idle && state != ENEMY_STATE.Patrol))
        //    return;

        if (!EnableStateChange)
            return;

        StopAllCoroutines();

        switch (state)
        {
            case ENEMY_STATE.Idle:
                StartCoroutine(EnemyIdle());
                break;

            case ENEMY_STATE.Patrol:
                StartCoroutine(EnemyPatrol());
                break;

            case ENEMY_STATE.Chase:
                StartCoroutine(EnemyChase());
                break;

            case ENEMY_STATE.Fire:
                StartCoroutine(EnemyFire());
                break;

            case ENEMY_STATE.Pain:
                StartCoroutine(EnemyPain());
                break;

            case ENEMY_STATE.Cover:
                StartCoroutine(EnemyCover());
                break;

            case ENEMY_STATE.Frozen:
                StartCoroutine(EnemyFrozen());
                break;

            case ENEMY_STATE.Burning:
                StartCoroutine(EnemyBurning());
                break;
        }
    }

    // A method for changing the state without cancelling and reactivating coroutines, used for enemy jumping
    public void ChangeStateHidden( ENEMY_STATE state )
    {
        // Store the previous state so we can return it, once flying is done
        savedState = currentState;
        currentState = state;
    }

    public virtual IEnumerator EnemyIdle()
    {
        currentState = ENEMY_STATE.Idle;     
        animator.SetFloat("enemyMovement", 0);
        while (true)
        {
            if (CheckForEnemy())
                break;

            else
                yield return new WaitForSeconds(.1f);

        }

        yield return null;
    }

    /// <summary>
    /// Check to see if enemy in idle / patrol state should start chasing player
    /// </summary>
    /// <returns>If enemy sees player</returns>
    private bool CheckForEnemy()
    {
        if (target == null)
        {
            target = FindTarget();
            return false;
        }

        Vector3 offset = target.transform.position - transform.position;
        float sqrLen = offset.sqrMagnitude;

        if (sqrLen <= enemySeeDistance * enemySeeDistance)
        {
            //Enemy is close, check direction
            if (EnemyCanSeeTarget(target))
            {
                bSeenPlayer = true;
                if (enemyType == ENEMY_TYPE.Civilian)
                {
                    ChangeState(ENEMY_STATE.Cover);
                    return true;
                }

                else
                {
                    // Chance to shoot enemy instantly in harder modes
                    bool shoot = UnityEngine.Random.Range(0, 2) == 1;
                    if (shoot && GameManager.Instance.DifficultyLevel > EnumContainer.DIFFICULTY.MEDIUM && IsValidShot())
                    {
                        //Debug.Log("EnemyMovement: Instant firing");
                        ChangeState(ENEMY_STATE.Fire);
                    }

                    else
                    {
                        ChangeState(ENEMY_STATE.Chase);
                    }
                }

                if (enemyData.GetNoticeVox() != null)
                {
                    AudioClip[] clips = enemyData.GetNoticeVox();
                    aSource.PlayOneShot(clips[UnityEngine.Random.Range(0, clips.Length)]);
                }

                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// State for patroling (roaming around the map)
    /// </summary>
    /// <returns></returns>
    public virtual IEnumerator EnemyPatrol()
    {
        ENEMY_STATE prevState = currentState;
        currentState = ENEMY_STATE.Patrol;

        spriteAnimator.BUpdateRotation = true;
        animator.SetFloat("enemyMovement", 1);

        NavMeshPath path = new NavMeshPath();
        yield return StartCoroutine(FollowPath(path, null));

        ChangeState(prevState);
    }

    /// <summary>
    /// State for enemy chasing its target
    /// </summary>
    /// <returns></returns>
    public virtual IEnumerator EnemyChase()
    {
        Debug.Log("Enemy Chase");
        ENEMY_STATE prevState = currentState;
        currentState = ENEMY_STATE.Chase;
        spriteAnimator.BUpdateRotation = true;
        bCanShoot = false;

        NavMeshPath path = new NavMeshPath();

        animator.SetFloat("enemyMovement", 1);
        animator.SetBool("enemyShoot", false);

        if (target == null || !TargetIsValid(target))
        {
            target = FindTarget();
            if (target == null)
            {
                ChangeState(defaultState);
                yield break;
            }
        }
        
        Invoke(nameof(EnableShooting), enemyShootCooldown);
        yield return StartCoroutine(FollowPath(path, target));

        // Force chasing player, if player has attacked this enemy previously
        ChangeState(prevState);
    }

    /// <summary>
    /// State for enemy attacking
    /// </summary>
    /// <returns></returns>
    public virtual IEnumerator EnemyFire()
    {
        ENEMY_STATE prevState = currentState;
        currentState = ENEMY_STATE.Fire;
        bCanShoot = false;

        if(target == null)
        {
            ChangeState(prevState);
            yield break;
        }
            
        // Make sure object rotates with the enemy of this actor
        Vector3 LookAtDir = new Vector3(target.position.x - transform.position.x, 0, target.position.z - transform.position.z);
        transform.rotation = Quaternion.LookRotation(LookAtDir.normalized, Vector3.up);

        yield return new WaitForEndOfFrame();

        // Disable sprite rotations
        spriteAnimator.BUpdateRotation = false;

        if (enemyReactionTime > 0f)
            yield return new WaitForSeconds(enemyReactionTime);

        for(int i = 0; i < enemyShootCount; i++)
        {
            animator.SetBool("enemyShoot", true);

            AudioClip[] aSound = enemyData.GetShootVox();
            if(aSound.Length > 0)
                aSource.PlayOneShot(aSound[UnityEngine.Random.Range(0, aSound.Length)]);

            OnEnemyFire?.Invoke();

            yield return new WaitForSeconds(enemyShootTime);

            // Should we shoot again ?
            if (!EnemyCanSeeTarget(target) || enemyMultiShotChance > UnityEngine.Random.Range(0, 100))
                break;
        }

        //Debug.Log("EnemyMovement: Enemy stopped firing");

        animator.SetBool("enemyShoot", false);
        spriteAnimator.BUpdateRotation = true;

        // Make sure object rotates with the enemy of this actor
        if(target != null)
        {
            LookAtDir = new Vector3(target.position.x - transform.position.x, 0, target.position.z - transform.position.z);
            transform.rotation = Quaternion.LookRotation(LookAtDir.normalized, Vector3.up);
        }
        
        ChangeState(ENEMY_STATE.Chase);
    }

    public virtual IEnumerator EnemyCover()    // TODO: Add logic here to find a position where the player can't see
    {
        ENEMY_STATE prevState = currentState;
        currentState = ENEMY_STATE.Cover;

        spriteAnimator.BUpdateRotation = true;
        animator.SetFloat("enemyMovement", 1);

        NavMeshPath path = new NavMeshPath();
        float fleeTime = UnityEngine.Random.Range(1f, 3f);
        yield return StartCoroutine(FollowPath(path, null, fleeTime));

        Debug.Log("Cover state finished");

        ChangeState(prevState);
    }

    /// <summary>
    /// State for displaying pain, cancels firing and chasing
    /// </summary>
    /// <returns></returns>
    public virtual IEnumerator EnemyPain()
    {
        currentState = ENEMY_STATE.Pain;
        bCanShoot = false;

        spriteAnimator.BUpdateRotation = true;

        animator.SetBool("enemyShoot", bCanShoot);
        animator.SetBool("enemyPain", true);

        // Play a pain sound
        if (enemyData.GetHurtVox() != null && enemyData.GetHurtVox().Length > 0)
        {
            int aSize = enemyData.GetHurtVox().Length;
            aSource.PlayOneShot(enemyData.GetHurtVox()[UnityEngine.Random.Range(0, aSize)]);
        }

        float wait = animator.GetCurrentAnimatorStateInfo(0).length;
        //Debug.Log("Pain Lenght: " + wait);
        yield return new WaitForSeconds(wait);

        animator.SetBool("enemyPain", false);
        ChangeState(ENEMY_STATE.Chase);
    }

    public virtual IEnumerator EnemyFrozen()
    {
        currentState = ENEMY_STATE.Frozen;
        // TODO: Make Rigidbody kinematic
        bCanShoot = false;

        animator.SetBool("enemyShoot", false);
        animator.SetFloat("enemyMovement", 0);

        while(currentState == ENEMY_STATE.Frozen)
        {
            yield return new WaitForEndOfFrame();
        }
    }

    /// <summary>
    /// State for handling death by fire
    /// </summary>
    /// <returns></returns>
    public virtual IEnumerator EnemyBurning()
    {
        if (enemyHealth <= 0)
            yield break;

        currentState = ENEMY_STATE.Burning;
        fireModel = Instantiate(burningObject, transform.position, transform.rotation);

        CanShoot = false;
        EnableStateChange = false;
        spriteAnimator.BUpdateRotation = true;
        animator.SetBool("enemyShoot", false);
        animator.SetFloat("enemyMovement", 1);

        NavMeshPath path = new NavMeshPath();
        float fleeTime = UnityEngine.Random.Range(1.5f, 3f);
        yield return StartCoroutine(FollowPath(path, null, fleeTime));

        EnableStateChange = true;
        EnemyDeath(Vector3.zero, killedBy, false);
    }

    public void SetBurning(bool value, int damage = 1, float duration = 0f, Transform attacker = null)
    {
        if(value)
        {
            burningCoroutine = StartCoroutine(BurnEnemy(damage, duration, attacker));
        }
            
        else
        {
            if(burningCoroutine != null)
            {
                StopCoroutine(burningCoroutine);
                burningCoroutine = null;
            }   
        }
    }

    IEnumerator BurnEnemy(int damage, float duration, Transform attacker)
    {
        float interval = 0.1f;
        while(duration > 0)
        {
            if (enemyHealth <= damage && currentState != ENEMY_STATE.Burning)
            {
                killedBy = attacker;
                ChangeState(ENEMY_STATE.Burning);
                yield break;
            }

            TakeDamage(transform.position, GetComponent<CapsuleCollider>(), damage, attacker, false, false, 1, EnumContainer.DAMAGETYPE.Fire);
            // TODO: FX
            yield return new WaitForSeconds(interval);
            duration -= interval;
        }

        burningCoroutine = null;
        yield return null;
    }

    bool IsFloor(Vector3 v)
    {
        float angle = Vector3.Angle(Vector3.up, v);
        return angle < 45;
    }

    /// <summary>
    /// The enemy death state (remove enemy, spawn corpse)
    /// </summary>
    /// <param name="direction">Direction of damage</param>
    /// <param name="attacker">Damage inflictor</param>
    /// <param name="bHeadShot">Was this headshot damage</param>
    /// <param name="damage">the amount of damage</param>
    /// <param name="bExplosionDamage">Was this explosion damage</param>
    public virtual void EnemyDeath(Vector3 direction, Transform attacker, bool bHeadShot = false, int damage = 1, bool bExplosionDamage = false )
    {
        StopAllCoroutines();

        if (fireModel != null)
            Destroy(fireModel);

        // Increment kill count
        if(enemyType != ENEMY_TYPE.Civilian)
            GameManager.Instance.IncrementKillCount();

        // Disable collision on death
        Collider[] enemyCollider = GetComponentsInChildren<Collider>();
        if (enemyCollider != null)
        {
            foreach(Collider col in enemyCollider)
            {
                col.enabled = false;
            }
        }
            
        if (dropItem != null)
            Instantiate(dropItem, transform.position, transform.rotation);

        if(attacker != null && attacker.gameObject.CompareTag("Player"))
        {
            //Debug.Log("Player Killed enemy");
            PlayerContainer pc = attacker.GetComponentInParent<PlayerContainer>();
            if(pc != null)
            {
                pc.UpdateAbility(20);  // TODO: Make a variable for this
            }
        }

        // High damage dealt, possible to gib enemy
        if(damage > maxHealth * 2 || deathModel == null)
        {
            if(UnityEngine.Random.Range(0, 100) < enemyGibChance || deathModel == null)
            {
                FxManager.Instance.PlayFX(gibFX, transform.position, Quaternion.LookRotation(transform.forward, transform.up));
                Destroy(this.gameObject, 0.1f);
                return;
            }
        }

        GameObject corpse = Instantiate(deathModel, transform.position + new Vector3(0, 0.5f, 0), transform.rotation);
        if(bHeadShot)
        {
            Animator corpseAnimator = corpse.GetComponent<Animator>();
            if(corpseAnimator != null)
            {
                corpseAnimator.SetBool("bHeadShot", true);
            }

            GameObject head = Instantiate(headModel, headPosition.position, Quaternion.identity);
            if(head.TryGetComponent<Rigidbody>(out Rigidbody headRB))
            {
                headRB.AddForce(new Vector3(UnityEngine.Random.Range(-2, 2), 6, UnityEngine.Random.Range(-2, 2)), ForceMode.VelocityChange);
            }
        }

        // Play death sound
        AudioClip[] aSound = enemyData.GetDeathVox();
        AudioSource aSrc = corpse.GetComponent<AudioSource>();
        if(aSrc != null)
            aSrc.PlayOneShot(aSound[UnityEngine.Random.Range(0, aSound.Length)]);

        float forceMultiplier = 0.1f;
           
        Rigidbody rb = corpse.GetComponent<Rigidbody>();
        if(rb != null)
        {
            rb.velocity = direction * damage * forceMultiplier;
        }

        Destroy(this.gameObject, 0.1f);
    }

    public void EnableShooting()
    {
        bCanShoot = true;
    }

    public Transform FindTarget()
    {
        Transform player = GameObject.FindGameObjectWithTag("Player").transform;
        if (player != null)
        {
            // Previous target was an enemy and hasn't seen the player so don't chase
            if (currentState == ENEMY_STATE.Chase && !bSeenPlayer)
                return null;

            else if (!TargetIsValid(player))
                return null;

            else
                return player;
        }
            
        return null;
    }

    public bool TargetIsValid(Transform target)
    {
        if(target.gameObject.CompareTag("Player"))
        {
            PlayerHealth health = target.GetComponent<PlayerHealth>();
            if (!health.IsAlive)
                return false;

            return true;
        }

        EnemyMovement mov = target.gameObject.GetComponent<EnemyMovement>();
        if (mov == null)
            return false;

        if (mov.EnemyHealth <= 0)
            return false;

        return true;
    }

    public bool IsValidShot()
    {
        if (target == null)
            return false;

        Vector3 offset = target.position - transform.position;
        float sqrLen = offset.sqrMagnitude;

        if (sqrLen <= enemyShootDistance * enemyShootDistance)
        {
            if (EnemyCanSeeTarget(target, false))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Determine the path for enemy to follow when chasing
    /// </summary>
    /// <param name="path">the path to return</param>
    /// <param name="targetObject">the reference object to find a path to</param>
    /// <returns></returns>
    public NavMeshPath GetPath(NavMeshPath path, Transform targetObject)
    {
        Vector3 targetPosition;

        if (targetObject == null || targetObject == transform)
        {
            float checkRadius = navigationAreaRadius;
            if (checkRadius <= 0)
                checkRadius = enemyRadius * 4f;

            NavMeshHit hitt;
            targetPosition = rb.position + UnityEngine.Random.insideUnitSphere * checkRadius;
            if (NavMesh.SamplePosition(targetPosition, out hitt, checkRadius, NavMesh.AllAreas))
                targetPosition = hitt.position;

            else
                targetPosition = rb.position;
        }

        else
        {
            targetPosition = targetObject.position;
            if(navigationAreaRadius > 0)
            {
                NavMeshHit checkPoint;
                if (NavMesh.SamplePosition(targetPosition + UnityEngine.Random.insideUnitSphere * navigationAreaRadius, out checkPoint, navigationAreaRadius, NavMesh.AllAreas))
                    targetPosition = checkPoint.position;
            }
        }

        path.ClearCorners();
        bool success = NavMesh.CalculatePath(rb.position, targetPosition, NavMesh.AllAreas, path);
        if (!success)
        {
            //Debug.Log("NavPath: No succesful path found, using fallback");
            Vector3 closestPoint = rb.position;
            bool findClosest = NavMesh.SamplePosition(closestPoint + UnityEngine.Random.insideUnitSphere * 5f, out NavMeshHit hit, 5f, NavMesh.AllAreas);
            if (findClosest)
                closestPoint = hit.position;

            NavMesh.CalculatePath(rb.position, closestPoint, NavMesh.AllAreas, path);
        }

        return path;
    }

    /// <summary>
    /// Handles enemy moving along a given path
    /// </summary>
    /// <param name="path">Path to follow</param>
    /// <param name="targetObject">Reference point for path finding</param>
    /// <param name="followDuration">OPTIONAL: The duration of this state</param>
    /// <returns></returns>
    IEnumerator FollowPath(NavMeshPath path, Transform targetObject, float followDuration = 0f)
    {
        float elapsedTime = 0f;
        float totalTime = 0f;
        int pathIndex = 0;
        int pathFailCounter = 0;
        float audioCounter = 0f;

        Vector3 targetPos = Vector3.zero;
        Vector3 offset = Vector3.up * (enemyHeight / 2f);
        Vector3 targetDirection = transform.forward;

        // Get a random target position, if no target is defined
        if(targetObject == null)
        {
            targetObject = transform;
        }
            
        while (targetObject != null)
        {
            totalTime += Time.fixedDeltaTime;

            // End following, if timer expires (used for fleeing, not chasing)
            if (totalTime > followDuration && followDuration > 0f)
                yield break;

            // Allow target changing between paths
            if (targetObject != target && currentState == ENEMY_STATE.Chase)
            {
                //Debug.Log("Enemy Change");
                ChangeState(ENEMY_STATE.Chase);
                yield break;
            }

            // Make Rigidbody affected / not affected by physics
            RampCheck();

            if (bCanShoot && currentState == ENEMY_STATE.Chase)
            {
                if (IsValidShot())
                {
                    ChangeState(ENEMY_STATE.Fire);
                    yield break;
                }
            }

            if (currentState == ENEMY_STATE.Patrol)
            {
                if (CheckForEnemy())
                    break;
            }

            // Force movement to random direction
            if(pathFailCounter > 15)
            {
                if(targetDirection == Vector3.zero)
                {
                    targetPos = transform.position + UnityEngine.Random.insideUnitSphere * 5f;
                    targetDirection = targetPos - rb.position;
                    targetDirection.y = 0;
                    Vector3.Normalize(targetDirection);
                }

                MoveTowards(targetDirection, targetPos);
                pathFailCounter--;
                elapsedTime += Time.fixedDeltaTime;
                audioCounter += Time.fixedDeltaTime;
                yield return new WaitForFixedUpdate();
                continue;
            }

            // TODO: Make this a possible melee radius
            if (path.corners.Length <= 0 || elapsedTime > targetUpdateRate)
            {
                //Debug.Log("RB: Enemy get new path");
                GetPath(path, targetObject);
                if (path.corners.Length <= 0)
                {
                    targetDirection = Vector3.zero;
                    pathFailCounter++;
                    yield return null;
                    continue;
                }

                pathFailCounter = 0;
                pathIndex = 0;
                elapsedTime = 0;
            }

            // Don't constanty update path
            if (path.corners.Length < pathIndex)
            {
                //Debug.Log("RB: Path Index too high, Clearing path");
                path.ClearCorners();
                yield return null;
                continue;
            }

            // Don't update path if target withing attack range
            if ((targetObject.position - rb.position).sqrMagnitude < 1f && targetObject != transform)
            {
                //Debug.Log("RB: Target near player, not updating path");
                yield return null;
                continue;
            }

            // Set new path index, since we're at target position
            Vector2 targetPos2D = new Vector2(targetPos.x, targetPos.z);
            Vector2 currentPos2D = new Vector2(rb.position.x, rb.position.z);

            //Debug.LogFormat("Current Position {0}, target position {1}", targetPos2D, currentPos2D);

            if ((targetPos2D - currentPos2D).sqrMagnitude < 0.05f)
            {
                //Debug.Log("RB: Close to target position, set new index");
                Vector3 posToSet = path.corners[pathIndex];
                posToSet.y = rb.position.y;
                rb.MovePosition(posToSet);
                int newIndex = pathIndex + 1;
                if (newIndex >= path.corners.Length)
                {
                    //Debug.Log("RB: PathIndex too high, setting new path");
                    path.ClearCorners();
                    yield return null;
                    continue;
                }

                else
                {
                    pathIndex = newIndex;
                }
            }

            // Offset required to make RB target proper height
            targetPos = path.corners[pathIndex] + offset;

            // Move Enemy towards target position
            targetDirection = transform.forward;

            // No downwards motion
            if (targetObject.position.y <= rb.position.y)
            {
                targetDirection = new Vector3(targetPos.x - rb.position.x, 0, targetPos.z - rb.position.z).normalized;   //(targetPos - transform.position).normalized;
            }

            // Upwards motion for stairs
            else
            {
                targetDirection = (targetPos - rb.position).normalized;   //(targetPos - transform.position).normalized;
            }

            MoveTowards(targetDirection, targetPos);

            // Play a search sound
            if (currentState == ENEMY_STATE.Chase && audioCounter > 1 && enemyData.GetSearchVox() != null && enemyData.GetSearchVox().Length > 0)
            {
                audioCounter = 0;
                int aSize = enemyData.GetSearchVox().Length;
                if (UnityEngine.Random.Range(0, 100) >= 75)
                {
                    aSource.PlayOneShot(enemyData.GetSearchVox()[UnityEngine.Random.Range(0, aSize)]);
                }
            }

            elapsedTime += Time.fixedDeltaTime;
            audioCounter += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        yield return null;
    }


    /// <summary>
    /// Checks to see if Rigidbody is on a ramp. Set rb to kinematic accordingly
    /// </summary>
    void RampCheck()
    {
        RaycastHit hit;
        // We're in stairs / slope, check if enemy can climb or is it too high
        if (Physics.Raycast(groundCheck.position, transform.forward, out hit, enemyRadius + 0.1f, whatIsWall))
        {
            RaycastHit target;
            if (Physics.Raycast(slopeCheck.position, transform.forward, out target, enemyRadius + 0.2f, whatIsWall))
            {
                if (onRamp)
                {
                    onRamp = false;
                    rb.isKinematic = false;
                }
            }

            else
            {
                rb.isKinematic = true;
                onRamp = true;
                return;
            }
        }

        else
        {
            onRamp = false;
            rb.isKinematic = false;
        }

        RaycastHit hitLower45;
        if(Physics.Raycast(groundCheck.transform.position, transform.TransformDirection(1.5f, 0, 1), out hitLower45, enemyRadius + 0.1f, whatIsWall))
        {
            RaycastHit hitUpper45;
            if (Physics.Raycast(slopeCheck.transform.position, transform.TransformDirection(1.5f, 0, 1), out hitUpper45, enemyRadius + 0.2f, whatIsWall))
            {
                if (onRamp)
                {
                    onRamp = false;
                    rb.isKinematic = false;
                }
            }

            else
            {
                rb.isKinematic = true;
                onRamp = true;
                return;
            }
        }

        else
        {
            onRamp = false;
            rb.isKinematic = false;
        }

        RaycastHit hitLowerMinus45;
        if (Physics.Raycast(groundCheck.transform.position, transform.TransformDirection(-1.5f, 0, 1), out hitLowerMinus45, enemyRadius + 0.1f, whatIsWall))
        {
            RaycastHit hitUpperMinus45;
            if (Physics.Raycast(slopeCheck.transform.position, transform.TransformDirection(-1.5f, 0, 1), out hitUpperMinus45, enemyRadius + 0.2f, whatIsWall))
            {
                if (onRamp)
                {
                    onRamp = false;
                    rb.isKinematic = false;
                }
            }

            else
            {
                rb.isKinematic = true;
                onRamp = true;
                return;
            }
        }

        else
        {
            onRamp = false;
            rb.isKinematic = false;
        }
    }

    /// <summary>
    /// Moves rigidbody agent towards given direction, when target position is targetPos
    /// </summary>
    /// <param name="direction"></param>
    /// <param name="targetPos"></param>
    void MoveTowards(Vector3 direction, Vector3 targetPos)
    {
        // If we're on a ramp, move without physics to avoid getting stuck
        if (rb.isKinematic)
        {
            transform.position += direction * enemySpeed * Time.fixedDeltaTime;
        }

        else
        {
            rb.MovePosition(rb.position + direction * enemySpeed * Time.fixedDeltaTime);
        }

        Vector3 lookPos = new Vector3(targetPos.x, transform.position.y, targetPos.z);
        transform.LookAt(lookPos, Vector3.up);

        if (fireModel != null)
            fireModel.transform.position = rb.position;
    }

    bool EnemyCanSeeTarget( Transform target, bool sightCheck = true )
    {
        if (target == null)
            return false;

        Vector3 rayOrigin = headPosition.position;
        if (!sightCheck)
        {
            rayOrigin = transform.position;
        }

        // Check raycast first, that way we don't have to do the other math if this returns false
        RaycastHit hit;
        // Does the ray intersect any objects tagged as wall
        if (Physics.Raycast(rayOrigin, (target.transform.position - rayOrigin), out hit, enemySeeDistance, whatIsWall))
        {
            if (hit.collider.gameObject != target.gameObject)
            {
                return false;
            }
        }

        Vector3 forwardVec = transform.forward;
        Vector3 toPlayerVec = (target.transform.position - transform.position).normalized;

        return Vector3.Angle(forwardVec, toPlayerVec) <= enemyFov;
    }

    // Public methods


    /// <summary>
    /// Handles enemy damage
    /// </summary>
    /// <param name="point">Point of damage</param>
    /// <param name="collider">Enemy collider which took the damage</param>
    /// <param name="damage">Amount of damage</param>
    /// <param name="attacker">Damage inflictor</param>
    /// <param name="knockback">Should we apply knockback to enemy</param>
    /// <param name="bExplosionDamage">TO BE DEPRECIATED (HANDLED IN DAMAGETYOE)</param>
    /// <param name="headShotMultiplier">Damage multiplier for headShot</param>
    /// <param name="damageType">What type of damage was taken</param>
    public virtual void TakeDamage( Vector3 point, Collider collider, int damage, Transform attacker, bool knockback = true, bool bExplosionDamage = false, float headShotMultiplier = 2f, EnumContainer.DAMAGETYPE damageType = EnumContainer.DAMAGETYPE.Undefined )
    {
        // No damage, if immunity
        if (damageType == damageImmunity && damageImmunity != EnumContainer.DAMAGETYPE.Undefined)
            return;

        // No damage if enemy is already burning to death
        if (currentState == ENEMY_STATE.Burning)
            return;

        // Check if attacker is valid
        if(attacker != null && attacker.GetComponent<EnemyMovement>() != null)
        {
            // Only hitscan enemies can harm each other
            ENEMY_TYPE type = attacker.GetComponent<EnemyMovement>().EnemyType;
            if (type == enemyType && enemyType != ENEMY_TYPE.HitScan)
                return;
        }

        if(damageType != EnumContainer.DAMAGETYPE.Explosion && damageType != EnumContainer.DAMAGETYPE.Fire)
            FxManager.Instance.PlayFX(hitFX, point, Quaternion.identity);

        bool bHeadShot = collider.gameObject.name == "Head";

        float damageMultiplier = 1f;
        if (bHeadShot)
        {
            if (headShotMultiplier <= 0)
                headShotMultiplier = 2f;

            damageMultiplier = headShotMultiplier;
        }      

        enemyHealth -= (int)(damage * damageMultiplier);
        if(enemyHealth <= 0)
        {
            EnemyDeath((transform.position - point).normalized, attacker, bHeadShot, damage, bExplosionDamage);
            return;
        }

        // TODO: Check to see, if target should be changed
        if (attacker != null && attacker != transform)
        {
            target = attacker;
            if (attacker.gameObject.CompareTag("Player"))
                bSeenPlayer = true;
        }

        // Apply explosion Force
        if (bExplosionDamage)
        {
            FlingEnemy((transform.position - point).normalized, damage);
        }

        // Apply knockback
        else if(knockback)
        {
            Vector3 pushDirection = rb.position - point;
            pushDirection.y = 0;
            rb.AddForce(pushDirection.normalized * damage / 2f, ForceMode.Impulse);
        }

        // Should we enter pain state?
        if(currentState != ENEMY_STATE.Pain && damageType != EnumContainer.DAMAGETYPE.Fire)
        {
            int num = UnityEngine.Random.Range(0, 100);
            if (num < enemyPainChance)
            {
                ChangeState(ENEMY_STATE.Pain);
                return;
            }
        }
    
        if(currentState == ENEMY_STATE.Idle || currentState == ENEMY_STATE.Patrol)
        {
            if(enemyType == ENEMY_TYPE.Civilian)
                ChangeState(ENEMY_STATE.Cover);

            else
                ChangeState(ENEMY_STATE.Chase);

            if (enemyData.GetNoticeVox() != null)
            {
                AudioClip[] clips = enemyData.GetNoticeVox();
                aSource.PlayOneShot(clips[UnityEngine.Random.Range(0, clips.Length)]);
            }
        }
        
        OnEnemyDamage?.Invoke();
    }

    void FlingEnemy(Vector3 direction, int damage)
    {
        rb.isKinematic = false;
        rb.AddForce(direction * damage * 2f / rb.mass, ForceMode.Force);
        rb.AddForce(Vector3.up * damage * 2f);
    }

    public void FreezeEnemy(bool bFaceTarget = false)
    {
        if(bFaceTarget)
        {
            Vector3 LookAtDir = new Vector3(target.position.x - transform.position.x, 0, target.position.z - transform.position.z);
            transform.rotation = Quaternion.LookRotation(LookAtDir.normalized, Vector3.up);
            spriteAnimator.BUpdateRotation = false;
        }

        ChangeState(ENEMY_STATE.Frozen);
    }

    #region Saving & Loading
    void SaveEnemy()
    {
        SaveManager.SaveData.EnemyData enemyData = new SaveManager.SaveData.EnemyData();
        enemyData.transformData.position = transform.position;
        enemyData.transformData.rotation = transform.rotation.eulerAngles;
        enemyData.transformData.scale = transform.localScale;

        enemyData.objectName = enemyID;

        enemyData.bSeenPlayer = bSeenPlayer;

        if(target != null)
            enemyData.targetName = target.gameObject.name;  // TODO: Better way for this, since this might set an incorrect target

        enemyData.health = enemyHealth;
        enemyData.enemyState = currentState;

        SaveManager.Instance.gameState.enemyList.Add(enemyData);

        // TODO: Add health, targetting and other savables here
    }

    void LoadEnemy()
    {
        SaveManager.SaveData.EnemyData enemyData = null;
        for(int i = 0; i < SaveManager.Instance.gameState.enemyList.Count; i++)
        {
            if(SaveManager.Instance.gameState.enemyList[i].objectName == enemyID)
            {
                // Save Object Found
                //Debug.Log("Found saved enemy");
                enemyData = SaveManager.Instance.gameState.enemyList[i];
            }
        }

        if(enemyData != null)
        {
            InitializeStats();

            transform.position = enemyData.transformData.position;
            transform.rotation = Quaternion.Euler(enemyData.transformData.rotation);
            transform.localScale = enemyData.transformData.scale;
            bSeenPlayer = enemyData.bSeenPlayer;
            enemyHealth = enemyData.health;

            GameObject targetObj = GameObject.Find(enemyData.targetName);
            if (targetObj != null)
                target = targetObj.transform;

            ChangeState(enemyData.enemyState);
        }

        else
        {
            Destroy(gameObject);
        }
    }
    #endregion
}

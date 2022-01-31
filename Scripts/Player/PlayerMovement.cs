//Original script from Dani 
//https://github.com/DaniDevy/FPS_Movement_Rigidbody

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class PlayerMovement : MonoBehaviour
{
    //Assingables
    public Transform playerCam;
    public Transform orientation;
    public Transform gunPosition;

    //Other
    private Rigidbody rb;

    //Rotation and look
    private float xRotation;
    private float sensitivity = 50f;
    private float sensMultiplier = 1f;

    //Movement
    public float moveSpeed = 4500;
    public float maxSpeed = 20;
    public bool grounded;
    public LayerMask whatIsGround;

    public float counterMovement = 0.2f;
    private float threshold = 0.01f;
    public float maxSlopeAngle = 35f;
    private bool isSprinting = false;
    private Vector3 handOriginalPosition;
    private Vector3 handCrouchPosition;
    private Vector3 handOriginalScale;
    private Vector3 handRefVel = Vector3.zero;

    //Crouch & Slide
    private Vector3 crouchScale = new Vector3(1, 0.4f, 1);
    private Vector3 playerScale;
    public float slideForce = 400;
    public float slideCounterMovement = 0.2f;

    //Jumping
    private bool readyToJump = true;
    private bool readyToMantle = false;
    private float jumpCooldown = 0.25f;
    public float jumpForce = 760f;
    public float mantleHeight = 2f;

    //Input
    float x, y;
    bool jumping, sprinting, crouching;

    //Sliding
    private Vector3 normalVector = Vector3.up;
    private Vector3 wallNormalVector;

    // Dashing
    [SerializeField] float dashDuration = 1f;
    [SerializeField] private float dashSpeed = 10f;
    [SerializeField] private float dashCoolDown = 0.5f;

    private bool dashEnabled = false;
    private bool canDash = true;
    private Vector3 dashPosition = Vector3.zero;
    private bool dashing = false;
    private float dashCurrent = 0f;

    //Custom
    AudioSource aSource = null;
    public float handOffset = 0.05f;
    public float interpolationRatio = 0.5f;
    private float handBaseMovement = 0.5f;
    private Vector3 frameVelocity = Vector3.zero;
    PlayerContainer playerContainer = null;
    private bool noClip = false;
    [SerializeField] private float maxFallDistance;

    private bool bInWater = false;
    private bool bOnRamp = false;
    
    public bool InWater { get { return bInWater; } set { bInWater = value; EnterWater(); } }

    [SerializeField] private AudioClip[] vox_death = null;
    [SerializeField] private AudioClip pickupSnd = null;
    [SerializeField] private AudioClip jumpSnd = null;

    //PlayerHealth health;
    private bool allowMovement = true;
    public bool AllowMovement { get { return allowMovement; } set { allowMovement = value; } }
    
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        aSource = GetComponentInParent<AudioSource>();
    }

    void Start()
    {
        handOriginalPosition = gunPosition.localPosition;
        handOriginalScale = gunPosition.localScale;

        playerContainer = GetComponentInParent<PlayerContainer>();
        playerContainer.OnPlayerSave += SavePlayer;
        playerContainer.OnPlayerLoad += LoadPlayer;
        playerContainer.OnPlayerDeath += PlayerDeath;
        playerContainer.OnAbilityActivate += ActivateAbility;
        playerContainer.OnAbilityDeactivate += DeactivateAbility;

        OnPlayerGameStarted();
    }

    private void OnDestroy()
    {
        playerContainer.OnPlayerSave -= SavePlayer;
        playerContainer.OnPlayerLoad -= LoadPlayer;
        playerContainer.OnAbilityActivate -= ActivateAbility;
        playerContainer.OnAbilityDeactivate -= DeactivateAbility;
        UIManager.Instance.OnSensitivityChanged -= ChangeSensMultiplier;
    }

    void OnPlayerGameStarted()
    {
        allowMovement = true;
        playerScale = transform.localScale;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        if(UIManager.Instance != null)
        {
            UIManager.Instance.OnSensitivityChanged += ChangeSensMultiplier;
        }

        if(PlayerPrefs.HasKey("HS_FOV"))
        {
            Camera.main.fieldOfView = PlayerPrefs.GetFloat("HS_FOV");
        }

        ChangeSensMultiplier();
    }

    private void FixedUpdate()
    {
        Movement();
    }

    private void Update()
    {
        if (UIManager.Instance.BGamePaused)
            return;

        if (DebugController.Instance.ConsoleOpen)
            return;

        MyInput();
        Look();
    }

    /// <summary>
    /// Find user input. Should put this in its own class but im lazy
    /// </summary>
    private void MyInput()
    {
        if (!playerContainer.PlayerHealth.IsAlive)
            return;

        x = Input.GetAxisRaw("Horizontal");
        y = Input.GetAxisRaw("Vertical");
        jumping = Input.GetButton("Jump");
        crouching = Input.GetButton("Crouch");
        isSprinting = Input.GetKey(KeyCode.LeftShift);

        // Crouching TODO: Add to custom input system, add toggle crouch option
        if (Input.GetButtonDown("Crouch"))
            StartCrouch();
        if (Input.GetButtonUp("Crouch"))
            StopCrouch();

        if (!crouching && transform.localScale == crouchScale)
            StopCrouch();
    }

    private void StartCrouch()
    {
        if (transform.localScale == crouchScale)
            return;

        if (!bInWater)
        {
            transform.localScale = crouchScale;
            transform.position = new Vector3(transform.position.x, transform.position.y - crouchScale.y, transform.position.z);
        }
    }

    private void StopCrouch()
    {
        if (bInWater)
            return;

        // Are we touching a wall
        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.up, out hit, 1f, whatIsGround))
            return;

        transform.localScale = playerScale;
        transform.position = new Vector3(transform.position.x, transform.position.y + crouchScale.y, transform.position.z);
    }

    private void Movement()
    {
        if (!allowMovement)
            return;

        if(dashing)
        {
            if(dashPosition != Vector3.zero)
            {
                Vector3 newPos = rb.position + dashPosition * Time.fixedDeltaTime * dashSpeed;
                Vector3 dir = (newPos - rb.position).normalized;
                if(Physics.Raycast(rb.position, dir, 1f, whatIsGround))
                {
                    StopDash();
                    return;
                }

                rb.MovePosition(rb.position + dashPosition * Time.fixedDeltaTime * dashSpeed);
                dashCurrent += Time.fixedDeltaTime;
                if(dashCurrent >= dashDuration)
                {
                    StopDash();
                }

                return;
            }  
        }

        if (bInWater)
        {
            if (crouching)
                rb.velocity = rb.velocity + (Vector3.down * Time.fixedDeltaTime * 10f); //rb.AddForce(Vector3.down * Time.fixedDeltaTime2f);
        }

        // Only add extra down force, if we're not in water
        else if(!noClip && rb.useGravity)
        {
            rb.AddForce(Vector3.down * Time.fixedDeltaTime * 300);
        }

        if(noClip && crouching)
        {
            rb.AddForce(-Vector2.up * Time.fixedDeltaTime * jumpForce);
        }
            
        // Find actual velocity relative to where player is looking
        Vector2 mag = FindVelRelativeToLook();
        float xMag = mag.x, yMag = mag.y;

        // Counteract sliding and sloppy movement
        CounterMovement(x, y, mag);

        if (canDash && jumping)    //if (!grounded && canDash && jumping)
            Dash();

        // If holding jump && ready to jump, then jump
        if (readyToJump && jumping)
            Jump();

        // Set max speed
        float maxSpeed = this.maxSpeed;

        // If speed is larger than maxspeed, cancel out the input so you don't go over max speed
        if (x > 0 && xMag > maxSpeed) x = 0;
        if (x < 0 && xMag < -maxSpeed) x = 0;
        if (y > 0 && yMag > maxSpeed) y = 0;
        if (y < 0 && yMag < -maxSpeed) y = 0;

        // Some multipliers
        float multiplier = .9f, multiplierV = .9f;

        // Sprinting = faster
        if (isSprinting)
        {
            multiplier = 3.5f;
            multiplierV = 3.5f;
        }

        // Movement in air
        if (!grounded)
        {
            if(isSprinting)
            {
                multiplier = 0.8f;
                multiplierV = 0.8f;
            }

            else
            {
                multiplier = 0.5f;
                multiplierV = 0.5f;
            }        
        }

        if(bInWater)
        {
            if(isSprinting)
            {
                multiplier = 0.3f;
                multiplierV = 0.3f;
            }

            else
            {
                multiplier = 0.2f;
                multiplierV = 0.2f;
            }
        }

        // Movement while sliding
        if (grounded && crouching) multiplierV = 0.8f;

        // Apply forces to move player
        rb.AddForce(orientation.transform.forward * y * moveSpeed * Time.fixedDeltaTime * multiplier * multiplierV);
        rb.AddForce(orientation.transform.right * x * moveSpeed * Time.fixedDeltaTime * multiplier);

        frameVelocity = rb.velocity;

        float fallSpeed = rb.velocity.y * -0.01f;

        if (fallSpeed > 0.1f)
            fallSpeed = 0.1f;

        else if (fallSpeed < -0.4f)
            fallSpeed = -0.4f;

        else
        {
            if(rb.velocity.magnitude > 0.5f)
            {
                fallSpeed = Mathf.Abs(Mathf.Sin(handBaseMovement += Time.deltaTime)) * -0.1f;
            }     
        }
                 
        Vector3 desiredPos = handOriginalPosition + new Vector3(Mathf.Sin((handBaseMovement += Time.deltaTime) * 3f) * Mathf.Abs(mag.magnitude) * 0.01f, fallSpeed, 0);
        handBaseMovement -= Time.deltaTime;
        gunPosition.localPosition = Vector3.SmoothDamp(gunPosition.localPosition, desiredPos, ref handRefVel, 0.08f);
        //gunPosition.localPosition = Vector3.Lerp(gunPosition.localPosition, desiredPos, Time.fixedDeltaTime * rb.velocity.magnitude);
    }

    private void Jump()
    {
        float nJumpForce = jumpForce;
        if (dashing)
            nJumpForce /= 2;

        if (noClip)
        {
            rb.AddForce(Vector2.up * Time.fixedDeltaTime * nJumpForce);
            return;
        }

        if (bInWater)
        {
            rb.velocity = rb.velocity + (Vector3.up * Time.fixedDeltaTime * 10f);
            //Debug.Log("Jumping in water, rb velocity: " + rb.velocity + " Using gravity: " +rb.useGravity);
            return;
        }

        // No jumping allowed while crouched (unless in water)
        if (crouching)
            return;

        if (grounded && readyToJump)
        {
            readyToJump = false;
            readyToMantle = true;

            //Add jump forces
            rb.AddForce(Vector2.up * nJumpForce * 1.5f);
            rb.AddForce(normalVector * nJumpForce * 0.5f);

            //If jumping while falling, reset y velocity.
            Vector3 vel = rb.velocity;
            if (rb.velocity.y < 0.5f)
                rb.velocity = new Vector3(vel.x, 0, vel.z);
            else if (rb.velocity.y > 0)
                rb.velocity = new Vector3(vel.x, vel.y / 2, vel.z);

            aSource.PlayOneShot(jumpSnd);

            Invoke(nameof(ResetJump), jumpCooldown);
            Invoke(nameof(ResetMantle), jumpCooldown * 4);
        }
    }

    private void Dash()
    {
        if (!dashEnabled)
            return;

        if(isSprinting)
        {
            Vector3 direction = (orientation.transform.forward * y * dashSpeed + orientation.transform.right * x * dashSpeed).normalized;
            //Debug.Log("StartPos: " + transform.position + " Direction: " + direction);

            dashPosition = direction;

            canDash = false;
            dashing = true;
        }

        if (jumping && !isSprinting && grounded)
        {
            //Debug.Log("Dash Up");
            rb.AddForce(Vector2.up * jumpForce);
            //rb.AddForce(normalVector * jumpForce * 0.5f);
            canDash = false;
        }

        Invoke(nameof(ResetDash), dashCoolDown);
    }

    void StopDash()
    {
        dashPosition = Vector3.zero;
        dashing = false;
        dashCurrent = 0f;
    }

    private void ResetJump()
    {
        readyToJump = true;
    }

    private void ResetDash()
    {
        canDash = true;
    }

    private void ResetMantle()
    {
        readyToMantle = false;
    }

    private float desiredX;
    private void Look()
    {
        //if (UiManager.Instance.GamePaused)
        //    return;

        if (!allowMovement)
            return;

        float mouseX = Input.GetAxis("Mouse X") * sensitivity * Time.fixedDeltaTime * sensMultiplier;
        float mouseY = Input.GetAxis("Mouse Y") * sensitivity * Time.fixedDeltaTime * sensMultiplier;

        //Find current look rotation
        Vector3 rot = playerCam.transform.localRotation.eulerAngles;
        desiredX = rot.y + mouseX;

        //Rotate, and also make sure we dont over- or under-rotate.
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -50f, 50f);  // -90f, 90f

        //Perform the rotations
        playerCam.transform.localRotation = Quaternion.Euler(xRotation, desiredX, 0);
        orientation.transform.localRotation = Quaternion.Euler(0, desiredX, 0);
    }

    private void CounterMovement(float x, float y, Vector2 mag)
    {
        if (!grounded || jumping) return;

        // Counter movement
        if (Mathf.Abs(mag.x) > threshold && Mathf.Abs(x) < 0.05f || (mag.x < -threshold && x > 0) || (mag.x > threshold && x < 0))
        {
            rb.AddForce(moveSpeed * orientation.transform.right * Time.deltaTime * -mag.x * counterMovement);
        }
        if (Mathf.Abs(mag.y) > threshold && Mathf.Abs(y) < 0.05f || (mag.y < -threshold && y > 0) || (mag.y > threshold && y < 0))
        {
            rb.AddForce(moveSpeed * orientation.transform.forward * Time.deltaTime * -mag.y * counterMovement);
        }

        // Limit diagonal running. This will also cause a full stop if sliding fast and un-crouching, so not optimal.
        if (Mathf.Sqrt((Mathf.Pow(rb.velocity.x, 2) + Mathf.Pow(rb.velocity.z, 2))) > maxSpeed)
        {
            float fallspeed = rb.velocity.y;
            Vector3 n = rb.velocity.normalized * maxSpeed;
            rb.velocity = new Vector3(n.x, fallspeed, n.z);
        }
    }

    /// <summary>
    /// Find the velocity relative to where the player is looking
    /// Useful for vectors calculations regarding movement and limiting movement
    /// </summary>
    /// <returns></returns>
    public Vector2 FindVelRelativeToLook()
    {
        float lookAngle = orientation.transform.eulerAngles.y;
        float moveAngle = Mathf.Atan2(rb.velocity.x, rb.velocity.z) * Mathf.Rad2Deg;

        float u = Mathf.DeltaAngle(lookAngle, moveAngle);
        float v = 90 - u;

        float magnitue = rb.velocity.magnitude;
        float yMag = magnitue * Mathf.Cos(u * Mathf.Deg2Rad);
        float xMag = magnitue * Mathf.Cos(v * Mathf.Deg2Rad);

        return new Vector2(xMag, yMag);
    }

    private bool IsFloor(Vector3 v)
    {
        float angle = Vector3.Angle(Vector3.up, v);
        return angle < maxSlopeAngle;
    }

    private bool IsRamp(Vector3 v)
    {
        float angle = Vector3.Angle(Vector3.up, v);
        return angle > 10;  // TODO: Make this a variable
    }

    private bool IsWall(Collision collision)
    {
        for(int i = 0; i < collision.contactCount; i++)
        {
            Vector3 normal = collision.GetContact(i).normal;
            float angle = Vector3.Angle(Vector3.up, normal);
            if (angle > maxSlopeAngle)
                return true;
        }

        return false;
    }

    private bool cancellingGrounded;
    private void OnCollisionEnter(Collision collision)
    {
        // Auto mantle check here, frame velocity used so that we don't get the velocity where wall has been hit, which is always (0,0,0)
        Vector3 savedVelocity = new Vector3(frameVelocity.x, frameVelocity.y, frameVelocity.z);
        bool fallDamageTaken = false;

        if (collision.gameObject.CompareTag("Enemy"))
        {
            if(!grounded)
            {
                //Add jump forces
                rb.AddForce(Vector2.up * jumpForce);
                rb.AddForce(normalVector * jumpForce * 0.5f);
                rb.AddForce(orientation.transform.forward * y * moveSpeed * Time.fixedDeltaTime * 2f);
                rb.AddForce(orientation.transform.right * x * moveSpeed * Time.fixedDeltaTime * 2f);
            }
        }

        for(int i = 0; i < collision.contactCount; i++)
        {
            if (IsFloor(collision.GetContact(i).normal))
            {
                if (-savedVelocity.y > maxFallDistance && !fallDamageTaken && !dashEnabled)
                {
                    playerContainer.TakeDamage(Mathf.FloorToInt(-savedVelocity.y * 0.5f), null);
                    fallDamageTaken = true;
                }

                // Stop movement, if no input is pressed
                if (Input.GetAxisRaw("Horizontal") == 0 && Input.GetAxisRaw("Vertical") == 0)
                {
                    rb.velocity = new Vector3(rb.velocity.x / 20f, 0, rb.velocity.z / 20f);     //Vector3.zero;
                    break;
                } 
            }
        }
            
        if (grounded || crouching || !readyToMantle)
            return;

        if (!IsWall(collision))
        {
            if (dashing)
                StopDash();

            return;
        }

        // Are we touching a wall
        RaycastHit hit;
        if (Physics.Raycast(transform.position, orientation.forward, out hit, 1f, whatIsGround))
        {
            if (hit.collider.gameObject.layer != LayerMask.NameToLayer("Actor"))
                return;

            Vector3 mantlePos = transform.position;
            float iterateAmount = mantleHeight * 0.1f;

            // Iterate through heights to see if player can mantle on the object
            for (float iter = 0f; iter < mantleHeight; iter += iterateAmount)
            {
                Vector3 addVector = new Vector3(0, iter, 0);
                Vector3 checkPos = transform.position + addVector;
                RaycastHit newHit;

                // Found a platform to mantle on
                if (!Physics.Raycast(checkPos, orientation.forward, out newHit, 1f, whatIsGround))
                {
                    //Debug.Log("Auto mantle did not hit wall");
                    mantlePos = hit.point + addVector;
                    break;
                }
            }

            if (mantlePos == transform.position)
                return;

            // Do mantle, if ceiling is high enough
            if (!Physics.Raycast(mantlePos, transform.up, 2f, whatIsGround))
            {
                //Debug.Log("Auto Mantle");
                rb.position = mantlePos + new Vector3(0, 1, 0);
                Vector3 newVecolity = savedVelocity * 0.5f;
                //newVecolity.y *= 0.5f;
                rb.velocity = newVecolity;
            }
        }
    }

    /// <summary>
    /// Handle ground detection
    /// </summary>
    private void OnCollisionStay(Collision other)
    {
        //Make sure we are only checking for walkable layers
        int layer = other.gameObject.layer;
        if (whatIsGround != (whatIsGround | (1 << layer))) return;

        if (bInWater)
            return;

        // Iterate through every collision in a physics update
        for (int i = 0; i < other.contactCount; i++)
        {
            Vector3 normal = other.GetContact(i).normal;
            //FLOOR
            if (IsFloor(normal))
            {
                grounded = true;
                canDash = true;
                cancellingGrounded = false;
                normalVector = normal;
                CancelInvoke(nameof(StopGrounded));

                if (IsRamp(normal))
                {
                    //Debug.Log("On Ramp");
                    bOnRamp = true;

                    // Half velocities when entering a ramp (stop sliding down ramps)
                    if (Vector3.Angle(Vector3.up, normal) > 30)
                    {
                        if (rb.velocity.y < 0)
                            rb.velocity = new Vector3(rb.velocity.x / 2f, 0, rb.velocity.z / 2f); 
                    }

                    rb.useGravity = false;
                }

                else
                {
                    bOnRamp = false;
                    rb.useGravity = true;
                }
            }
        }

        //Invoke ground/wall cancel, since we can't check normals with CollisionExit
        float delay = 3f;
        if (!cancellingGrounded)
        {
            cancellingGrounded = true;
            Invoke(nameof(StopGrounded), Time.deltaTime * delay);
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if(!bInWater)
            rb.useGravity = true;

        if (bOnRamp)
        {
            rb.AddForce(Vector3.down * 10f);
            rb.AddForce(orientation.transform.forward * 10f);
        }
    }

    private void StopGrounded()
    {
        grounded = false;
    }

    void EnterWater()
    {
        rb.useGravity = !bInWater;
        if (bInWater)
        {
            StartCoroutine(DecreaseGravity());
        }

        else
        {
            StopCoroutine(DecreaseGravity());
            rb.AddForce(Vector2.up * jumpForce);
            rb.AddForce(normalVector * jumpForce * 0.5f);
        }
    }

    IEnumerator DecreaseGravity()
    {
        float yVel = rb.velocity.y;
        yVel = -1;
        rb.velocity = new Vector3(rb.velocity.x / 10, yVel, rb.velocity.z / 10);
        while (Mathf.Abs(yVel) > 0.5f)
        {
            if (!bInWater)
                yield break;

            yVel += Time.fixedDeltaTime;
            //Debug.Log("Y Velocity: " + yVel);
            rb.velocity = new Vector3(rb.velocity.x, yVel, rb.velocity.z);
            yield return new WaitForFixedUpdate();
        }
    }

    void ActivateAbility(EnumContainer.PLAYERABILITY ability)
    {
        if(ability == EnumContainer.PLAYERABILITY.SPEED)
        {
            dashEnabled = true;
        }

        else if(ability == EnumContainer.PLAYERABILITY.TIME)
        {
            moveSpeed *= 2;
        }
    }

    void DeactivateAbility(EnumContainer.PLAYERABILITY ability)
    {
        if (ability == EnumContainer.PLAYERABILITY.SPEED)
        {
            dashEnabled = false;
        }

        else if (ability == EnumContainer.PLAYERABILITY.TIME)
        {
            moveSpeed /= 2;
        }
    }

    void SavePlayer()
    {
        SaveManager.Instance.gameState.player.transFormData.position = transform.position;
        SaveManager.Instance.gameState.player.transFormData.rotation = playerCam.rotation.eulerAngles;
        SaveManager.Instance.gameState.player.transFormData.scale = transform.localScale;
    }

    void LoadPlayer()
    {
        transform.position = SaveManager.Instance.gameState.player.transFormData.position;
        playerCam.rotation = Quaternion.Euler(SaveManager.Instance.gameState.player.transFormData.rotation);
        orientation.rotation = Quaternion.Euler(0, SaveManager.Instance.gameState.player.transFormData.rotation.y, 0);
        transform.localScale = SaveManager.Instance.gameState.player.transFormData.scale;
    }

    void PlayerDeath(EnumContainer.DamageInflictor damageInflictor)
    {
        allowMovement = false;

        //Player falls over
        rb.constraints = RigidbodyConstraints.None;
        rb.AddExplosionForce(200f, transform.position - new Vector3(2, 0, 0), 20f);

        if(damageInflictor.attacker != null)
        {
            Vector3 vecToAttacker = damageInflictor.attacker.position - transform.position;
            playerCam.transform.localRotation = Quaternion.LookRotation(vecToAttacker, Vector3.up);
            orientation.transform.localRotation = Quaternion.LookRotation(vecToAttacker, Vector3.up);
        }
    }

    public void NoClipEnabled()
    {
        noClip = !noClip;

        rb.useGravity = !noClip;
        if (TryGetComponent<CapsuleCollider>(out CapsuleCollider col))
        {
            col.enabled = !noClip;
        }

        rb.velocity = Vector3.zero;
    }

    public void OnSensitivityChanged( float amount )
    {
        sensitivity = amount;
    }

    public void SetPlayerRotation(float rotationX, float rotationY)
    {
        playerCam.transform.localRotation = Quaternion.Euler(rotationX, rotationY, 0);
        orientation.transform.localRotation = Quaternion.Euler(0, rotationY, 0);
    }

    void ChangeSensMultiplier()
    {
        if (PlayerPrefs.HasKey("HS_MouseSpeed"))
        {
            float val = PlayerPrefs.GetFloat("HS_MouseSpeed");
            if(val > 0)
                sensMultiplier = PlayerPrefs.GetFloat("HS_MouseSpeed");
        }
    }

    /*public void OnPlayerDeath()
    {
        aSource.PlayOneShot(vox_death[UnityEngine.Random.Range(0, vox_death.Length)]);
        health.OnPlayerDeath -= OnPlayerDeath;
        allowMovement = false;
        rb.constraints = RigidbodyConstraints.None;

        //Player falls over
        rb.AddExplosionForce(200f, transform.position - new Vector3(2, 0, 0), 20f);

    }*/
}

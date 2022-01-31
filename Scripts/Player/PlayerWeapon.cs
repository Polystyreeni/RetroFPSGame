using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerWeapon : MonoBehaviour
{
    [Header("Ammo & Damage")]
    public int stockSize;
    public int clipSize;
    public int stockDefault;
    public int clipDefault;
    public int ammoCost;
    public int maxDamage;
    public int minDamage;

    [Header("Stats")]
    public float fireTime = 0f;
    public float reloadTime = 0f;
    public float range = 1f;

    [Header("Weapon Types")]
    public int inventorySlot = 1;
    public bool fullAuto = false;
    public bool rechamber = false;
    public bool clipOnly = false;   // No reloading
    public bool allowEmpty = true;  // Can you hold this weapon if it's empty?
    public bool autoReload = false;

    [Header("Sounds")]
    public AudioClip sndWeaponFire = null;
    public AudioClip sndWeaponReload = null;

    // Support for throwables
    public bool throwWeapon = false;
    public float throwForce { get; private set; }
    private bool isCooking = false;
    private bool secondaryActive = false;
    private bool canReload = true;

    [HideInInspector] public bool bCanShoot = true;
    public bool CanSwitchWeapon { get; set; }

    public WeaponAnimator weaponAnimator = null;

    // TODO: Hide in inspector
    public int fireCounter = 0;
    public int ammoClip = 0;
    public int ammoStock = 0;

    private bool bWeaponReloading = false;

    public Transform Player { get; private set; }

    public AudioSource aSource = null;

    protected void Start()
    {
        weaponAnimator = GetComponent<WeaponAnimator>();
        aSource = GetComponent<AudioSource>();
        CanSwitchWeapon = true;
        AssingPlayer();
    }

    public void AssingPlayer()
    {
        GameObject parentObject = GetComponentInParent<PlayerContainer>().gameObject;
        Player = parentObject.GetComponentInChildren<PlayerHealth>().transform;
    }

    public virtual void SwitchToWeapon(bool bInstant = false)
    {
        if(bInstant)
        {
            weaponAnimator.StartAnimation(0);
            UpdateAmmoHud();
        }

        else
        {
            bCanShoot = false;
            int animIndex = 3;
            weaponAnimator.StartAnimation(animIndex);

            float animLenght = weaponAnimator.GetAnimLenght(animIndex);
            Invoke(nameof(EnableShooting), animLenght);
        } 
    }

    public virtual void SwitchFromWeapon()
    {
        bCanShoot = false;
        int animIndex = 4;
        weaponAnimator.StartAnimation(animIndex);
    }
    
    // Primary fire for weapon
    public virtual void FireWeapon()
    {
        if (ammoClip <= 0)
            return;

        fireCounter += ammoCost;
        ammoClip -= ammoCost;

        weaponAnimator.StartAnimation(1);
        bCanShoot = false;
        CanSwitchWeapon = false;

        //Debug.Log("Can Switch: " + CanSwitchWeapon);

        UpdateAmmoHud();

        if(throwWeapon)
        {
            throwForce = 0;
            secondaryActive = false;
            UIManager.Instance.ShowThrowBar(false);
        }

        if (aSource != null)
            aSource.PlayOneShot(sndWeaponFire);

        Invoke(nameof(EnableShooting), fireTime);
        Invoke(nameof(EnableWeaponSwitching), weaponAnimator.GetAnimLenght(1));
    }

    // Secondary fire for weapon specific secondary fire weapon functionalities
    public virtual void FireSecondary()
    {
        if (ammoClip <= 0)
            return;

        fireCounter += ammoCost;
        ammoClip -= ammoCost;

        weaponAnimator.StartAnimation(2);
        bCanShoot = false;
        CanSwitchWeapon = false;

        UpdateAmmoHud();

        if (throwWeapon)
        {
            throwForce = 0;
            secondaryActive = false;
            UIManager.Instance.ShowThrowBar(false);
        }

        // TODO: Add special secondary fire sound
        if(aSource != null)
            aSource.PlayOneShot(sndWeaponFire);

        Invoke(nameof(EnableShooting), fireTime);
        Invoke(nameof(EnableWeaponSwitching), weaponAnimator.GetAnimLenght(2));
    }

    public virtual void Reload()
    {
        // Clip only weapons can't be reloaded!
        if (clipOnly)
            return;

        if (ammoStock <= 0)
            return;

        if (!canReload)
            return;

        canReload = false;

        bCanShoot = false;
        CanSwitchWeapon = false;
        bWeaponReloading = true;

        // Set new ammo  
        if (ammoStock < clipSize + ammoClip)
        {
            ammoClip += ammoStock;
            if (ammoClip > clipSize)
                ammoClip = clipSize;
        }
           
        else
            ammoClip = clipSize;

        ammoStock -= fireCounter;
        if (ammoStock < 0)
            ammoStock = 0;

        fireCounter = 0;

        // Play the reload animation
        int animIndex = 5;
        weaponAnimator.StartAnimation(animIndex);

        aSource.PlayOneShot(sndWeaponReload);

        float reloadTime = weaponAnimator.GetAnimLenght(animIndex);
        Invoke(nameof(EnableShooting), reloadTime);
        Invoke(nameof(EnableWeaponSwitching), reloadTime);
        Invoke(nameof(EnableReloading), reloadTime);
    }

    // TODO: Add implemetation when / if needed
    public virtual void Rechamber()
    {

    }

    protected void Update()
    {
        if (UIManager.Instance.BGamePaused)
            return;

        if (DebugController.Instance.ConsoleOpen)
            return;

        // Override other functionality, if we're dealing with throwables
        if (throwWeapon)
        {
            FireWeaponThrow();
            return;
        }

        bool isFiring = false;
        bool isFiringAlt = false;
        bool isReloading = false;

        if(autoReload)  // TODO: Add game option for always auto-reload
        {
            if (ammoClip <= 0)
            {
                isReloading = true;
                CancelInvoke(nameof(EnableShooting));
                Invoke(nameof(Reload), fireTime / 2);
                return;
            }     
        }

        if(Input.GetKey(KeyCode.R)) // TODO: Change to new input system
        {
            if (fireCounter > 0 && !IsFiringAny() && bCanShoot)
                isReloading = true;
        }

        if(isReloading && !bWeaponReloading)
        {
            CancelInvoke(nameof(EnableShooting));
            Reload();
            return;
        }

        isFiring = IsFiringPrimary();
        isFiringAlt = IsFiringSecondary();

        if(bCanShoot && ammoClip > 0)
        {
            if (isFiringAlt)
                FireSecondary();

            else if (isFiring)
                FireWeapon();
        }

        if(bCanShoot && ammoClip <= 0 && ammoStock > 0)
        {
            if (IsFiringAny())
            {
                CancelInvoke(nameof(EnableShooting));
                Reload();
            }
        }
    }

    public virtual void FireWeaponThrow()
    {
        if (ammoClip <= 0)
            return;

        if (!bCanShoot)
            return;

        bool isFiring = Input.GetButton("Fire1");
        bool isFiringSecondary = Input.GetButton("Fire2");

        if(isFiring)
        {
            if(throwForce <= 0f)
            {
                if(!secondaryActive)
                    weaponAnimator.StartAnimation(6);

                aSource.PlayOneShot(sndWeaponReload);

                UIManager.Instance.ShowThrowBar(true);
            }
                
            if (throwForce < 2)
            {
                throwForce += Time.deltaTime;
                UIManager.Instance.ThrowBarSetValue(throwForce);
            }          
        }

        else if (isFiringSecondary && !isFiring && !secondaryActive)
        {
            weaponAnimator.StartAnimation(6);
            secondaryActive = true;
        }
            
        else
        {
            if (throwForce <= 0f)
                return;

            if (secondaryActive)
                FireSecondary();
            
            else
                FireWeapon();
        }
    }

    protected void EnableShooting()
    {
        bCanShoot = true;
        bWeaponReloading = false;
        if(gameObject.activeInHierarchy)
        {
            UpdateAmmoHud();
            if (ammoClip <= 0 && ammoStock <= 0 && !allowEmpty)
            {
                SwitchFromWeapon();
            }
        }   
    }

    protected void EnableWeaponSwitching()
    {
        CanSwitchWeapon = true;
    }

    protected void EnableReloading()
    {
        canReload = true;
    }

    protected void PlaySound(AudioClip soundClip)
    {
        if(aSource == null)
            aSource = GetComponent<AudioSource>();

        aSource.PlayOneShot(soundClip);
    }

    // Updates ammo hud
    protected void UpdateAmmoHud()
    {
        // TODO: Possible infinite ammo stuff here???
        string ammoText;
        if (clipOnly)
            ammoText = ammoClip.ToString();

        else
            ammoText = ammoClip.ToString() + " | " + ammoStock.ToString();

        UIManager.Instance.UpdateAmmo(ammoText);
    }
    
    public bool IsFiringPrimary()
    {
        if (fullAuto)
            return Input.GetButton("Fire1");
        else
            return Input.GetButtonDown("Fire1");
    }
    public bool IsFiringSecondary()
    {
        if (fullAuto)
            return Input.GetButton("Fire2");
        else
            return Input.GetButtonDown("Fire2");
    }

    public bool IsFiringAny()
    {
        if (throwWeapon)
        {
            if (isCooking || secondaryActive)
                return true;

            else
                return throwForce > 0f;
        }

        if (IsFiringPrimary() || IsFiringSecondary())
            return true;

        return false;    
    }

    public void AddAmmo(int amount)
    {
        if(clipOnly)
        {
            int newAmmo = ammoClip + amount;
            if (newAmmo >= clipSize)
                newAmmo = clipSize;

            ammoClip = newAmmo;
        }

        else
        {
            int newAmmo = ammoStock + amount;
            if (newAmmo >= stockSize)
                newAmmo = stockSize;

            ammoStock = newAmmo;
        }

        if(this.gameObject.activeInHierarchy)
            UpdateAmmoHud();       
    }
}

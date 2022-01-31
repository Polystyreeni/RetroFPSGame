using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class WeaponAnimator : MonoBehaviour
{
    [Header("Animation Sets")]
    [SerializeField] private SpriteAnimation idleAnimation = new SpriteAnimation();
    [SerializeField] private SpriteAnimation primaryFireAnimation = null;
    [SerializeField] private SpriteAnimation altFireAnimation = null;
    [SerializeField] private SpriteAnimation raiseAnimation = null;
    [SerializeField] private SpriteAnimation lowerAnimation = null;
    [SerializeField] private SpriteAnimation reloadAnimation = null;
    [SerializeField] private SpriteAnimation rechamberAnimation = null;

    [Header("Animator information")]
    [SerializeField] private bool bLoopAnimation = false;

    private bool bAnimationPlaying = false;

    [SerializeField] private SpriteRenderer sr = null;

    // Used for unlit parts of animations (muzzle flash, bright lights in weapon etc)
    [SerializeField] private SpriteRenderer altRenderer = null;

    private Sprite defaultSprite = null;

    // Start is called before the first frame update
    void Awake()
    {
        if(sr == null)
            sr = GetComponent<SpriteRenderer>();

        defaultSprite = sr.sprite;
    }

    private void Start()
    {
        StartAnimation(0, false);
    }

    IEnumerator PlayAnimation( int animIndex )
    {
        bAnimationPlaying = true;
        SpriteAnimation animation = GetAnimationByIndex(animIndex);
        bLoopAnimation = animation.isLooping;
        if (animation.isLit)
        {
            altRenderer.gameObject.SetActive(true);
            altRenderer.GetComponent<WeaponAnimator>().StartAnimation(animIndex);
        }

        else
        {
            if(altRenderer != null)
                altRenderer.gameObject.SetActive(false);
        }

        do
        {
            foreach (Sprite s in animation.sprites)
            {
                if (sr == null)
                    continue;

                sr.sprite = s;
                yield return new WaitForSeconds(animation.animSpeed);
            }
        }

        while (bLoopAnimation);
        bAnimationPlaying = false;
        if(!animation.pauseLastFrame)
            StartAnimation(0);
    }

    void StopAnimation()
    {
        StopAllCoroutines();
    }

    SpriteAnimation GetAnimationByIndex( int animIndex )
    {
        switch(animIndex)
        {
            case 0:
                return idleAnimation;

            case 1:
                return primaryFireAnimation;

            case 2:
                return altFireAnimation;

            case 3:
                return raiseAnimation;

            case 4:
                return lowerAnimation;

            case 5:
                return reloadAnimation;

            case 6:
                return rechamberAnimation;

            default:
                return idleAnimation;
        }
    }

    // Public methods
    public void StartAnimation(int animationIndex, bool bIsLooping = false )
    {
        StopAnimation();
        StartCoroutine(PlayAnimation(animationIndex));
    }

    public float GetAnimLenght( int animIndex )
    {
        SpriteAnimation animation = GetAnimationByIndex(animIndex);
        return animation.sprites.Length * animation.animSpeed;
    }
}

[Serializable]
public class SpriteAnimation
{
    public Sprite[] sprites = null;
    public bool isLooping = false;
    public bool isLit = false;
    public bool pauseLastFrame = false;
    public float animSpeed = 0.05f;
}

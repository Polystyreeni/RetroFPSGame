using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioModifier : MonoBehaviour
{
    // TODO: Other effects
    [SerializeField] private bool randomizePitch = false;

    AudioSource aSource = null;

    private void Awake()
    {
        aSource = GetComponent<AudioSource>();

        if (randomizePitch)
            RandomizePitch();
    }

    void RandomizePitch()
    {
        aSource.pitch = Random.Range(0.8f, 1.2f);
    }
}

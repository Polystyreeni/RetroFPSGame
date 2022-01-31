using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Water : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Player"))
        {
            PlayerMovement mov = other.GetComponent<PlayerMovement>();
            if (mov != null)
                mov.InWater = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerMovement mov = other.GetComponent<PlayerMovement>();
            if (mov != null)
                mov.InWater = false;
        }
    }
}

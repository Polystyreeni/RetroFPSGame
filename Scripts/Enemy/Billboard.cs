using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Billboard : MonoBehaviour
{
    public Animator anim = null;
    public Transform enemy = null;
    private Transform target;
    private Camera playerCam = null;

    private bool bUpdateRotation = true;
    public bool BUpdateRotation { get { return bUpdateRotation; } set { bUpdateRotation = value; } }

    private void LateUpdate()
    {
        // Checking Camera.main every frame is expensive as hell, so only do that when necessary
        if(playerCam == null)
        {
            playerCam = FindPlayerCam();
            return;
        }

        if(target == null)
        {
            target = FindTarget();
            return;
        }

        Vector3 unitforwardvec = enemy.forward.normalized;
        Vector3 unitLeftVec = -enemy.right.normalized;
        Vector3 unitPlayerToEnemyVec = (target.position - enemy.position).normalized;

        // Checking needed directional sprite using left and front dot products
        float vectorDotFwd = Vector3.Dot(unitforwardvec, unitPlayerToEnemyVec);
        float vectorDotLeft = Vector3.Dot(unitLeftVec, unitPlayerToEnemyVec);

        // If allowed to rotate, check correct sprite
        if (bUpdateRotation)
            SetAnimationLayer(vectorDotFwd, vectorDotLeft);

        //Make sure object rotates with player camera
        Vector3 LookAtDir = new Vector3(playerCam.transform.position.x - transform.position.x, 0, playerCam.transform.position.z - transform.position.z);
        transform.rotation = Quaternion.LookRotation(-LookAtDir.normalized, Vector3.up);
    }

    private Transform FindTarget()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            return player.transform;

        return null;
    }

    private Camera FindPlayerCam()
    {
        if (Camera.main != null)
            return Camera.main;

        return null;
    }

    /// <summary>
    /// Set the correct animation layer according to player camera direction & enemy direction
    /// </summary>
    /// <param name="dotF"></param>
    /// <param name="dotL"></param>
    void SetAnimationLayer(float dotF, float dotL)
    {
        //Debug.Log("Dot L " + dotL + "Dot F: " + dotF);
        if (dotF < 0.775 && dotF > 0.55)
        {
            //Debug.Log("Front");
            if (dotL < 0.55 && dotL > -0.225)
            {
                anim.SetLayerWeight(anim.GetLayerIndex("Front"), 1);
                anim.SetLayerWeight(anim.GetLayerIndex("FrontL"), 0);
                anim.SetLayerWeight(anim.GetLayerIndex("FrontR"), 0);
                anim.SetLayerWeight(anim.GetLayerIndex("Left"), 0);
                anim.SetLayerWeight(anim.GetLayerIndex("Right"), 0);
                anim.SetLayerWeight(anim.GetLayerIndex("BackL"), 0);
                anim.SetLayerWeight(anim.GetLayerIndex("BackR"), 0);
                anim.SetLayerWeight(anim.GetLayerIndex("Back"), 0);
            }

            else if (dotL < -0.225)
            {
                anim.SetLayerWeight(anim.GetLayerIndex("Front"), 0);
                anim.SetLayerWeight(anim.GetLayerIndex("FrontL"), 0);
                anim.SetLayerWeight(anim.GetLayerIndex("FrontR"), 1);
                anim.SetLayerWeight(anim.GetLayerIndex("Left"), 0);
                anim.SetLayerWeight(anim.GetLayerIndex("Right"), 0);
                anim.SetLayerWeight(anim.GetLayerIndex("BackL"), 0);
                anim.SetLayerWeight(anim.GetLayerIndex("BackR"), 0);
                anim.SetLayerWeight(anim.GetLayerIndex("Back"), 0);
            }

            // Front Left
            else if(dotL > 0.55)
            {
                anim.SetLayerWeight(anim.GetLayerIndex("Front"), 0);
                anim.SetLayerWeight(anim.GetLayerIndex("FrontL"), 1);
                anim.SetLayerWeight(anim.GetLayerIndex("FrontR"), 0);
                anim.SetLayerWeight(anim.GetLayerIndex("Left"), 0);
                anim.SetLayerWeight(anim.GetLayerIndex("Right"), 0);
                anim.SetLayerWeight(anim.GetLayerIndex("BackL"), 0);
                anim.SetLayerWeight(anim.GetLayerIndex("BackR"), 0);
                anim.SetLayerWeight(anim.GetLayerIndex("Back"), 0);
            }   
            
        }

        // Side
        else if (dotF < 0.55 && dotF > -0.225)
        {
            //Debug.Log("Side");
            // Right
            if (dotL < -0.775)
            {
                anim.SetLayerWeight(anim.GetLayerIndex("Front"), 0);
                anim.SetLayerWeight(anim.GetLayerIndex("FrontL"), 0);
                anim.SetLayerWeight(anim.GetLayerIndex("FrontR"), 0);
                anim.SetLayerWeight(anim.GetLayerIndex("Left"), 0);
                anim.SetLayerWeight(anim.GetLayerIndex("Right"), 1);
                anim.SetLayerWeight(anim.GetLayerIndex("BackL"), 0);
                anim.SetLayerWeight(anim.GetLayerIndex("BackR"), 0);
                anim.SetLayerWeight(anim.GetLayerIndex("Back"), 0);
            }

            // Left
            else
            {
                anim.SetLayerWeight(anim.GetLayerIndex("Front"), 0);
                anim.SetLayerWeight(anim.GetLayerIndex("FrontL"), 0);
                anim.SetLayerWeight(anim.GetLayerIndex("FrontR"), 0);
                anim.SetLayerWeight(anim.GetLayerIndex("Left"), 1);
                anim.SetLayerWeight(anim.GetLayerIndex("Right"), 0);
                anim.SetLayerWeight(anim.GetLayerIndex("BackL"), 0);
                anim.SetLayerWeight(anim.GetLayerIndex("BackR"), 0);
                anim.SetLayerWeight(anim.GetLayerIndex("Back"), 0);
            }

        }

        else if (dotF < -0.225 && dotF > -0.55)
        {
           // Debug.Log("Back");
            // Back Right
            if (dotL > -0.775)
            {
                anim.SetLayerWeight(anim.GetLayerIndex("Front"), 0);
                anim.SetLayerWeight(anim.GetLayerIndex("FrontL"), 0);
                anim.SetLayerWeight(anim.GetLayerIndex("FrontR"), 0);
                anim.SetLayerWeight(anim.GetLayerIndex("Left"), 0);
                anim.SetLayerWeight(anim.GetLayerIndex("Right"), 0);
                anim.SetLayerWeight(anim.GetLayerIndex("BackL"), 1);
                anim.SetLayerWeight(anim.GetLayerIndex("BackR"), 0);
                anim.SetLayerWeight(anim.GetLayerIndex("Back"), 0);
            }

            // Back left
            else
            {
                anim.SetLayerWeight(anim.GetLayerIndex("Front"), 0);
                anim.SetLayerWeight(anim.GetLayerIndex("FrontL"), 0);
                anim.SetLayerWeight(anim.GetLayerIndex("FrontR"), 0);
                anim.SetLayerWeight(anim.GetLayerIndex("Left"), 0);
                anim.SetLayerWeight(anim.GetLayerIndex("Right"), 0);
                anim.SetLayerWeight(anim.GetLayerIndex("BackL"), 0);
                anim.SetLayerWeight(anim.GetLayerIndex("BackR"), 1);
                anim.SetLayerWeight(anim.GetLayerIndex("Back"), 0);
            }
        }

        // Back
        else if (dotF < -0.55 && dotF > -1)
        {
            anim.SetLayerWeight(anim.GetLayerIndex("Front"), 0);
            anim.SetLayerWeight(anim.GetLayerIndex("FrontL"), 0);
            anim.SetLayerWeight(anim.GetLayerIndex("FrontR"), 0);
            anim.SetLayerWeight(anim.GetLayerIndex("Left"), 0);
            anim.SetLayerWeight(anim.GetLayerIndex("Right"), 0);
            anim.SetLayerWeight(anim.GetLayerIndex("BackL"), 0);
            anim.SetLayerWeight(anim.GetLayerIndex("BackR"), 0);
            anim.SetLayerWeight(anim.GetLayerIndex("Back"), 1);
        }

        else
        {
            anim.SetLayerWeight(anim.GetLayerIndex("Front"), 1);
            anim.SetLayerWeight(anim.GetLayerIndex("FrontL"), 0);
            anim.SetLayerWeight(anim.GetLayerIndex("FrontR"), 0);
            anim.SetLayerWeight(anim.GetLayerIndex("Left"), 0);
            anim.SetLayerWeight(anim.GetLayerIndex("Right"), 0);
            anim.SetLayerWeight(anim.GetLayerIndex("BackL"), 0);
            anim.SetLayerWeight(anim.GetLayerIndex("BackR"), 0);
            anim.SetLayerWeight(anim.GetLayerIndex("Back"), 0);
        }
    }
}

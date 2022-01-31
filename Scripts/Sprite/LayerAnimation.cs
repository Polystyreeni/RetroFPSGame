using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LayerAnimation : MonoBehaviour
{
    public Animator anim = null;
    public Transform enemy = null;
    private Transform target;
    private Camera playerCam = null;

    private void LateUpdate()
    {
        // Checking Camera.main every frame is expensive as hell, so only do that when necessary
        if (playerCam == null)
        {
            playerCam = FindPlayerCam();
            return;
        }

        if (target == null)
        {
            target = FindTarget();
            return;
        }

        Vector3 unitforwardvec = enemy.forward;
        Vector3 unitLeftVec = -enemy.right;
        Vector3 unitPlayerToEnemyVec = (target.transform.position - enemy.position).normalized;

        // Checking needed directional sprite using left and front dot products
        float vectorDotFwd = Vector3.Dot(unitforwardvec, unitPlayerToEnemyVec);
        float vectorDotLeft = Vector3.Dot(unitLeftVec, unitPlayerToEnemyVec);

        //Rotate the enemy properly with animations
        SetActiveRotation(vectorDotFwd, vectorDotLeft);

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

    void SetActiveRotation( float vectorDotF, float vectorDotL)
    {
        if (vectorDotF < 0.775 && vectorDotF > 0.55)
        {
            Debug.Log("Front");
            anim.SetLayerWeight(anim.GetLayerIndex("Front"), 1);
            anim.SetLayerWeight(anim.GetLayerIndex("Left"), 0);
            anim.SetLayerWeight(anim.GetLayerIndex("Right"), 0);
            anim.SetLayerWeight(anim.GetLayerIndex("Back"), 0);

        }

        // Side
        if (vectorDotF < 0.55 && vectorDotF > -0.225)
        {
            Debug.Log("Side");
            // Right
            if (vectorDotL < -0.775)
            {
                anim.SetLayerWeight(anim.GetLayerIndex("Front"), 0);
                anim.SetLayerWeight(anim.GetLayerIndex("Left"), 0);
                anim.SetLayerWeight(anim.GetLayerIndex("Right"), 1);
                anim.SetLayerWeight(anim.GetLayerIndex("Back"), 0);
            }
               
            // Left
            else
            {
                anim.SetLayerWeight(anim.GetLayerIndex("Front"), 0);
                anim.SetLayerWeight(anim.GetLayerIndex("Left"), 1);
                anim.SetLayerWeight(anim.GetLayerIndex("Right"), 0);
                anim.SetLayerWeight(anim.GetLayerIndex("Back"), 0);
            }

        }

        // Back
        if (vectorDotF < -0.55 && vectorDotF > -1)
        {
            anim.SetLayerWeight(anim.GetLayerIndex("Front"), 0);
            anim.SetLayerWeight(anim.GetLayerIndex("Left"), 0);
            anim.SetLayerWeight(anim.GetLayerIndex("Right"), 0);
            anim.SetLayerWeight(anim.GetLayerIndex("Back"), 1);
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BillboardAnimator : MonoBehaviour
{
    public GameObject[] orientations;
    public SpriteRenderer activeSprite = null;

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

    void SetActiveRotation( float vectorDotF, float vectorDotL )
    {
        int orientationIndex = 0;
        // Front
        if(vectorDotF < 0.775 && vectorDotF > 0.55)
        {
            //Debug.Log("Front");
            // Front Right
            if(vectorDotL < -0.225)
            {
                orientationIndex = 1;
            }

            // Front Left
            else
            {
                orientationIndex = 7;
            }
        }

        // Side
        if(vectorDotF < 0.55 && vectorDotF > -0.225)
        {
            //Debug.Log("Side");
            // Right
            if (vectorDotL < -0.775)
                orientationIndex = 2;

            // Left
            else
                orientationIndex = 6;
        }

        // Back
        if (vectorDotF < -0.225 && vectorDotF > -0.55)
        {
            //Debug.Log("Back");
            // Back Right
            if (vectorDotL > -0.775)
                orientationIndex = 3;

            // Back left
            else
                orientationIndex = 5;
        }

        // Back 
        if (vectorDotF < -0.55 && vectorDotF > -1)
        {
            //Debug.Log("Behind");
            orientationIndex = 4;
        }           

        GameObject orientationGO = orientations[orientationIndex];
        if (orientationGO == null)
            return;

        SpriteRenderer sr = orientationGO.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            activeSprite = sr;
            sr.enabled = true;
        }

        //Debug.Log("Orientation index: " + orientationIndex);
            
        // Disable other sprite renderers
        for(int i = 0; i < orientations.Length; i++)
        {
            if(orientations[i] != orientationGO)
            {
                SpriteRenderer s = orientations[i].GetComponent<SpriteRenderer>();
                if (s != null)
                    s.enabled = false;
            }
        }
    }
}

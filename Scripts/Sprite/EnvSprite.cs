/*
 * A Class used in transforming 2D-sprites to always face the player camera 
 * Used for environment props that are 2D (have no depth)
 * 
 */

using UnityEngine;

public class EnvSprite : MonoBehaviour
{
    private Transform parentObject = null;
    private Camera playerCam = null;

    [SerializeField]
    private bool bYRotationOnly = false;

    private void Start()
    {
        if (transform.parent != null)
            parentObject = transform.parent;

        else
            parentObject = transform;
    }

    private void LateUpdate()
    {
        // Checking Camera.main every frame is expensive, so only do that when necessary
        if (playerCam == null)
        {
            playerCam = FindPlayerCam();
            return;
        }

        //Make sure object rotates with player camera
        Vector3 LookAtDir = new Vector3(playerCam.transform.position.x - transform.position.x, 0, playerCam.transform.position.z - transform.position.z);
        if (bYRotationOnly)
            transform.rotation = Quaternion.LookRotation(-LookAtDir.normalized, parentObject.up);
        
        else
            transform.rotation = Quaternion.LookRotation(-LookAtDir.normalized, Vector3.up);     
    }

    private Camera FindPlayerCam()
    {
        if (Camera.main != null)
            return Camera.main;

        return null;
    }
}

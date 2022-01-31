/*
 * Used to move the camera with player movement
 */

using UnityEngine;

public class MoveCamera : MonoBehaviour
{
    public Transform player = null;

    // Update is called once per frame
    void Update()
    {
        transform.position = player.transform.position;
    }
}

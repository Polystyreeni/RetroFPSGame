// Used to optimize spawned enemy corpses
// This way corpses stop doing physics/collision checks once they hit a flat surface
// Still allowing them to fly during explosions

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class CorpseMovement : MonoBehaviour
{
    [SerializeField] private string hitParticle = string.Empty;
    [SerializeField] private float velocityLimit = 30f;
    Rigidbody rb = null;

    private bool bHitObject = false;
    private bool bHitFloor = false;

    private string objectID = string.Empty;
    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        Invoke(nameof(EnableCollision), 0.1f);
        Invoke(nameof(ForceConstrains), 5f);

        SetObjectID();

        SaveManager.Instance.OnGameSaved += SaveCorpse;
        GameManager.Instance.OnLoadObjects += LoadCorpseData;
    }

    private void OnDestroy()
    {
        SaveManager.Instance.OnGameSaved -= SaveCorpse;
        GameManager.Instance.OnLoadObjects -= LoadCorpseData;
    }

    void SetObjectID()
    {
        if (gameObject.name.Contains("(Clone)"))
            objectID = gameObject.name.Replace("(Clone)", string.Empty);

        else
            objectID = gameObject.name;
    }

    private void OnCollisionEnter(Collision other)
    {
        bHitObject = true;

        if (!rb.useGravity)
            return;

        FxManager.Instance.PlayFX(hitParticle, transform.position, transform.rotation);

        // TODO: If velocity is high, destroy to gibs
        if (rb.velocity.sqrMagnitude > velocityLimit * velocityLimit)
            Destroy(gameObject);

        for(int i = 0; i < other.contactCount; i++)
        {
            if (IsFloor(other.GetContact(i).normal))
            {
                bHitFloor = true;
                break;
            }     
        }

        ActivateConstrains();
    }

    private bool IsFloor(Vector3 v)
    {
        float angle = Vector3.Angle(Vector3.up, v);
        return angle < 35f;
    }

    void EnableCollision()
    {
        rb.useGravity = true;
        if (bHitObject)
            ActivateConstrains();
    }

    void ForceConstrains()
    {
        bHitFloor = true;
        ActivateConstrains();
    }

    /// <summary>
    /// Disables rigidbody movement and collision checking, allowing only Y-movement
    /// </summary>
    void ActivateConstrains()
    {
        rb.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezePositionX;
        if(bHitFloor)
        {
            // If corpse has landed, don't update physics and disable collision for performance (and player doesn't get blocked)
            rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
            rb.isKinematic = true;
            Collider col = GetComponentInChildren<Collider>();
            if (col != null)
                col.enabled = false;
        }
    }

    void SaveCorpse()
    {
        SaveManager.SaveData.ObjectData objectData = new SaveManager.SaveData.ObjectData();
        objectData.prefabName = "Enemy/" + objectID;
        objectData.transformData.position = transform.position;
        objectData.transformData.rotation = transform.rotation.eulerAngles;
        objectData.transformData.scale = transform.localScale;
        objectData.velocity = rb.velocity;

        SaveManager.Instance.gameState.objectList.Add(objectData);
    }

    void LoadCorpseData()
    {
        if(Mathf.Abs(rb.velocity.y) <= 0)
        {
            rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
            rb.isKinematic = true;

            Collider col = GetComponentInChildren<Collider>();
            if (col != null)
                col.enabled = false;
        }
    }
}

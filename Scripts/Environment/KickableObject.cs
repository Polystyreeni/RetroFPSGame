using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class KickableObject : MonoBehaviour
{
    [SerializeField] private float flyForce = 100f;
    [SerializeField] private float maxVelocity = 50f;
    [SerializeField] private string fxName = string.Empty;
    [SerializeField] private string objectID = string.Empty;

    Rigidbody rb = null;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        SaveManager.Instance.OnGameSaved += SaveObject;
    }

    private void OnDestroy()
    {
        SaveManager.Instance.OnGameSaved -= SaveObject;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (fxName != string.Empty)
            FxManager.Instance.PlayFX(fxName, transform.position, Quaternion.identity);

        if (collision.gameObject.CompareTag("Player"))
        {
            Rigidbody plr = collision.gameObject.GetComponent<Rigidbody>();
            if(plr != null)
            {
                Vector3 dir = plr.velocity;
                dir.y = Mathf.Abs(dir.y) * 2f;
                if (dir.magnitude < maxVelocity)
                    rb.AddForce(dir * flyForce);
            }
        }

        else if(collision.gameObject.CompareTag("Enemy"))
        {
            NavMeshAgent agent = collision.gameObject.GetComponent<NavMeshAgent>();
            if(agent != null)
            {
                Vector3 dir = agent.velocity;
                dir.y = dir.y * 2f;
                if (dir.magnitude < maxVelocity)
                    rb.AddForce(dir * flyForce);
            }
        }
    }

    void SaveObject()
    {
        SaveManager.SaveData.ObjectData objectData = new SaveManager.SaveData.ObjectData();
        objectData.transformData.position = transform.position;
        objectData.transformData.rotation = transform.rotation.eulerAngles;
        objectData.transformData.scale = transform.localScale;
        objectData.velocity = rb.velocity;

        objectData.prefabName = objectID;
        SaveManager.Instance.gameState.objectList.Add(objectData);
    }
}

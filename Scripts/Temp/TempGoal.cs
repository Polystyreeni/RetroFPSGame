using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TempGoal : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Player"))
        {
            GameManager.Instance.SavePlayerStats();
            GameManager.Instance.SetLoadPlayerFromSave(true);
            SceneManager.LoadScene("LevelCompleteMenu");
            Destroy(this);
        }
    }
}

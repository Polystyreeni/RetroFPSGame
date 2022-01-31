using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class LevelCompleteMenu : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI killText = null;
    [SerializeField] private TextMeshProUGUI secretText = null;

    private void Start()
    {
        if(GameManager.Instance != null)
        {
            SetStatInfo();
        }
    }

    void SetStatInfo()
    {
        int maxKills = GameManager.Instance.maxKills;
        int currKills = GameManager.Instance.currentKills;

        int maxSecrets = GameManager.Instance.maxSecrets;
        int currSecrets = GameManager.Instance.currentSecrets;

        killText.text = currKills.ToString() + " / " + maxKills.ToString();
        secretText.text = currSecrets.ToString() + " / " + maxSecrets.ToString();
    }

    public void MainMenuButtonPressed()
    {
        if (GameManager.Instance.CurrentLevel != "e1m2")
            GameManager.Instance.LoadLevel("e1m2", false);

        else
            SceneManager.LoadScene("MainMenu");
    }
}

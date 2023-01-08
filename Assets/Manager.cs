using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;


public class Manager : MonoBehaviour
{
    public GameObject gameOverOverlay;

    public void Pause()
    {
        Time.timeScale = 0f;
    }

    public void Resume()
    {
        Time.timeScale = 1f;
    }

    public void GameOver()
    {
        gameOverOverlay.SetActive(true);
    }

    public void Restart()
    {
        gameOverOverlay.SetActive(false);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}


using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    private List<PlayerHealth> players = new List<PlayerHealth>();
    private bool gameOver = false;

    [Header("UI References")]
    public GameObject gameOverCanvas;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void RegisterPlayer(PlayerHealth player)
    {
        if (!players.Contains(player))
            players.Add(player);
    }

    public void CheckForGameOver()
    {
        // If every player is dead, trigger game over
        bool allDead = true;
        foreach (PlayerHealth p in players)
        {
            if (!p.IsDead)
            {
                allDead = false;
                break;
            }
        }

        if (allDead && !gameOver)
        {
            TriggerGameOver();
        }
    }

    private void TriggerGameOver()
    {
        gameOver = true;
        if (gameOverCanvas != null)
            gameOverCanvas.SetActive(true);

        foreach (PlayerHealth p in players)
        {
            p.DisableControls();
        }
    }

    public void TryAgain()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void GiveUp()
    {
        Application.Quit();
    }
}
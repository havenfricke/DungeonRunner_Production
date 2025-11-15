using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("UI")]
    [SerializeField] private GameObject gameOverCanvas;
    [SerializeField] private GameObject gameWinCanvas;

    private int deadPlayers = 0;
    private int totalPlayers = 2;

    public AudioClip winSound;
    public AudioClip loseSound;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (gameOverCanvas != null)
            gameOverCanvas.SetActive(false);
    
        if (gameWinCanvas != null)
            gameWinCanvas.SetActive(false);
    }

    private void Update()
    {
        // Temporary test input
        if (Input.GetKeyDown(KeyCode.G))
        {
            TriggerGameOver();
        }
    }

    public void PlayerDied(PlayerHealth player)
    {
        deadPlayers++;
        Debug.Log($"A player has died! Dead players: {deadPlayers}/{totalPlayers}");

        if (deadPlayers >= totalPlayers)
        {
            TriggerGameOver();
        }
    }

    private void TriggerGameOver()
    {
        Debug.Log("All players have died — Game Over!");
        if (gameOverCanvas != null)
            gameOverCanvas.SetActive(true);

        // Destroy all player GameObjects
        foreach (PlayerHealth player in FindObjectsOfType<PlayerHealth>())
        {
            Destroy(player.gameObject);
        }

        AudioManager.Instance.PlaySFX(loseSound, 0.4f);

        // Optionally pause game time
        Time.timeScale = 0f;
    }

    public void TriggerGameWin()
    {
        Debug.Log("Players reached the treasure!");
        if (gameWinCanvas != null)
            gameWinCanvas.SetActive(true);

        // Destroy all player GameObjects
        foreach (PlayerHealth player in FindObjectsOfType<PlayerHealth>())
        {
            Destroy(player.gameObject);
        }

        AudioManager.Instance.PlaySFX(winSound, 0.4f);

        // Optionally pause game time
        Time.timeScale = 0f;
    }

    // Called by UI buttons
    public void TryAgain()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GiveUp()
    {
        Debug.Log("Quitting game...");
    #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
    #else
        Application.Quit();
    #endif
    }
}
using UnityEngine;
using UnityEngine.UI;

public class HealthBarUIManager : MonoBehaviour
{
    public static HealthBarUIManager Instance { get; private set; }

    [Header("Player Health Fill References")]
    [SerializeField] private Image player1Fill;
    [SerializeField] private Image player2Fill;

    private void Awake()
    {
        // Simple singleton pattern for global access
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public Image GetFillImageForPlayer(int playerNumber)
    {
        if (playerNumber == 1) return player1Fill;
        else if (playerNumber == 2) return player2Fill;

        Debug.LogWarning($"No fill image assigned for player number {playerNumber}!");
        return null;
    }
}
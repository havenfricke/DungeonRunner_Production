using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerJoinHandler : MonoBehaviour
{
    private int playerCount = 0;

    private void OnPlayerJoined(PlayerInput playerInput)
    {
        playerCount++;
        int assignedNumber = playerCount; // 1 for first, 2 for second

        // Get references
        var playerController = playerInput.GetComponent<PlayerController>();
        var healthBar = playerInput.GetComponent<HealthBarUI>();

        // Assign player number to whichever script needs it
        if (playerController != null)
        {
            playerController.SetPlayerNumber(assignedNumber);
        }

        if (healthBar != null)
        {
            healthBar.SetPlayerNumber(assignedNumber);
        }

        Debug.Log($"Player {assignedNumber} joined the game!");
    }

    private void OnPlayerLeft(PlayerInput playerInput)
    {
        Debug.Log($"Player {playerInput.playerIndex + 1} left the game.");
    }
}

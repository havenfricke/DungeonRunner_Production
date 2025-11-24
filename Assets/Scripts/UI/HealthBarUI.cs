using UnityEngine;
using UnityEngine.UI;

public class HealthBarUI : MonoBehaviour
{
    [SerializeField] private Image fillImage;
    [SerializeField] private int playerNumber;

    void Start()
    {
        // Ensure fillImage exists at startup
        if (fillImage == null)
        {
            fillImage = GetComponentInChildren<Image>();
            if (fillImage == null)
                Debug.LogWarning($"{name}: No fillImage found!");
        }
    }

    public void SetPlayerNumber(int number)
    {
        playerNumber = number;

        // If you use an external manager to assign UI images:
        if (HealthBarUIManager.Instance != null)
        {
            fillImage = HealthBarUIManager.Instance.GetFillImageForPlayer(playerNumber);
        }
        else
        {
            Debug.LogWarning("HealthBarUIManager not found!");
        }
    }

    // Called by PlayerHealth to update UI
    public void SetHealthInstant(float current, float max)
    {
        if (fillImage != null)
        {
            fillImage.fillAmount = current / max;
        }
    }
}
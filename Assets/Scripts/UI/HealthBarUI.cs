using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class HealthBarUI : MonoBehaviour
{
    [SerializeField] private Image fillImage;
    [SerializeField] private int playerNumber;

    private float currentHealth = 100f;
    private float maxHealth = 100f;

    private bool isDead = false;

    void Start()
    {
        UpdateHealthBar();
    }

    void Update()
    {
        // Temporary test input
        if (playerNumber == 1)
        {
            if (Input.GetKeyDown(KeyCode.Z))
            {
                ModifyHealth(-10f); // Decrease health
            }
            else if (Input.GetKeyDown(KeyCode.X))
            {
                ModifyHealth(+10f); // Increase health
            }
        }
        else if (playerNumber == 2)
        {
            if (Input.GetKeyDown(KeyCode.N))
            {
                ModifyHealth(-10f);
            }
            else if (Input.GetKeyDown(KeyCode.M))
            {
                ModifyHealth(+10f);
            }
        }
    }

    public void ModifyHealth(float amount)
    {
        if (isDead) return;

        currentHealth = Mathf.Clamp(currentHealth + amount, 0, maxHealth);
        UpdateHealthBar();

        if (currentHealth <= 0f && !isDead)
        {
            StartCoroutine(HandleDeath());
        }

        Debug.Log($"Player {playerNumber} Health: {currentHealth}");
    }

     private IEnumerator HandleDeath()
    {
        isDead = true;

        // Freeze the player (you'll need a reference to the PlayerController)
        PlayerController player = GetComponent<PlayerController>();
        if (player != null)
        {
            player.enabled = false; // disables movement
        }

        // Optionally, you could also disable shooting or other actions here

        // Wait 10 seconds
        yield return new WaitForSeconds(10f);

        // Restore health
        currentHealth = maxHealth;
        UpdateHealthBar();

        // Re-enable the player
        if (player != null)
        {
            player.enabled = true;
        }

        isDead = false;
    }

    private void UpdateHealthBar()
    {
        fillImage.fillAmount = currentHealth / maxHealth;
    }
}
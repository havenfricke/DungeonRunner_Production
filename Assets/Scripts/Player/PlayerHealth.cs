using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public HealthBarUI healthBar;
    public float maxHealth = 100f;
    private float currentHealth;
    private bool isDead = false;

    private void Awake()
    {
        // Automatically find the HealthBarUI on this GameObject
        healthBar = GetComponentInChildren<HealthBarUI>();
        if (healthBar == null)
        {
            Debug.LogWarning($"{name} has no HealthBarUI attached or in its children!");
        }

        currentHealth = maxHealth;
    }

    // Called externally by enemies or other sources of damage
    public void TakeDamage(float damageAmount)
    {
        if (isDead || healthBar == null) return;

        healthBar.ModifyHealth(-damageAmount);
        currentHealth = Mathf.Clamp(currentHealth - damageAmount, 0, maxHealth);

        if (currentHealth <= 0f && !isDead)
        {
            isDead = true;
            HandleDeath();
        }
    }

    private void HandleDeath()
    {
        Debug.Log($"{name} has died!");
        // You can add respawn logic or disable movement here
    }
}
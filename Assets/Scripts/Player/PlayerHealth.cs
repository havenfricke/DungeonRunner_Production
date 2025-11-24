using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public float maxHealth = 100f;
    private float currentHealth;
    private bool isDead = false;

    private HealthBarUI ui;
    private PlayerController controller;

    public AudioClip damageSound;

    private void Awake()
    {
        ui = GetComponentInChildren<HealthBarUI>();
        controller = GetComponent<PlayerController>();

        currentHealth = maxHealth;

        if (ui != null)
            ui.SetHealthInstant(currentHealth, maxHealth);
    }

    public void TakeDamage(float amount)
    {
        if (isDead) return;

        currentHealth = Mathf.Clamp(currentHealth - amount, 0, maxHealth);

        if (ui != null)
            ui.SetHealthInstant(currentHealth, maxHealth);

        AudioManager.Instance.PlaySFX(damageSound, 0.4f);

        if (currentHealth <= 0 && !isDead)
        {
            StartCoroutine(HandleDeath());
        }
    }

    private System.Collections.IEnumerator HandleDeath()
    {
        isDead = true;

        if (controller != null)
            controller.enabled = false;

        Debug.Log($"{name} has died!");

        // Wait 10 seconds
        yield return new WaitForSeconds(10f);

        // Revive
        currentHealth = maxHealth;
        isDead = false;

        if (controller != null)
            controller.enabled = true;

        if (ui != null)
            ui.SetHealthInstant(currentHealth, maxHealth);

        Debug.Log($"{name} has revived!");
    }
}
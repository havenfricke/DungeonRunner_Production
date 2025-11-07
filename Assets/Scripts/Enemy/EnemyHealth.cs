using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 10f;
    private float currentHealth;

    [Header("Optional Settings")]
    [SerializeField] private bool destroyOnDeath = true;
    [SerializeField] private float destroyDelay = 0.2f; // seconds after death before destroy

    private bool isDead = false;

    private void Start()
    {
        currentHealth = maxHealth;
    }

    // This can be called by any damage source, passing in how much damage was dealt
    public void TakeDamage(float damageAmount)
    {
        if (isDead) return;

        currentHealth -= damageAmount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        Debug.Log($"{name} took {damageAmount} damage. Remaining health: {currentHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log($"{name} has been destroyed!");

        // Here’s where you could trigger an animation, sound, or particle effect

        if (destroyOnDeath)
        {
            Destroy(gameObject, destroyDelay);
        }
    }
}
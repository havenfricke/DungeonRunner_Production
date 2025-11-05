using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public HealthBarUI healthBar;
    public float damageAmount = 10f;

    private void Awake()
    {
        // Automatically find the HealthBarUI on this GameObject
        healthBar = GetComponentInChildren<HealthBarUI>();
        if (healthBar == null)
        {
            Debug.LogWarning($"{name} has no HealthBarUI attached or in its children!");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            healthBar.ModifyHealth(-damageAmount);
        }
    }
}
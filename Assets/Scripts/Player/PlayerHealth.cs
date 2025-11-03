using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public HealthBarUI healthBar;
    public float damageAmount = 10f;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            healthBar.ModifyHealth(-damageAmount);
        }
    }
}
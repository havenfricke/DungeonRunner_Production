using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    [Header("Attack Settings")]
    [SerializeField] private float attackRange = 2f;        // how far the raycast reaches
    [SerializeField] private float attackDamage = 2f;       // damage dealt
    [SerializeField] private float attackCooldown = 0.8f;   // delay between attacks

    private float nextAttackTime = 0f;

    public AudioClip attackSound;
    public AudioClip damageSound;

    void Update()
    {
        // Left click + cooldown check
        if (Input.GetMouseButtonDown(0) && Time.time >= nextAttackTime)
        {
            CheckForEnemyHit();
            nextAttackTime = Time.time + attackCooldown;
            Debug.Log("Player Attacked!");
            AudioManager.Instance.PlaySFX(attackSound);
        }
    }

    private void CheckForEnemyHit()
    {
        RaycastHit hit;
        Vector3 origin = transform.position + Vector3.up * 0.6f; // lift slightly from ground
        Vector3 direction = transform.forward;

        if (Physics.Raycast(origin, direction, out hit, attackRange))
        {
            if (hit.collider.CompareTag("Enemy"))
            {
                EnemyHealth enemyHealth = hit.collider.GetComponent<EnemyHealth>();
                if (enemyHealth != null)
                {
                    enemyHealth.TakeDamage(attackDamage);
                    AudioManager.Instance.PlaySFX(damageSound, 0.4f);
                    Debug.Log($"Enemy {hit.collider.name} took {attackDamage} damage!");
                }
            }
        }

        // visualize ray for debugging
        Debug.DrawRay(origin, direction * attackRange, Color.red, 10f);
    }
}
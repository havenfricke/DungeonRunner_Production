using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Windows;

public class PlayerAttack : MonoBehaviour
{
    [Header("Attack Settings")]
    [SerializeField] private float attackRange = 2f;        // how far the raycast reaches
    [SerializeField] private float attackDamage = 2f;       // damage dealt
    [SerializeField] private float attackCooldown = 0.8f;   // delay between attacks

    private InputAction attack;
    private PlayerInput input;

    private float nextAttackTime = 0f;

    private void Awake()
    {
        input = GetComponent<PlayerInput>();

        // Safer: get Attack from the *current* map
        attack = input.currentActionMap.FindAction("Attack", throwIfNotFound: true);
    }

    private void OnEnable()
    {
        attack.Enable();
        attack.performed += OnAttackPerformed;
    }

    private void OnDisable()
    {
        attack.performed -= OnAttackPerformed;
        attack.Disable();
    }

    private void OnAttackPerformed(InputAction.CallbackContext ctx)
    {
        if (Time.time < nextAttackTime) return;

        Debug.Log("Attack input received");
        CheckForEnemyHit();
        nextAttackTime = Time.time + attackCooldown;
        Debug.Log("Player Attacked!");
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
                    Debug.Log($"Enemy {hit.collider.name} took {attackDamage} damage!");
                }
            }
        }

        // visualize ray for debugging
        Debug.DrawRay(origin, direction * attackRange, Color.red, 10f);
    }
}
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerControlsManager : MonoBehaviour
{
    private InputAction attack;
    private PlayerInput input;
    private Animator animator;

    [SerializeField]
    private float attackInterval = 0.1f;

    private void Awake()
    {
        input = GetComponent<PlayerInput>();
        animator = GetComponentInChildren<Animator>();

        if (animator == null)
        {
            Debug.LogWarning("PlayerControlsManager: No Animator found in children.");
        }
    }

    private void OnEnable()
    {
        if (input == null)
        {
            input = GetComponent<PlayerInput>();
        }

        // Use the same pattern as PlayerAttack
        var map = input.currentActionMap;
        attack = map.FindAction("Attack", throwIfNotFound: true);

        attack.Enable();
        attack.performed += OnAttackPerformed;
    }

    private void OnDisable()
    {
        if (attack != null)
        {
            attack.performed -= OnAttackPerformed;
            attack.Disable();
        }
    }

    // ------ ATTACK INPUT START ------ //
    private void OnAttackPerformed(InputAction.CallbackContext ctx)
    {
        if (animator == null) return;

        animator.SetBool("Attack", true);
        StartDelayedExecution(attackInterval);
    }

    void ReadAttackInput()
    {
        // No longer needed (event-based), but keep if you still reference it elsewhere
        // Left empty intentionally
    }

    public void StartDelayedExecution(float delayTime)
    {
        StartCoroutine(ExecuteAfterDelay(delayTime));
    }

    IEnumerator ExecuteAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        FinishAttackInput();
    }

    void FinishAttackInput()
    {
        if (animator == null) return;
        animator.SetBool("Attack", false);
    }
    // ------ ATTACK INPUT END ------ //
}

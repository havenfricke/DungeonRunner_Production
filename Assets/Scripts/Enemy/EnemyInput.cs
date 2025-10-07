// EnemyInput.cs
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(EnemyAwareness))]
public class EnemyInput : MonoBehaviour
{
    [Header("References")]
    private NavMeshAgent agent;
    private Animator animator;
    private EnemyAwareness awareness;

    [Header("Targeting")]
    [Tooltip("If multiple targets are in range, the enemy will pick the closest.")]
    public Transform currentTarget;

    [Header("Movement")]
    [Tooltip("How fast the transform rotates to face travel direction.")]
    public float rotationSpeed = 8f;

    [Tooltip("Velocity magnitude below this counts as idle.")]
    public float speedDeadzone = 0.035f;

    [Tooltip("Snap animator values to 0 if below this.")]
    public float snapDeadzone = 0.02f;

    [Tooltip("Extra distance before 'arrived'.")]
    public float arriveTolerance = 0.05f;

    [Header("Animation")]
    [Tooltip("Damp time for smoothing parameter changes while moving.")]
    public float animDampMoving = 0.08f;

    [Tooltip("Damp time used when snapping to zero (0 = instant).")]
    public float animDampStopping = 0.0f;

    // Cached hashes
    private static readonly int HashMoveX = Animator.StringToHash("Enemy_MoveX");
    private static readonly int HashMoveY = Animator.StringToHash("Enemy_MoveY");
    private static readonly int HashSpeed = Animator.StringToHash("Enemy_Speed");

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
        awareness = GetComponent<EnemyAwareness>();

        // 2D/top-down friendly
        agent.updateRotation = false;
        agent.updateUpAxis = false;
        agent.autoBraking = true;
        agent.stoppingDistance = Mathf.Max(0.1f, agent.stoppingDistance);
    }

    void Update()
    {
        // If not aware hard stop & zero params
        if (!awareness.isAware)
        {
            HaltAndZero();
            return;
        }

        // Target acquisition
        FindClosestTarget();

        // If no target or off mesh hard stop & zero
        if (currentTarget == null || !agent.isOnNavMesh)
        {
            HaltAndZero();
            return;
        }

        // Drive pathing
        agent.isStopped = false;
        agent.SetDestination(currentTarget.position);

        // If building path or no path yet keep zeroed
        if (agent.pathPending || !agent.hasPath)
        {
            SnapAnimatorZero();
            return;
        }

        // Consider arrived?
        bool arrived = agent.remainingDistance <= (agent.stoppingDistance + arriveTolerance);
        Vector3 worldMove = arrived ? Vector3.zero : GetAgentWorldMove();

        // If movement is negligible zero
        if (worldMove.sqrMagnitude <= speedDeadzone * speedDeadzone)
        {
            HaltVelocityOnly(); // keep path, but zero anim
            SnapAnimatorZero();
            return;
        }

        // Face movement and feed animator (LOCAL space)
        FaceMovement(worldMove);

        Vector3 localMove = transform.InverseTransformDirection(worldMove.normalized);
        float normSpeed = (agent.speed > 0.0001f)
            ? Mathf.Clamp01(worldMove.magnitude / agent.speed)
            : 0f;

        // Apply deadzone + damping for smooth control while moving
        float damp = animDampMoving;
        float x = Mathf.Abs(localMove.x) < snapDeadzone ? 0f : localMove.x;
        float y = Mathf.Abs(localMove.z) < snapDeadzone ? 0f : localMove.z;
        float s = normSpeed < snapDeadzone ? 0f : normSpeed;

        animator.SetFloat(HashMoveX, x, damp, Time.deltaTime);
        animator.SetFloat(HashMoveY, y, damp, Time.deltaTime);
        animator.SetFloat(HashSpeed, s, damp, Time.deltaTime);
    }

    private Vector3 GetAgentWorldMove()
    {
        // Prefer actual velocity; fallback to desired/steering for early frames
        Vector3 v = agent.velocity;
        if (v.sqrMagnitude <= speedDeadzone * speedDeadzone)
        {
            Vector3 desired = agent.desiredVelocity;
            if (desired.sqrMagnitude > 0.0001f) v = desired;
            else if (agent.hasPath)
            {
                Vector3 toCorner = agent.steeringTarget - transform.position;
                toCorner.y = 0f;
                if (toCorner.sqrMagnitude > 0.0001f)
                    v = toCorner.normalized * agent.speed;
            }
        }
        v.y = 0f;
        return v;
    }

    private void FaceMovement(Vector3 worldMove)
    {
        if (worldMove.sqrMagnitude <= speedDeadzone * speedDeadzone) return;
        Quaternion targetRot = Quaternion.LookRotation(worldMove.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
    }

    private void FindClosestTarget()
    {
        float radius = Mathf.Max(0.01f, awareness.detectionRadius);
        int layerMask = awareness.targetLayer;
        var hits = Physics.OverlapSphere(transform.position, radius, layerMask);

        if (hits.Length == 0) { currentTarget = null; return; }

        float best = float.PositiveInfinity;
        Transform bestT = null;
        Vector3 self = transform.position;

        for (int i = 0; i < hits.Length; i++)
        {
            float d = (hits[i].transform.position - self).sqrMagnitude;
            if (d < best) { best = d; bestT = hits[i].transform; }
        }
        currentTarget = bestT;
    }

    private void HaltAndZero()
    {
        if (agent.hasPath) agent.ResetPath();
        agent.isStopped = true;
        SnapAnimatorZero();
    }

    private void HaltVelocityOnly()
    {
        // Keep the path, but treat as idle for animation purposes
        // (no changes to the agent; we just want 0s in the animator)
    }

    private void SnapAnimatorZero()
    {
        // Instant snap to 0 to prevent “tail” from damping
        animator.SetFloat(HashMoveX, 0f, animDampStopping, Time.deltaTime);
        animator.SetFloat(HashMoveY, 0f, animDampStopping, Time.deltaTime);
        animator.SetFloat(HashSpeed, 0f, animDampStopping, Time.deltaTime);
    }

    // Optional: visualize arrival radius in Scene view
    void OnDrawGizmosSelected()
    {
        if (agent == null) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, agent.stoppingDistance + arriveTolerance);
    }
}

// EnemyInput.cs
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(EnemyAwareness))]
public class EnemyInput : MonoBehaviour
{
    private NavMeshAgent agent;
    private Animator animator;
    private EnemyAwareness awareness;

    [Header("Targeting")]
    public Transform currentTarget;

    [Header("Movement")]
    public float rotationSpeed = 10f;
    public float speedDeadzone = 0.035f;
    public float snapDeadzone = 0.02f;
    public float arriveTolerance = 0.08f;
    [Tooltip("If true, set stoppingDistance slightly inside attackRange so the agent parks in striking distance.")]
    public bool alignStopToAttackRange = true;
    public float stopInsideFactor = 0.9f; // stoppingDistance = attackRange * this

    [Header("Animation")]
    public float animDampMoving = 0.08f;
    public float animDampStopping = 0.0f;

    [Header("Attack")]
    public float attackRange = 1.8f;
    public float attackDuration = 0.45f;
    public float attackCooldown = 1f;
    public bool holdPositionWhileAttacking = false;
    private bool hasDealtDamage = false;

    [Tooltip("If true, requires a clear ray to target to attack.")]
    public bool requireLineOfSight = true;
    [Tooltip("Layers considered obstacles for LOS. Make sure NOT to include the player's layer.")]
    public LayerMask lineOfSightObstacles = ~0;

    // Animator hashes
    private static readonly int HashMoveX = Animator.StringToHash("Enemy_MoveX");
    private static readonly int HashMoveY = Animator.StringToHash("Enemy_MoveY");
    private static readonly int HashSpeed = Animator.StringToHash("Enemy_Speed");
    private static readonly int HashAttack = Animator.StringToHash("Enemy_Attack");

    // Spawn
    private Vector3 spawnPosition;
    private bool returningHome;

    // Attack state
    private bool isAttacking;
    private float attackTimer;
    private float cooldownTimer;

    // Debug
    [Header("Debug")]
    public bool debugAttackGate = false;
    private bool warnedMissingAttackParam = false;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
        awareness = GetComponent<EnemyAwareness>();

        agent.updateRotation = false;
        agent.updateUpAxis = false;
        agent.autoBraking = true;

        spawnPosition = transform.position;

        // Optional: align stopping distance to attack range for consistent reach
        if (alignStopToAttackRange)
        {
            agent.stoppingDistance = Mathf.Max(0.05f, attackRange * stopInsideFactor);
        }
        else
        {
            agent.stoppingDistance = Mathf.Max(0.1f, agent.stoppingDistance);
        }

        // One time check for animator bool
        if (animator != null)
        {
            bool hasParam = false;
            var ps = animator.parameters;
            for (int i = 0; i < ps.Length; i++)
            {
                if (ps[i].nameHash == HashAttack && ps[i].type == AnimatorControllerParameterType.Bool)
                {
                    hasParam = true;
                    break;
                }
            }
            if (!hasParam)
            {
                warnedMissingAttackParam = true;
                Debug.LogWarning("EnemyInput: Animator missing bool parameter 'Enemy_Attack'. The attack will not flip.");
            }
        }
    }

    void Update()
    {
        if (!agent.isOnNavMesh)
        {
            HaltAndZero();
            return;
        }

        // Update timers
        if (cooldownTimer > 0f) cooldownTimer -= Time.deltaTime;
        if (isAttacking)
        {
            attackTimer -= Time.deltaTime;
            // Perform the attack raycast when the attack "connects"
            if (attackTimer <= attackDuration * 0.5f && !hasDealtDamage) // Fires once near the end of the attack
            {
                PerformAttackRaycast();
                hasDealtDamage = true;
            }

            if (attackTimer <= 0f)
            {
                EndAttack();
                hasDealtDamage = false;
            }
        }

        // Awareness and target
        if (awareness.isAware)
        {
            returningHome = false;
            FindClosestTarget();
        }
        else
        {
            currentTarget = null;
            returningHome = true;
            if (isAttacking) EndAttack();
        }

        // Navigation intent
        if (currentTarget != null)
        {
            agent.isStopped = false;
            agent.SetDestination(currentTarget.position);
        }
        else if (returningHome)
        {
            agent.isStopped = false;
            agent.SetDestination(spawnPosition);
        }
        else
        {
            HaltAndZero();
            return;
        }

        if (agent.pathPending || !agent.hasPath)
        {
            SnapAnimatorZero();
            return;
        }

        bool arrived = agent.remainingDistance <= (agent.stoppingDistance + arriveTolerance);
        Vector3 worldMove = arrived ? Vector3.zero : GetAgentWorldMove();

        if (currentTarget != null)
            FacePlayer(currentTarget.position);
        else
            FaceTravel(worldMove);

        // Attack gate
        if (currentTarget != null && !isAttacking)
        {
            float dist = HorizontalDistance(transform.position, currentTarget.position);
            bool inRange = dist <= attackRange;
            bool hasLOS = !requireLineOfSight || HasClearLineOfSight(currentTarget);

            if (debugAttackGate)
            {
                Debug.Log($"EnemyInput AttackGate: dist={dist:F2} inRange={inRange} hasLOS={hasLOS} cooldown={cooldownTimer:F2}");
            }

            if (inRange && hasLOS && cooldownTimer <= 0f)
            {
                StartAttack();
            }
        }

        // During attack, optionally freeze locomotion
        if (isAttacking)
        {
            if (holdPositionWhileAttacking)
            {
                agent.velocity = Vector3.zero;
                agent.isStopped = true;
            }
            SnapAnimatorZero();
            return;
        }

        // Drive locomotion
        if (worldMove.sqrMagnitude <= speedDeadzone * speedDeadzone)
        {
            SnapAnimatorZero();
            if (returningHome && arrived)
            {
                agent.ResetPath();
                agent.isStopped = true;
            }
            return;
        }

        Vector3 localMoveDir = transform.InverseTransformDirection(worldMove.normalized);
        float normSpeed = (agent.speed > 0.0001f) ? Mathf.Clamp01(worldMove.magnitude / agent.speed) : 0f;

        float x = Mathf.Abs(localMoveDir.x) < snapDeadzone ? 0f : localMoveDir.x;
        float y = Mathf.Abs(localMoveDir.z) < snapDeadzone ? 0f : localMoveDir.z;
        float s = normSpeed < snapDeadzone ? 0f : normSpeed;

        animator.SetFloat(HashMoveX, x, animDampMoving, Time.deltaTime);
        animator.SetFloat(HashMoveY, y, animDampMoving, Time.deltaTime);
        animator.SetFloat(HashSpeed, s, animDampMoving, Time.deltaTime);
    }

    private void StartAttack()
    {
        if (warnedMissingAttackParam == false && animator != null)
        {
            // Optional extra log to prove the gate fired
            if (debugAttackGate) Debug.Log("EnemyInput StartAttack: setting Enemy_Attack = true");
        }

        isAttacking = true;
        attackTimer = Mathf.Max(0.05f, attackDuration);
        animator.SetBool(HashAttack, true);
    }

    private void PerformAttackRaycast()
    {
        // Starting point slightly above the ground to match the enemy's chest/weapon height
        Vector3 origin = transform.position + Vector3.up * 1f;

        // Forward direction based on enemy facing
        Vector3 direction = transform.forward;

        // You can tweak these or expose them as public variables if desired
        float attackRange = 2f;
        float attackDamage = 10f;

        // Visualize the ray in the Scene view
        Debug.DrawRay(origin, direction * attackRange, Color.red, 0.3f);

        // Perform the raycast
        if (Physics.Raycast(origin, direction, out RaycastHit hit, attackRange))
        {
            if (hit.collider.CompareTag("Player"))
            {
                PlayerHealth playerHealth = hit.collider.GetComponentInParent<PlayerHealth>();
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(attackDamage);
                    Debug.Log($"{name} hit {hit.collider.name} for {attackDamage} damage!");
                }
            }
        }
    }

    private void EndAttack()
    {
        if (debugAttackGate) Debug.Log("EnemyInput EndAttack: clearing Enemy_Attack and starting cooldown");

        isAttacking = false;
        cooldownTimer = Mathf.Max(0.05f, attackCooldown);
        animator.SetBool(HashAttack, false);
        agent.isStopped = false;
    }

    // LOS that ignores the target and only treats obstacle layers as blocking
    private bool HasClearLineOfSight(Transform target)
    {
        Vector3 origin = transform.position + Vector3.up * 0.5f;
        Vector3 dest = target.position + Vector3.up * 0.5f;
        Vector3 dir = dest - origin;
        float dist = dir.magnitude;
        if (dist <= 0.0001f) return true;
        dir /= dist;

        // Only test against obstacle layers. Do NOT include the player's layer here.
        if (Physics.Raycast(origin, dir, out RaycastHit hit, dist, lineOfSightObstacles, QueryTriggerInteraction.Ignore))
        {
            // Something on an obstacle layer is between us and the target
            return false;
        }
        return true;
    }

    private float HorizontalDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f; b.y = 0f;
        return Vector3.Distance(a, b);
    }

    private Vector3 GetAgentWorldMove()
    {
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

    private void FacePlayer(Vector3 playerPos)
    {
        Vector3 toPlayer = playerPos - transform.position;
        toPlayer.y = 0f;
        if (toPlayer.sqrMagnitude < 0.0001f) return;
        Quaternion facePlayer = Quaternion.LookRotation(toPlayer.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, facePlayer, rotationSpeed * Time.deltaTime);
    }

    private void FaceTravel(Vector3 worldMove)
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
        if (isAttacking) EndAttack();
    }

    private void SnapAnimatorZero()
    {
        animator.SetFloat(HashMoveX, 0f, animDampStopping, Time.deltaTime);
        animator.SetFloat(HashMoveY, 0f, animDampStopping, Time.deltaTime);
        animator.SetFloat(HashSpeed, 0f, animDampStopping, Time.deltaTime);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, agent != null ? agent.stoppingDistance + arriveTolerance : arriveTolerance);
    }
}

using UnityEngine;

public class EnemyAwareness : MonoBehaviour
{
    [Range(1, 10)]public float detectionRadius = 10f;
    public LayerMask targetLayer;
    [HideInInspector]
    public bool isAware = false;

    void Update()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, detectionRadius, targetLayer);

        if (hitColliders.Length > 0)
        {
            Debug.Log($"Found {hitColliders.Length} colliders within range.");
            foreach (Collider hitCollider in hitColliders)
            {
                isAware = true;
                Debug.Log($"Enemy.cs: detected player at {hitCollider.gameObject.transform.position}.");
            }
        }
        else
        {
            isAware = false;
            Debug.Log($"Enemy.cs: lost player.");
        }
    }
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}

using System.ComponentModel.Design;
using UnityEditor.PackageManager;
using UnityEngine;

public class EnemySpawn : MonoBehaviour
{
    [SerializeField] GameObject enemyPrefab;
    int prefabIndex;
    private void Start()
    {
        Instantiate(enemyPrefab, transform.position, transform.rotation);
    }
}

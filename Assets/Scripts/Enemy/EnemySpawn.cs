using System.ComponentModel.Design;
using UnityEditor.PackageManager;
using UnityEngine;

public class EnemySpawn : MonoBehaviour
{
    [SerializeField] GameObject enemyPrefab;
    private Vector3 position;
    public Vector3 spawnPosition
    {
        get => position;
    }
    private void Start()
    {
        transform.position = position;
        Instantiate(enemyPrefab);
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
public class EnemySpawner : MonoBehaviour
{
    [SerializeField]
    private Transform Player;
    [SerializeField]
    private Enemy EnemyPrefab;
    [SerializeField]
    [Range(1, 100)]
    private int EnemiesToSpawn = 10;
    [SerializeField]
    private bool RandomizePrediction;

    private NavMeshTriangulation Triangulation;

    private void Awake()
    {
        Triangulation = NavMesh.CalculateTriangulation();
    }

    private void Start()
    {
        for (int i = 0; i < EnemiesToSpawn; i++)
        {
            Enemy enemy = Instantiate(EnemyPrefab,
               Triangulation.vertices[Random.Range(0, Triangulation.vertices.Length)],
               Quaternion.identity
            );
            enemy.Movement.Triangulation = Triangulation;
            enemy.ThrowAttack.Target = Player;
            if (RandomizePrediction)
            {
                enemy.ThrowAttack.MovementPredictionMode = Random.value > 0.5f ? EnemyThrowAttack.PredictionMode.CurrentVelocity : EnemyThrowAttack.PredictionMode.AverageVelocity;
                enemy.ThrowAttack.HistoricalResolution = Random.Range(10, 100);
                enemy.ThrowAttack.HistoricalTime = Random.Range(0.25f, 5f);
                enemy.ThrowAttack.ForceRatio = Random.Range(0, 1f);
            }
        }
    }
}

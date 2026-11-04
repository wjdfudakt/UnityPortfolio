using System.Collections.Generic;
using UnityEngine;

public class TargetSpawner : MonoBehaviour
{
    [SerializeField] private ShootingTarget[] targetPrefabs;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private ScoreManager scoreManager;
    [SerializeField] private GameManager gameManager;

    [SerializeField] private float spawnInterval = 1f;
    [SerializeField] private float targetLifetime = 4f;
    [SerializeField] private int maxActiveTargets = 5;
    [SerializeField] private int maxForbiddenTargets = 3;

    private ShootingTarget[] activeTargets;
    private float spawnTimer = 0f;

    private void Start()
    {
        activeTargets = new ShootingTarget[spawnPoints.Length];
    }

    private void Update()
    {
         if (gameManager == null || !gameManager.IsPlaying)
        {
            return;
        }

        spawnTimer += Time.deltaTime;

        if (spawnTimer >= spawnInterval)
        {
            spawnTimer = 0f;

            SpawnTarget();
        }
    }

    public void StopSpawning()
    {
        enabled = false;

        for (int i = 0; i < activeTargets.Length; i++)
        {
            if (activeTargets[i] != null)
            {
                Destroy(activeTargets[i].gameObject);
            }
        }
    }

    private void SpawnTarget()
    {
        int activeCount = 0;

        for (int i = 0; i < activeTargets.Length; i++)
        {
            if (activeTargets[i] != null)
            {
                activeCount++;
            }
        }

        if (activeCount >= maxActiveTargets)
        {
            return;
        }
        List<int> emptySlots = new List<int>();

        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (activeTargets[i] == null)
            {
                emptySlots.Add(i);
            }
        }

        if (emptySlots.Count == 0)
        {
            return;
        }

        int randomSlot = Random.Range(0, emptySlots.Count);
        int pointIndex = emptySlots[randomSlot];

        int forbiddenCount = 0;

        for (int i = 0; i < activeTargets.Length; i++)
        {
            if (activeTargets[i] != null &&
                activeTargets[i].IsForbidden)
            {
                forbiddenCount++;
            }
        }

        int availableTypeCount = Mathf.Min(
            gameManager.CurrentRound,
            targetPrefabs.Length
        );

        if (forbiddenCount >= maxForbiddenTargets)
        {
            availableTypeCount = Mathf.Min(availableTypeCount, 2);
        }

        int prefabIndex = Random.Range(0, availableTypeCount);

        ShootingTarget newTarget = Instantiate(
            targetPrefabs[prefabIndex],
            spawnPoints[pointIndex].position,
            Quaternion.identity
        );

        newTarget.Initialize(scoreManager, targetLifetime);

        activeTargets[pointIndex] = newTarget;
    }
}
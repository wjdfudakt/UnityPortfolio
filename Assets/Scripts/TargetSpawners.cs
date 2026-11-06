using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class TargetSpawner : MonoBehaviour
{
    [SerializeField] private ShootingTarget[] targetPrefabs;
    [SerializeField] private Transform[] spawnPoints;

    [SerializeField] private ScoreManager scoreManager;
    [SerializeField] private GameManager gameManager;

    [SerializeField] private float spawnInterval = 1f;
    [SerializeField] private float targetLifetime = 4f;

    [SerializeField] private int maxActiveTargets = 5;
    [SerializeField] private int maxForbiddenTargets = 2;

    [SerializeField] private float spawnIntervalDecrease = 0.05f;
    [SerializeField] private float minSpawnInterval = 0.3f;

    [Header("Reaction Test")]
    [SerializeField] private Transform reactionSpawnPoint;

    private ShootingTarget[] activeTargets;
    private ShootingTarget reactionTarget;

    private float currentSpawnInterval;
    private float spawnTimer;

    private int lastTimeAttackPoint = -1;
    private bool stopped;

    public bool IsReactionConfigured
    {
        get
        {
            return isActiveAndEnabled
                && !stopped
                && gameManager != null
                && reactionSpawnPoint != null
                && targetPrefabs != null
                && targetPrefabs.Length > 0
                && targetPrefabs[0] != null
                && targetPrefabs[0].Type == TargetType.Static;
        }
    }

    private void Awake()
    {
        int pointCount = spawnPoints == null
            ? 0
            : spawnPoints.Length;

        activeTargets = new ShootingTarget[pointCount];
    }

    private void Start()
    {
        if (gameManager == null)
        {
            Debug.LogError(
                "TargetSpawner의 Game Manager를 연결해 주세요.",
                this
            );

            return;
        }

        switch (GameSettings.SelectedMode)
        {
            case GameMode.Round:
                {
                    int completedRounds = gameManager.CurrentRound - 1;

                    currentSpawnInterval = Mathf.Max(
                        minSpawnInterval,
                        spawnInterval
                            - completedRounds * spawnIntervalDecrease
                    );

                    break;
                }

            case GameMode.TimeAttack:
                {
                    SpawnTimeAttackTarget();
                    break;
                }

            case GameMode.ReactionTime:
                {
                    break;
                }
        }
    }

    private void Update()
    {
        if (stopped)
            return;

        if (GameSettings.SelectedMode != GameMode.Round)
            return;

        if (gameManager == null || !gameManager.IsPlaying)
            return;

        spawnTimer += Time.deltaTime;

        if (spawnTimer >= currentSpawnInterval)
        {
            spawnTimer = 0f;
            SpawnTarget();
        }
    }

    private void SpawnTarget()
    {
        if (stopped ||
            GameSettings.SelectedMode != GameMode.Round)
        {
            return;
        }

        if (gameManager == null || !gameManager.IsPlaying)
            return;

        if (targetPrefabs == null || targetPrefabs.Length == 0)
            return;

        int activeCount = 0;
        int forbiddenCount = 0;

        List<int> emptySlots = new List<int>();

        for (int i = 0; i < activeTargets.Length; i++)
        {
            if (activeTargets[i] != null)
            {
                activeCount++;

                if (activeTargets[i].IsForbidden)
                {
                    forbiddenCount++;
                }
            }
            else if (spawnPoints[i] != null)
            {
                emptySlots.Add(i);
            }
        }

        if (activeCount >= maxActiveTargets)
            return;

        if (emptySlots.Count == 0)
            return;

        int availableTypeCount = Mathf.Min(
            gameManager.CurrentRound,
            targetPrefabs.Length
        );

        List<ShootingTarget> candidates =
            new List<ShootingTarget>();

        for (int i = 0; i < availableTypeCount; i++)
        {
            ShootingTarget prefab = targetPrefabs[i];

            if (prefab == null)
                continue;

            if (prefab.IsForbidden &&
                forbiddenCount >= maxForbiddenTargets)
            {
                continue;
            }

            candidates.Add(prefab);
        }

        if (candidates.Count == 0)
            return;

        int pointIndex = emptySlots[
            Random.Range(0, emptySlots.Count)
        ];

        ShootingTarget selectedPrefab = candidates[
            Random.Range(0, candidates.Count)
        ];

        ShootingTarget newTarget = Instantiate(
            selectedPrefab,
            spawnPoints[pointIndex].position,
            Quaternion.identity
        );

        activeTargets[pointIndex] = newTarget;

        newTarget.Initialize(scoreManager, targetLifetime);
    }

    private void SpawnTimeAttackTarget()
    {
        if (stopped ||
            GameSettings.SelectedMode != GameMode.TimeAttack)
        {
            return;
        }

        if (gameManager == null || !gameManager.IsPlaying)
            return;

        if (targetPrefabs == null ||
            targetPrefabs.Length == 0 ||
            targetPrefabs[0] == null)
        {
            return;
        }

        List<int> availablePoints = new List<int>();

        for (int i = 0; i < activeTargets.Length; i++)
        {
            if (spawnPoints[i] != null &&
                i != lastTimeAttackPoint)
            {
                availablePoints.Add(i);
            }
        }

        if (availablePoints.Count == 0 &&
            lastTimeAttackPoint >= 0 &&
            lastTimeAttackPoint < activeTargets.Length &&
            spawnPoints[lastTimeAttackPoint] != null)
        {
            availablePoints.Add(lastTimeAttackPoint);
        }

        if (availablePoints.Count == 0)
            return;

        int pointIndex = availablePoints[
            Random.Range(0, availablePoints.Count)
        ];

        ShootingTarget newTarget = Instantiate(
            targetPrefabs[0],
            spawnPoints[pointIndex].position,
            Quaternion.identity
        );

        lastTimeAttackPoint = pointIndex;
        activeTargets[pointIndex] = newTarget;

        newTarget.InitializeForTimeAttack(
            scoreManager,
            OnTimeAttackTargetHit
        );
    }

    private void OnTimeAttackTargetHit()
    {
        if (lastTimeAttackPoint >= 0 &&
            lastTimeAttackPoint < activeTargets.Length)
        {
            activeTargets[lastTimeAttackPoint] = null;
        }

        SpawnTimeAttackTarget();
    }

    public bool SpawnReactionTarget(Action onHit)
    {
        if (GameSettings.SelectedMode != GameMode.ReactionTime)
            return false;

        if (!IsReactionConfigured)
            return false;

        if (!gameManager.IsPlaying || onHit == null)
            return false;

        ClearReactionTarget();

        reactionTarget = Instantiate(
            targetPrefabs[0],
            reactionSpawnPoint.position,
            Quaternion.identity
        );

        reactionTarget.InitializeForReaction(() =>
        {
            reactionTarget = null;

            if (!stopped &&
                GameSettings.SelectedMode == GameMode.ReactionTime &&
                gameManager != null &&
                gameManager.IsPlaying)
            {
                onHit();
            }
        });

        return true;
    }

    public void ClearReactionTarget()
    {
        if (reactionTarget == null)
            return;

        reactionTarget.gameObject.SetActive(false);
        Destroy(reactionTarget.gameObject);

        reactionTarget = null;
    }

    public void StopSpawning()
    {
        stopped = true;
        enabled = false;

        ClearReactionTarget();

        if (activeTargets == null)
            return;

        for (int i = 0; i < activeTargets.Length; i++)
        {
            if (activeTargets[i] == null)
                continue;

            activeTargets[i].gameObject.SetActive(false);
            Destroy(activeTargets[i].gameObject);

            activeTargets[i] = null;
        }
    }
}
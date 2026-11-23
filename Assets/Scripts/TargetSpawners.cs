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

    [Header("Tracking")]
    [SerializeField] private GameObject trackingTargetPrefab;

    [SerializeField, Min(0f)]
    private float trackingDepthOffset = 2f;

    [Header("Memory Shooting")]
    [SerializeField] private ShootingTarget memoryTargetPrefab;

    private GameObject trackingTargetObject;

    private ShootingTarget[] activeTargets;
    private ShootingTarget reactionTarget;

    private float currentSpawnInterval;
    private float spawnTimer;

    private int lastTimeAttackPoint = -1;
    private bool stopped = true;
    private int sessionVersion;

    private bool OwnsSession =>
        !stopped &&
        gameManager != null &&
        gameManager.ActiveSpawner == this;

    public bool IsReactionConfigured
    {
        get
        {
            return isActiveAndEnabled
                && OwnsSession
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

    // 씬 시작 시 생성하지 않는다.
    // 구역에 입장할 때 GameManager가 호출한다.
    public void BeginSession(
        GameManager manager,
        ScoreManager scores,
        int round)
    {
        StopSpawning();

        gameManager = manager;
        scoreManager = scores;

        int pointCount = spawnPoints == null
            ? 0
            : spawnPoints.Length;

        activeTargets = new ShootingTarget[pointCount];

        spawnTimer = 0f;
        lastTimeAttackPoint = -1;

        currentSpawnInterval = Mathf.Max(
            minSpawnInterval,
            spawnInterval -
            Mathf.Max(0, round - 1) * spawnIntervalDecrease
        );

        enabled = true;
        stopped = false;
    }

    // 선택한 모드에 필요한 연결 상태를 확인한다.
    public bool ValidateMode(GameMode mode, out string error)
    {
        error = "";

        if (!gameObject.activeInHierarchy)
        {
            error = "구역의 TargetSpawner 오브젝트를 활성화해 주세요.";
            return false;
        }

        // 메모리 모드는 전용 프리팹과 모든 스폰 포인트를 검사한다.
        if (mode == GameMode.Memory)
        {
            return ValidateMemoryConfiguration(out error);
        }

        if (mode == GameMode.Tracking)
        {
            if (trackingTargetPrefab == null)
            {
                error = "Tracking Target Prefab을 연결해 주세요.";
                return false;
            }
        }
        else if (targetPrefabs == null ||
                 targetPrefabs.Length == 0 ||
                 targetPrefabs[0] == null ||
                 targetPrefabs[0].Type != TargetType.Static)
        {
            error = "Target Prefabs의 Element 0에 Static 타겟을 연결해 주세요.";
            return false;
        }

        if (mode == GameMode.ReactionTime)
        {
            if (reactionSpawnPoint != null)
                return true;

            error = "Reaction Spawn Point를 연결해 주세요.";
            return false;
        }

        bool found = false;
        Vector3 min = Vector3.zero;
        Vector3 max = Vector3.zero;

        if (spawnPoints != null)
        {
            foreach (Transform point in spawnPoints)
            {
                if (point == null)
                    continue;

                if (!found)
                {
                    min = point.position;
                    max = point.position;
                    found = true;
                }
                else
                {
                    min = Vector3.Min(min, point.position);
                    max = Vector3.Max(max, point.position);
                }
            }
        }

        if (!found ||
            (mode == GameMode.Tracking &&
             (max.x - min.x <= 0.01f ||
              max.y - min.y <= 0.01f)))
        {
            error = "Spawn Points 연결 및 가로·세로 이동 범위를 확인해 주세요.";
            return false;
        }

        return true;
    }

    // 메모리 모드의 프리팹과 배치 설정을 검사한다.
    private bool ValidateMemoryConfiguration(out string error)
    {
        error = "";

        if (memoryTargetPrefab == null)
        {
            error = "Memory Target Prefab을 연결해 주세요.";
            return false;
        }

        if (!memoryTargetPrefab.ValidateMemorySetup(out error))
        {
            return false;
        }

        if (spawnPoints == null || spawnPoints.Length < 2)
        {
            error = "메모리 모드에는 스폰 포인트가 2개 이상 필요합니다.";
            return false;
        }

        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (spawnPoints[i] == null)
            {
                error = "Spawn Points의 Element " + i +
                    "에 스폰 포인트를 연결해 주세요.";

                return false;
            }

            // 서로 다른 번호의 타겟이 같은 위치에 생성되지 않도록 한다.
            for (int j = 0; j < i; j++)
            {
                float distance = Vector3.Distance(
                    spawnPoints[i].position,
                    spawnPoints[j].position
                );

                if (distance < 0.01f)
                {
                    error = "메모리 모드의 스폰 포인트 위치가 겹쳐 있습니다.";
                    return false;
                }
            }
        }

        return true;
    }

    private void Update()
    {
        if (!OwnsSession || !gameManager.IsPlaying)
            return;

        if (GameSettings.SelectedMode == GameMode.TimeAttack)
        {
            bool hasTarget = false;

            foreach (ShootingTarget target in activeTargets)
            {
                if (target != null)
                {
                    hasTarget = true;
                    break;
                }
            }

            if (!hasTarget)
            {
                SpawnTimeAttackTarget();
            }

            return;
        }

        // 라운드 모드에서만 일정 간격으로 타겟을 생성한다.
        // 메모리 타겟은 SpawnMemoryTargets에서 한 번에 생성한다.
        if (GameSettings.SelectedMode != GameMode.Round)
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
        if (!OwnsSession ||
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
        if (!OwnsSession ||
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

        int version = sessionVersion;

        newTarget.InitializeForTimeAttack(
            scoreManager,
            () =>
            {
                if (version == sessionVersion && OwnsSession)
                {
                    OnTimeAttackTargetHit();
                }
            }
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

        int version = sessionVersion;

        reactionTarget.InitializeForReaction(() =>
        {
            if (version != sessionVersion || !OwnsSession)
                return;

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

    public Transform SpawnTrackingTarget(
        out Vector3 minPosition,
        out Vector3 maxPosition)
    {
        minPosition = Vector3.zero;
        maxPosition = Vector3.zero;

        if (GameSettings.SelectedMode != GameMode.Tracking ||
            !OwnsSession ||
            !isActiveAndEnabled)
        {
            return null;
        }

        if (trackingTargetPrefab == null ||
            spawnPoints == null)
        {
            Debug.LogError(
                "트래킹 타겟 프리팹과 Spawn Points를 확인해 주세요.",
                this
            );

            return null;
        }

        bool foundPoint = false;

        foreach (Transform point in spawnPoints)
        {
            if (point == null)
                continue;

            if (!foundPoint)
            {
                minPosition = point.position;
                maxPosition = point.position;
                foundPoint = true;
            }
            else
            {
                minPosition = Vector3.Min(
                    minPosition,
                    point.position
                );

                maxPosition = Vector3.Max(
                    maxPosition,
                    point.position
                );
            }
        }

        if (!foundPoint ||
            maxPosition.x - minPosition.x <= 0.01f ||
            maxPosition.y - minPosition.y <= 0.01f)
        {
            Debug.LogError(
                "트래킹에는 가로와 세로로 떨어진 스폰 포인트들이 필요합니다.",
                this
            );

            return null;
        }

        ClearTrackingTarget();

        minPosition.z -= trackingDepthOffset;
        maxPosition.z -= trackingDepthOffset;

        Vector3 centerPosition =
            (minPosition + maxPosition) * 0.5f;

        trackingTargetObject = Instantiate(
            trackingTargetPrefab,
            centerPosition,
            trackingTargetPrefab.transform.rotation
        );

        trackingTargetObject.SetActive(true);

        return trackingTargetObject.transform;
    }

    private void ClearTrackingTarget()
    {
        if (trackingTargetObject == null)
            return;

        trackingTargetObject.SetActive(false);
        Destroy(trackingTargetObject);

        trackingTargetObject = null;
    }

    // 메모리 모드 시작 시 GameManager에서 호출한다.
    // 배열 순서가 각 타겟의 번호가 된다.
    public ShootingTarget[] SpawnMemoryTargets()
    {
        if (GameSettings.SelectedMode != GameMode.Memory ||
            !OwnsSession ||
            !isActiveAndEnabled)
        {
            return null;
        }

        if (!ValidateMode(GameMode.Memory, out string error))
        {
            Debug.LogError(error, this);
            return null;
        }

        // 다시 시작하거나 다음 라운드로 넘어갈 때
        // 이전 타겟이 남아 있지 않도록 정리한다.
        ClearActiveTargets();

        activeTargets = new ShootingTarget[spawnPoints.Length];

        for (int i = 0; i < spawnPoints.Length; i++)
        {
            ShootingTarget newTarget = Instantiate(
                memoryTargetPrefab,
                spawnPoints[i].position,
                memoryTargetPrefab.transform.rotation
            );

            activeTargets[i] = newTarget;

            // 일반 Initialize를 호출하지 않으므로
            // 제한 시간이 지나도 자동으로 제거되지 않는다.
            newTarget.InitializeForMemory(this, i);
        }

        // 외부에서 반환 배열을 변경하더라도
        // 스포너의 타겟 관리 배열은 유지한다.
        return (ShootingTarget[])activeTargets.Clone();
    }

    // 현재 구역에서 생성한 타겟인지 확인한다.
    public bool OwnsTarget(ShootingTarget target)
    {
        if (!OwnsSession || target == null)
            return false;

        if (target == reactionTarget)
            return true;

        if (activeTargets != null)
        {
            foreach (ShootingTarget active in activeTargets)
            {
                if (target == active)
                    return true;
            }
        }

        return false;
    }

    // 일반·타임어택·메모리 타겟을 공통으로 정리한다.
    private void ClearActiveTargets()
    {
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

    public void StopSpawning()
    {
        stopped = true;
        sessionVersion++;

        ClearReactionTarget();
        ClearTrackingTarget();
        ClearActiveTargets();
    }
}
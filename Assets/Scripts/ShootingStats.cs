using UnityEngine;

public class ShootingStats : MonoBehaviour
{
    public int TotalShots { get; private set; }

    // 일반 타겟을 하나 이상 맞힌 발사 횟수
    public int SuccessfulShots { get; private set; }

    // 종류별로 맞힌 타겟 수
    public int StaticHits { get; private set; }
    public int MovingHits { get; private set; }
    public int ForbiddenHits { get; private set; }

    private bool currentShotHasValidHit;

    public float Accuracy => TotalShots == 0
        ? 0f
        : (float)SuccessfulShots / TotalShots * 100f;

    public void ResetStats()
    {
        TotalShots = 0;
        SuccessfulShots = 0;

        StaticHits = 0;
        MovingHits = 0;
        ForbiddenHits = 0;

        currentShotHasValidHit = false;
    }

    public void RegisterShot()
    {
        TotalShots++;

        // 새로운 발사의 명중 여부를 초기화한다.
        currentShotHasValidHit = false;
    }

    public void RegisterHit(TargetType type)
    {
        switch (type)
        {
            case TargetType.Static:
                StaticHits++;
                RegisterSuccessfulShot();
                break;

            case TargetType.Moving:
                MovingHits++;
                RegisterSuccessfulShot();
                break;

            case TargetType.Forbidden:
                // 금지 타겟은 명중률의 성공 횟수에 포함하지 않는다.
                ForbiddenHits++;
                break;
        }
    }

    private void RegisterSuccessfulShot()
    {
        if (TotalShots <= 0 || currentShotHasValidHit)
            return;

        // 한 발사로 여러 타겟을 맞혀도 한 번만 증가한다.
        currentShotHasValidHit = true;
        SuccessfulShots++;
    }
}
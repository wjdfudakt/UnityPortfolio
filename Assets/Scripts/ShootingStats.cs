using UnityEngine;

public class ShootingStats : MonoBehaviour
{
    public int TotalShots { get; private set; }
    public int StaticHits { get; private set; }
    public int MovingHits { get; private set; }
    public int ForbiddenHits { get; private set; }
    public float Accuracy => TotalShots == 0 ? 0f
        : (float)(StaticHits + MovingHits) / TotalShots * 100f;

    public void ResetStats()
    {
        TotalShots = 0;
        StaticHits = 0;
        MovingHits = 0;
        ForbiddenHits = 0;
    }

    public void RegisterShot() { TotalShots++; }

    public void RegisterHit(TargetType type)
    {
        switch (type)
        {
            case TargetType.Static: StaticHits++; break;
            case TargetType.Moving: MovingHits++; break;
            case TargetType.Forbidden: ForbiddenHits++; break;
        }
    }
}

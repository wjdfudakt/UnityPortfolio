using UnityEngine;

public class ShootingStats : MonoBehaviour
{
    public int TotalShots { get; private set; }
    public int StaticHits { get; private set; }
    public int MovingHits { get; private set; }
    public int ForbiddenHits { get; private set; }

    public float Accuracy
    {
        get
        {
            if (TotalShots == 0)
            {
                return 0f;
            }

            int validHits = StaticHits + MovingHits;

            return (float)validHits / TotalShots * 100f;
        }
    }

    public void RegisterShot()
    {
        TotalShots++;
    }

    public void RegisterHit(TargetType type)
    {
        switch (type)
        {
            case TargetType.Static:
                StaticHits++;
                break;

            case TargetType.Moving:
                MovingHits++;
                break;

            case TargetType.Forbidden:
                ForbiddenHits++;
                break;
        }
    }
}
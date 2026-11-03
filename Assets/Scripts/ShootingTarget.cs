using UnityEngine;

public class ShootingTarget : MonoBehaviour
{
    public void Hit()
    {
        Debug.Log("표적 명중!");

        Destroy(gameObject);
    }
}
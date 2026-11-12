using UnityEngine;
using UnityEngine.UI;

public class CrosshairUI : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private RectTransform canvasRect;
    [SerializeField] private RectTransform crosshair;
    [SerializeField] private GameObject hitMarker;
    [SerializeField] private Image hitLineA;
    [SerializeField] private Image hitLineB;
    [SerializeField] private float hitDisplayTime = 0.15f;
    private float hitTimer;

    private void Awake()
    {
        if (crosshair != null)
        {
            crosshair.anchorMin = crosshair.anchorMax = new Vector2(0.5f, 0.5f);
            crosshair.pivot = new Vector2(0.5f, 0.5f);
            crosshair.anchoredPosition = Vector2.zero;
        }
        ResetFeedback();
    }

    private void LateUpdate()
    {
        bool show = gameManager != null && gameManager.CanLook;
        if (crosshair != null) crosshair.gameObject.SetActive(show);
        if (!show)
        {
            ResetFeedback();
            return;
        }
        hitTimer = Mathf.Max(0f, hitTimer - Time.deltaTime);
        if (hitMarker != null) hitMarker.SetActive(hitTimer > 0f);
    }

    public void ShowHit(bool isForbidden)
    {
        Color color = isForbidden ? Color.red : Color.green;
        if (hitLineA != null) hitLineA.color = color;
        if (hitLineB != null) hitLineB.color = color;
        hitTimer = hitDisplayTime;
        if (hitMarker != null) hitMarker.SetActive(true);
    }

    public void ResetFeedback()
    {
        hitTimer = 0f;
        if (hitMarker != null) hitMarker.SetActive(false);
    }
}

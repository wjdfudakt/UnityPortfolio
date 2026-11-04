using UnityEngine;
using UnityEngine.InputSystem;
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

    private float hitTimer = 0f;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.None;

        hitMarker.SetActive(false);
    }

    private void LateUpdate()
    {
        bool showCrosshair =
            gameManager != null &&
            gameManager.IsPlaying &&
            Mouse.current != null &&
            Application.isFocused;

        crosshair.gameObject.SetActive(showCrosshair);

        Cursor.visible = !showCrosshair;

        if (!showCrosshair)
        {
            hitTimer = 0f;
            hitMarker.SetActive(false);
            return;
        }

        Vector2 mousePosition = Mouse.current.position.ReadValue();

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            mousePosition,
            null,
            out Vector2 localPosition))
        {
            crosshair.anchoredPosition = localPosition;
        }

        hitTimer = Mathf.Max(0f, hitTimer - Time.deltaTime);

        hitMarker.SetActive(hitTimer > 0f);
    }

    public void ShowHit(bool isForbidden)
    {
        Color markerColor;

        if (isForbidden)
        {
            markerColor = Color.red;
        }
        else
        {
            markerColor = Color.green;
        }

        hitLineA.color = markerColor;
        hitLineB.color = markerColor;

        hitTimer = hitDisplayTime;
        hitMarker.SetActive(true);
    }

    private void OnDisable()
    {
        Cursor.visible = true;
    }
}
using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-50)]
[RequireComponent(typeof(CharacterController))]
public class HubPlayerController : MonoBehaviour
{
    [SerializeField] private Camera viewCamera;
    [SerializeField, Min(0f)] private float moveSpeed = 5f;
    [SerializeField, Min(0f)] private float mouseSensitivity = 0.1f;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float pitchLimit = 80f;

    private CharacterController controller;
    private GameManager gameManager;
    private float pitch;
    private float verticalSpeed;

    public Camera ViewCamera => viewCamera;
    public Vector3 CenterPosition => controller != null
        ? transform.TransformPoint(controller.center) : transform.position;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    public void Initialize(GameManager manager)
    {
        gameManager = manager;
    }

    private void Update()
    {
        if (gameManager == null || !gameManager.CanLook) return;

        if (Mouse.current != null)
        {
            // Mouse.delta는 이미 한 프레임의 이동량이므로 deltaTime을 곱하지 않는다.
            Vector2 delta = Mouse.current.delta.ReadValue() * mouseSensitivity;
            transform.Rotate(0f, delta.x, 0f);
            float limit = Mathf.Clamp(pitchLimit, 1f, 89f);
            pitch = Mathf.Clamp(pitch - delta.y, -limit, limit);
            viewCamera.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        // 모드 진행 중에는 회전만 허용하고 위치와 중력은 갱신하지 않는다.
        if (!gameManager.CanMove)
        {
            verticalSpeed = 0f;
            return;
        }

        Vector2 input = Vector2.zero;
        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed) input.y += 1f;
            if (Keyboard.current.sKey.isPressed) input.y -= 1f;
            if (Keyboard.current.dKey.isPressed) input.x += 1f;
            if (Keyboard.current.aKey.isPressed) input.x -= 1f;
        }
        input = Vector2.ClampMagnitude(input, 1f);
        Vector3 velocity = (transform.right * input.x + transform.forward * input.y) * moveSpeed;
        if (controller.isGrounded && verticalSpeed < 0f) verticalSpeed = -2f;
        verticalSpeed += gravity * Time.deltaTime;
        velocity.y = verticalSpeed;
        controller.Move(velocity * Time.deltaTime);
    }

    public void Teleport(Transform point)
    {
        if (point == null) return;
        if (controller == null) controller = GetComponent<CharacterController>();
        controller.enabled = false;
        transform.SetPositionAndRotation(point.position,
            Quaternion.Euler(0f, point.eulerAngles.y, 0f));
        float limit = Mathf.Clamp(pitchLimit, 1f, 89f);
        pitch = Mathf.Clamp(Mathf.DeltaAngle(0f, point.eulerAngles.x), -limit, limit);
        viewCamera.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        verticalSpeed = 0f;
        controller.enabled = true;
        Physics.SyncTransforms();
    }
}

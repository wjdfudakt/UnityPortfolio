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

    // 마우스로 조작하는 기본 조준 각도
    private float pitch;
    private float verticalSpeed;

    private float lookSensitivityMultiplier = 1f;

    // 기본 조준 각도에 추가로 적용할 반동
    private float targetRecoilPitch;
    private float currentRecoilPitch;

    private float recoilApplySpeed = 25f;
    private float recoilReturnSpeed = 8f;
    private float recoilRecoveryStartTime;

    public Camera ViewCamera => viewCamera;

    public Vector3 CenterPosition => controller != null
        ? transform.TransformPoint(controller.center)
        : transform.position;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    public void Initialize(GameManager manager)
    {
        gameManager = manager;
        ResetCameraRecoil();
    }

    private void Update()
    {
        if (gameManager == null ||
            !gameManager.CanLook ||
            viewCamera == null)
        {
            return;
        }

        float limit = Mathf.Clamp(pitchLimit, 1f, 89f);

        if (Mouse.current != null)
        {
            // 기본 조준 각도는 마우스 입력으로만 변경한다.
            Vector2 delta =
                Mouse.current.delta.ReadValue()
                * mouseSensitivity
                * lookSensitivityMultiplier;

            transform.Rotate(0f, delta.x, 0f);

            pitch = Mathf.Clamp(
                pitch - delta.y,
                -limit,
                limit
            );
        }

        if (gameManager.IsPlaying && !gameManager.IsTracking)
        {
            UpdateCameraRecoil(limit);
        }
        else
        {
            ResetCameraRecoil();
        }

        ApplyCameraRotation();

        // 모드 진행 중에는 회전만 허용한다.
        if (!gameManager.CanMove)
        {
            verticalSpeed = 0f;
            return;
        }

        Vector2 input = Vector2.zero;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed)
                input.y += 1f;

            if (Keyboard.current.sKey.isPressed)
                input.y -= 1f;

            if (Keyboard.current.dKey.isPressed)
                input.x += 1f;

            if (Keyboard.current.aKey.isPressed)
                input.x -= 1f;
        }

        input = Vector2.ClampMagnitude(input, 1f);

        Vector3 velocity =
            (transform.right * input.x + transform.forward * input.y)
            * moveSpeed;

        if (controller.isGrounded && verticalSpeed < 0f)
        {
            verticalSpeed = -2f;
        }

        verticalSpeed += gravity * Time.deltaTime;
        velocity.y = verticalSpeed;

        controller.Move(velocity * Time.deltaTime);
    }

    public void AddCameraRecoil(
        float angle,
        float applySpeed,
        float returnSpeed,
        float returnDelay,
        float maxAngle)
    {
        if (gameManager == null ||
            !gameManager.IsPlaying ||
            !gameManager.CanLook ||
            gameManager.IsTracking)
        {
            return;
        }

        recoilApplySpeed = Mathf.Max(0.1f, applySpeed);
        recoilReturnSpeed = Mathf.Max(0.1f, returnSpeed);

        float limit = Mathf.Clamp(pitchLimit, 1f, 89f);

        // 현재 조준 위치에서 위로 회전할 수 있는 범위까지만 허용한다.
        float availableAngle = Mathf.Max(0f, pitch + limit);
        float allowedAngle = Mathf.Min(
            Mathf.Max(0f, maxAngle),
            availableAngle
        );

        targetRecoilPitch = Mathf.Clamp(
            targetRecoilPitch + Mathf.Max(0f, angle),
            0f,
            allowedAngle
        );

        // 발사할 때마다 회복 시작 시간을 뒤로 미룬다.
        recoilRecoveryStartTime =
            Time.time + Mathf.Max(0f, returnDelay);
    }

    private void UpdateCameraRecoil(float limit)
    {
        float deltaTime = Time.deltaTime;

        if (deltaTime <= 0f)
            return;

        // 마지막 발사 후 대기 시간이 지나면 반동을 줄인다.
        if (Time.time >= recoilRecoveryStartTime)
        {
            float returnFactor =
                Mathf.Exp(-recoilReturnSpeed * deltaTime);

            targetRecoilPitch *= returnFactor;
        }

        float applyBlend =
            1f - Mathf.Exp(-recoilApplySpeed * deltaTime);

        currentRecoilPitch = Mathf.Lerp(
            currentRecoilPitch,
            targetRecoilPitch,
            applyBlend
        );

        // 마우스로 위쪽 회전 제한에 접근한 경우에도
        // 화면에 반영할 수 없는 반동이 쌓이지 않게 한다.
        float availableAngle = Mathf.Max(0f, pitch + limit);

        targetRecoilPitch = Mathf.Min(
            targetRecoilPitch,
            availableAngle
        );

        currentRecoilPitch = Mathf.Min(
            currentRecoilPitch,
            availableAngle
        );

        // 충분히 작아지면 정확히 기본 조준 각도로 복귀한다.
        if (targetRecoilPitch < 0.001f &&
            currentRecoilPitch < 0.001f)
        {
            targetRecoilPitch = 0f;
            currentRecoilPitch = 0f;
        }
    }

    private void ApplyCameraRotation()
    {
        if (viewCamera == null)
            return;

        float limit = Mathf.Clamp(pitchLimit, 1f, 89f);

        // 기본 조준 각도에서 반동 각도만큼 위를 바라본다.
        float finalPitch = Mathf.Clamp(
            pitch - currentRecoilPitch,
            -limit,
            limit
        );

        viewCamera.transform.localRotation =
            Quaternion.Euler(finalPitch, 0f, 0f);
    }

    public void ResetCameraRecoil()
    {
        targetRecoilPitch = 0f;
        currentRecoilPitch = 0f;
        recoilRecoveryStartTime = 0f;

        ApplyCameraRotation();
    }

    public void SetLookSensitivityMultiplier(float multiplier)
    {
        lookSensitivityMultiplier = Mathf.Clamp(
            multiplier,
            0.05f,
            1f
        );
    }

    public void Teleport(Transform point)
    {
        if (point == null)
            return;

        if (controller == null)
        {
            controller = GetComponent<CharacterController>();
        }

        controller.enabled = false;

        transform.SetPositionAndRotation(
            point.position,
            Quaternion.Euler(0f, point.eulerAngles.y, 0f)
        );

        float limit = Mathf.Clamp(pitchLimit, 1f, 89f);

        pitch = Mathf.Clamp(
            Mathf.DeltaAngle(0f, point.eulerAngles.x),
            -limit,
            limit
        );

        ResetCameraRecoil();

        verticalSpeed = 0f;
        controller.enabled = true;

        Physics.SyncTransforms();
    }

    private void OnDisable()
    {
        ResetCameraRecoil();
    }
}
using UnityEngine;

public class MouseLook : MonoBehaviour
{
    public float sensitivity = 1.5f;
    public float smoothing = 1.5f;

    [Header("Nhìn lên / xuống")]
    public Transform cameraTransform;       // để trống thì tự lấy Main Camera
    public float verticalSensitivity = 1.5f;
    public float minPitch = -60f;           // giới hạn cúi xuống
    public float maxPitch = 60f;            // giới hạn ngửa lên
    public bool invertY = false;

    private float xMousePos, yMousePos;
    private float smoothedMousePos, smoothedMouseY;

    private float currenntLookingPos;       // góc xoay ngang
    private float currentPitch;             // góc nhìn dọc

    private void Start()
    {
        if (!cameraTransform && Camera.main) cameraTransform = Camera.main.transform;
        if (!cameraTransform) Debug.LogWarning("MouseLook: chưa gán Camera Transform, sẽ không nhìn lên xuống được.");

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        if (PauseMenu.IsPaused || WakeUpIntro.IsPlaying) return;
        GetInput();
        ModifyInput();
        MovePlayer();
    }

    void GetInput()
    {
        xMousePos = Input.GetAxisRaw("Mouse X");
        yMousePos = Input.GetAxisRaw("Mouse Y");
    }

    void ModifyInput()
    {
        xMousePos *= sensitivity * smoothing;
        smoothedMousePos = Mathf.Lerp(smoothedMousePos, xMousePos, 1f / smoothing);

        yMousePos *= verticalSensitivity * smoothing;
        smoothedMouseY = Mathf.Lerp(smoothedMouseY, yMousePos, 1f / smoothing);
    }

    void MovePlayer()
    {
        // Xoay ngang: xoay cả Player
        currenntLookingPos += smoothedMousePos;
        transform.localRotation = Quaternion.AngleAxis(currenntLookingPos, transform.up);

        // Xoay dọc: chỉ xoay camera
        currentPitch += invertY ? smoothedMouseY : -smoothedMouseY;
        currentPitch = Mathf.Clamp(currentPitch, minPitch, maxPitch);
        if (cameraTransform)
            cameraTransform.localRotation = Quaternion.Euler(currentPitch, 0f, 0f);
    }

    // Dùng khi lưu / load ván
    public float LookingPos => currenntLookingPos;
    public float LookingPitch => currentPitch;

    public void SetLookingPos(float value, float pitch = 0f)
    {
        currenntLookingPos = value;
        currentPitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        xMousePos = 0f;
        yMousePos = 0f;
        smoothedMousePos = 0f;
        smoothedMouseY = 0f;
        transform.localRotation = Quaternion.AngleAxis(currenntLookingPos, Vector3.up);
        if (cameraTransform)
            cameraTransform.localRotation = Quaternion.Euler(currentPitch, 0f, 0f);
    }
}
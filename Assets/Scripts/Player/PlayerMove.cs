using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    public float playerSpeed = 10f;
    public float momentumDamping = 5f;

    private CharacterController myCC;

    public Animator camAnim;
    private bool isWalking;
    private Vector3 inputVector;
    private Vector3 movementVector;
    private float myGravity = -10f;

    // ===== MỚI: sprint / crouch =====
    public float sprintMultiplier = 1.5f;      // chạy nhanh gấp 1.5 lần đi bộ
    public float crouchMultiplier = 0.5f;      // cúi thì đi chậm bằng một nửa
    public float crouchHeight = 1.2f;          // chiều cao CharacterController khi cúi
    public Transform cameraRoot;               // object cha của Main Camera (tạo trong Editor)
    public float crouchCameraOffset = -0.8f;   // camera hạ xuống bao nhiêu khi cúi
    public float crouchSmooth = 10f;           // camera hạ / nâng nhanh hay chậm

    // ===== MỚI: bán kính tiếng động (mét) theo từng trạng thái =====
    public float idleNoise = 2f;
    public float crouchNoise = 4f;
    public float walkNoise = 8f;
    public float sprintNoise = 16f;

    private bool isSprinting;
    private bool isCrouching;
    private float standHeight;                 // chiều cao lúc đứng (lấy từ CharacterController)
    private float feetY;                       // vị trí bàn chân (local) để giữ chân không bị nhấc lên
    private float cameraRootStartY;

    // Enemy đọc giá trị này để biết Player đang ồn tới đâu
    public float NoiseRadius { get; private set; }

    void Start()
    {
        myCC = GetComponent<CharacterController>();

        // ===== MỚI =====
        standHeight = myCC.height;
        feetY = myCC.center.y - myCC.height / 2f;
        if (cameraRoot) cameraRootStartY = cameraRoot.localPosition.y;
        NoiseRadius = idleNoise;
    }

    void Update()
    {
        if (PauseMenu.IsPaused)
        {
            NoiseRadius = idleNoise;   // MỚI: enemy vẫn chạy Update khi pause nên phải hạ tiếng động xuống
            return;
        }
        GetInput();
        HandleCrouch();   // MỚI
        MovePlayer();
        UpdateNoise();    // MỚI

        camAnim.SetBool("isWalking", isWalking);
    }

    void GetInput()
    {

        if (Input.GetKey(KeyCode.W) ||
           Input.GetKey(KeyCode.A) ||
           Input.GetKey(KeyCode.S) ||
           Input.GetKey(KeyCode.D))
        {
            inputVector = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));
            inputVector.Normalize();
            inputVector = transform.TransformDirection(inputVector);

            isWalking = true;
        }
        else
        {
            inputVector = Vector3.Lerp(inputVector, Vector3.zero, momentumDamping * Time.deltaTime);

            isWalking = false;
        }

        // ===== MỚI: cúi (giữ C hoặc Ctrl) và chạy (giữ Shift + W) =====
        bool crouchPressed = Input.GetKey(KeyCode.C) || Input.GetKey(KeyCode.LeftControl);
        if (crouchPressed) isCrouching = true;
        else if (isCrouching && CanStandUp()) isCrouching = false;   // thả phím nhưng trần còn thấp thì vẫn cúi

        isSprinting = isWalking && Input.GetKey(KeyCode.LeftShift) && Input.GetAxisRaw("Vertical") > 0f && !isCrouching;

        float speed = playerSpeed;
        if (isSprinting) speed *= sprintMultiplier;
        else if (isCrouching) speed *= crouchMultiplier;

        movementVector = (inputVector * speed) + (Vector3.up * myGravity);
    }

    void MovePlayer()
    {
        myCC.Move(movementVector * Time.deltaTime);
    }

    // ===== MỚI: hạ thấp CharacterController và camera khi cúi =====
    void HandleCrouch()
    {
        float h = isCrouching ? crouchHeight : standHeight;
        myCC.height = h;

        Vector3 c = myCC.center;
        c.y = feetY + h / 2f;      // đổi chiều cao nhưng giữ bàn chân đứng yên, chỉ phần đầu thấp xuống
        myCC.center = c;

        if (cameraRoot)
        {
            // Hạ cameraRoot (cha), không hạ trực tiếp Main Camera vì Animator đang điều khiển vị trí của nó
            float targetY = cameraRootStartY + (isCrouching ? crouchCameraOffset : 0f);
            Vector3 pos = cameraRoot.localPosition;
            pos.y = Mathf.Lerp(pos.y, targetY, crouchSmooth * Time.deltaTime);
            cameraRoot.localPosition = pos;
        }
    }

    // ===== MỚI: bắn 1 tia từ giữa người lên trên, trúng trần thì chưa được đứng dậy =====
    bool CanStandUp()
    {
        return !Physics.Raycast(transform.position, Vector3.up, standHeight / 2f + 0.1f,
                                ~0, QueryTriggerInteraction.Ignore);
    }

    // ===== MỚI: đổi trạng thái hiện tại thành bán kính tiếng động =====
    void UpdateNoise()
    {
        if (!isWalking) NoiseRadius = idleNoise;
        else if (isSprinting) NoiseRadius = sprintNoise;
        else if (isCrouching) NoiseRadius = crouchNoise;
        else NoiseRadius = walkNoise;
    }

    // (Tùy chọn) vẽ vòng tiếng động trong Scene view để dễ debug ======= Mới
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, Application.isPlaying ? NoiseRadius : walkNoise);
    }



}
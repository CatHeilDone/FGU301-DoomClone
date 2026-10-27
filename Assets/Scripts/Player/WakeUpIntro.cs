using UnityEngine;

// Hiệu ứng "finally awake": màn hình đen -> xám -> hiện gameplay,
// camera từ nằm dưới đất nhìn lên trời -> ngồi dậy -> đứng dậy.
// Gắn lên object Player (cùng chỗ với PlayerMove).
public class WakeUpIntro : MonoBehaviour
{
    // static giống PauseMenu.IsPaused: các script khác chỉ cần hỏi "intro đang chạy không?"
    public static bool IsPlaying { get; private set; }

    public bool playIntro = true;           // bỏ tick để tắt intro khi đang làm các tính năng khác

    [Header("Thời gian (giây, phải lớn hơn 0)")]
    public float blackHold = 1.5f;          // đen hoàn toàn
    public float toGrayTime = 2f;           // đen -> xám
    public float sitUpTime = 3.5f;          // nằm -> ngồi, đồng thời lớp xám mờ dần để hiện gameplay
    public float standUpTime = 2.5f;        // ngồi -> đứng

    [Header("Màu màn hình")]
    public Color grayColor = new Color(0.5f, 0.5f, 0.5f);

    [Header("Tư thế camera")]
    public float lyingPitch = -80f;         // góc nhìn lúc nằm (số âm = ngửa lên trời)
    public float lyingOffset = -1.5f;       // lúc nằm camera thấp hơn lúc đứng bao nhiêu mét
    public float sittingPitch = -25f;       // lúc ngồi vẫn hơi ngẩng lên
    public float sittingOffset = -0.8f;

    [Header("Chóng mặt")]
    public float swayAmount = 3f;           // độ nghiêng đầu (độ)
    public float swayFrequency = 2f;

    private Transform cameraRoot;           // object cha của Main Camera (lấy từ PlayerMove)
    private Transform camTransform;
    private Camera weaponCamera;            // camera riêng chỉ vẽ khẩu súng
    private Vector3 baseLocalPos;           // vị trí cameraRoot lúc đứng bình thường
    private float t;                        // thời gian đã trôi qua
    private float totalTime;
    private float overlayAlpha = 1f;
    private Color overlayColor = Color.black;

    void Awake()
    {
        IsPlaying = false;   // static nên phải reset mỗi lần vào scene (giống PauseMenu)

        // Bấm CONTINUE từ menu thì không chiếu intro (cờ này được SaveGameController xóa ở Start, sau Awake)
        if (!playIntro || SaveSystem.ContinueRequested)
            enabled = false;
    }

    void Start()
    {
        var move = GetComponent<PlayerMove>();
        cameraRoot = move ? move.cameraRoot : null;
        if (!cameraRoot)
        {
            Debug.LogWarning("WakeUpIntro: PlayerMove chưa gán Camera Root, bỏ qua intro.");
            enabled = false;
            return;
        }

        camTransform = Camera.main.transform;
        var w = camTransform.Find("WeaponCamera");
        if (w) weaponCamera = w.GetComponent<Camera>();

        baseLocalPos = cameraRoot.localPosition;
        totalTime = blackHold + toGrayTime + sitUpTime + standUpTime;

        IsPlaying = true;
        if (weaponCamera) weaponCamera.enabled = false;   // đang nằm thì chưa cầm súng
    }

    void Update()
    {
        t += Time.deltaTime;
        if (Input.GetKeyDown(KeyCode.Space)) t = totalTime;   // bấm Space để bỏ qua intro

        // Mỗi pha có 1 tiến độ chạy từ 0 -> 1
        float revealStart = blackHold + toGrayTime;
        float toGray = Smooth(t, blackHold, toGrayTime);
        float sitUp = Smooth(t, revealStart, sitUpTime);
        float standUp = Smooth(t, revealStart + sitUpTime, standUpTime);

        // Màn hình: đen -> xám (vẫn che kín), rồi lớp xám mờ dần
        overlayColor = Color.Lerp(Color.black, grayColor, toGray);
        overlayAlpha = 1f - sitUp;

        // Camera: nằm -> ngồi -> đứng (đoạn sau nội suy tiếp từ kết quả đoạn trước)
        float pitch = Mathf.Lerp(Mathf.Lerp(lyingPitch, sittingPitch, sitUp), 0f, standUp);
        float offset = Mathf.Lerp(Mathf.Lerp(lyingOffset, sittingOffset, sitUp), 0f, standUp);
        float roll = Mathf.Sin(t * swayFrequency) * swayAmount * (1f - standUp);

        // Hạ cameraRoot (cha), không hạ trực tiếp Main Camera vì Animator đang điều khiển vị trí của nó
        cameraRoot.localPosition = baseLocalPos + Vector3.up * offset;
        camTransform.localRotation = Quaternion.Euler(pitch, 0f, roll);

        if (standUp > 0f && weaponCamera) weaponCamera.enabled = true;   // bắt đầu đứng dậy thì cầm súng lên

        if (t >= totalTime) EndIntro();
    }

    // Tiến độ 0 -> 1 của một pha, có làm mượt đầu và cuối (SmoothStep)
    float Smooth(float time, float start, float duration)
    {
        return Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((time - start) / duration));
    }

    void EndIntro()
    {
        cameraRoot.localPosition = baseLocalPos;
        camTransform.localRotation = Quaternion.identity;   // khớp với MouseLook (góc nhìn bắt đầu từ 0)
        if (weaponCamera) weaponCamera.enabled = true;
        overlayAlpha = 0f;
        IsPlaying = false;
        enabled = false;
    }

    void OnDestroy()
    {
        IsPlaying = false;
    }

    // Vẽ lớp phủ toàn màn hình lên trên cùng (kể cả HUD)
    void OnGUI()
    {
        if (overlayAlpha <= 0f || PauseMenu.IsPaused) return;

        GUI.depth = -1000;
        GUI.color = new Color(overlayColor.r, overlayColor.g, overlayColor.b, overlayAlpha);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;
    }
}
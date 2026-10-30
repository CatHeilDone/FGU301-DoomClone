using UnityEngine;

// ===== CÔNG CỤ TEST (không ảnh hưởng gameplay, xóa đi lúc nào cũng được) =====
// Gắn vào ĐÚNG 1 Enemy cần test: vẽ vòng tầm phát hiện và hiện số liệu lên màn hình.
// Bấm T để reset trạng thái aggro, test lại mà không cần Stop/Play.
[RequireComponent(typeof(EnemyAwareness))]
public class EnemyRangeDebug : MonoBehaviour
{
    public KeyCode resetKey = KeyCode.T;

    private EnemyAwareness awareness;
    private PlayerMove player;

    void Start()
    {
        awareness = GetComponent<EnemyAwareness>();
        player = FindObjectOfType<PlayerMove>();
    }

    void Update()
    {
        if (Input.GetKeyDown(resetKey)) awareness.isAggro = false;
    }

    // Tầm phát hiện hiện tại: lớn hơn trong (NoiseRadius của Player, closeSenseRadius của enemy)
    float DetectRadius()
    {
        float r = awareness.closeSenseRadius;
        if (player) r = Mathf.Max(r, player.NoiseRadius);
        return r;
    }

    // Vẽ luôn trong Scene view (không cần chọn enemy); bật nút Gizmos ở Game view để thấy cả ở đó
    void OnDrawGizmos()
    {
        if (!awareness) awareness = GetComponent<EnemyAwareness>();
        if (!awareness) return;

        Gizmos.color = awareness.isAggro ? Color.red : Color.green;   // xanh = chưa thấy, đỏ = đã aggro
        Gizmos.DrawWireSphere(transform.position, DetectRadius());

        if (Application.isPlaying && player)
            Gizmos.DrawLine(transform.position, player.transform.position);   // đường nối enemy - Player
    }

    void OnGUI()
    {
        if (!player || !awareness) return;

        float dist = Vector3.Distance(transform.position, player.transform.position);

        GUI.color = awareness.isAggro ? Color.red : Color.green;
        GUI.Label(new Rect(10, 40, 600, 25),
            "Distance: " + dist.ToString("F1") + " m   |   Detect radius: " + DetectRadius().ToString("F1") +
            " m (Noise " + player.NoiseRadius + ", Close " + awareness.closeSenseRadius + ")");
        GUI.Label(new Rect(10, 62, 600, 25),
            "Aggro: " + (awareness.isAggro ? "YES" : "NO") + "   |   [" + resetKey + "] = reset aggro");
        GUI.color = Color.white;
    }
}
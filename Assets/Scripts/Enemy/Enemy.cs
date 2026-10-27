using UnityEngine;
using UnityEngine.AI;

public class Enemy : MonoBehaviour
{
    private EnemyManager enemyManager;
    public float enemyHealth = 2f;      // MỚI: đổi private -> public để chỉnh máu từng enemy trong Inspector
    private bool isDead;

    public GameObject gunHitEffect;
    public Animator anim;            // kéo model 3D vào (hoặc để trống, code tự tìm)

    // ===== MỚI: Boss =====
    public bool isBoss;                       // tick vào ĐÚNG 1 enemy muốn làm Boss
    public float bossHealthMultiplier = 10f;  // máu Boss = máu thường x 10
    public float bossSize = 2.5f;             // to gấp mấy lần
    public float bossDamageMultiplier = 2f;   // đánh đau gấp mấy lần
    public float bossGrowSpeed = 2f;          // to lên nhanh hay chậm
    private bool bossActivated;               // đã hóa Boss chưa
    private Vector3 bossTargetScale;          // kích thước cuối cùng của Boss
    private EnemyAwareness awareness;

    public bool IsDead => isDead;

    private void Start()
    {
        if (!anim) anim = GetComponentInChildren<Animator>();
        enemyManager = FindObjectOfType<EnemyManager>();
        awareness = GetComponent<EnemyAwareness>();   // MỚI

        // MỚI: Boss có máu cao ngay từ đầu, để không bị bắn chết trước khi kịp hóa to
        if (isBoss) enemyHealth *= bossHealthMultiplier;

        // Ghi lại chỗ đứng ban đầu làm điểm spawn cho các đợt sau
        if (EnemySpawner.Instance) EnemySpawner.Instance.Register(this);
    }

    void Update()
    {
        if (!isDead && enemyHealth <= 0) Die();

        // MỚI: Boss hóa to lần đầu tiên khi phát hiện Player
        if (isBoss && !bossActivated && !isDead && awareness.isAggro) BecomeBoss();

        // MỚI: to dần dần cho đến kích thước Boss
        if (bossActivated && !isDead)
            transform.localScale = Vector3.Lerp(transform.localScale, bossTargetScale, bossGrowSpeed * Time.deltaTime);
    }

    // ===== MỚI: biến enemy này thành Boss =====
    void BecomeBoss()
    {
        bossActivated = true;
        bossTargetScale = transform.localScale * bossSize;

        var ai = GetComponent<EnemyAI>();
        if (ai)
        {
            ai.attackDamage *= bossDamageMultiplier;   // đánh đau hơn
            ai.attackRange *= bossSize;                // thân to hơn nên tầm đánh xa hơn
            // EnemyAI chỉ tính stoppingDistance 1 lần trong Start nên phải cập nhật lại ở đây
            GetComponent<NavMeshAgent>().stoppingDistance = ai.attackRange * 0.8f;
        }
    }

    public void TakeDamage(float damage)
    {
        if (isDead) return;
        Instantiate(gunHitEffect, transform.position, Quaternion.identity);
        enemyHealth -= damage;
        if (ScoreManager.Instance) ScoreManager.Instance.EnemyHit(this, damage);
    }

    void Die()
    {
        isDead = true;
        enemyManager.RemoveEnemy(this);
        if (ScoreManager.Instance) ScoreManager.Instance.EnemyKilled(this);

        // Tắt AI trước, rồi mới tắt agent để tránh lỗi SetDestination trên agent đã tắt
        GetComponent<EnemyAI>().enabled = false;
        GetComponent<EnemyAwareness>().enabled = false;
        GetComponent<NavMeshAgent>().enabled = false;
        GetComponent<Collider>().enabled = false;

        if (anim) anim.SetTrigger("Die");
        Destroy(gameObject, 3f);     // chờ 3 giây cho animation chết diễn xong
    }
}
using UnityEngine;
using UnityEngine.AI;

public class Enemy : MonoBehaviour
{
    private EnemyManager enemyManager;
    private float enemyHealth = 2f;
    private bool isDead;

    public GameObject gunHitEffect;
    public Animator anim;            // kéo model 3D vào (hoặc để trống, code tự tìm)

    public bool IsDead => isDead;

    private void Start()
    {
        if (!anim) anim = GetComponentInChildren<Animator>();
        enemyManager = FindObjectOfType<EnemyManager>();

        // Ghi lại chỗ đứng ban đầu làm điểm spawn cho các đợt sau
        if (EnemySpawner.Instance) EnemySpawner.Instance.Register(this);
    }

    void Update()
    {
        if (!isDead && enemyHealth <= 0) Die();
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
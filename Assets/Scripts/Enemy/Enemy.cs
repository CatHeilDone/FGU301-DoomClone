using UnityEngine;
using UnityEngine.AI;

public class Enemy : MonoBehaviour
{
    private EnemyManager enemyManager;
    private float enemyHealth = 2f;
    private bool isDead;

    public GameObject gunHitEffect;
    public Animator anim; // kéo model 3D vào (hoặc để trống, code tự tìm)

    public bool IsDead => isDead;

    private RoomEnemyGroup roomGroup;

    public void SetRoomGroup(RoomEnemyGroup group)
    {
        roomGroup = group;
    }

    private void Start()
    {
        if (!anim)
            anim = GetComponentInChildren<Animator>();
        enemyManager = FindObjectOfType<EnemyManager>();

        // Ghi lại chỗ đứng ban đầu làm điểm spawn cho các đợt sau
        if (EnemySpawner.Instance)
            EnemySpawner.Instance.Register(this);
    }

    void Update()
    {
        if (!isDead && enemyHealth <= 0)
            Die();
    }

    public void TakeDamage(float damage)
    {
        if (isDead)
            return;
        Instantiate(gunHitEffect, transform.position, Quaternion.identity);
        enemyHealth -= damage;
        if (ScoreManager.Instance)
            ScoreManager.Instance.EnemyHit(this, damage);
    }

    void Die()
    {
        if (isDead)
            return;

        isDead = true;

        var lootDrop = GetComponent<EnemyLootDrop>();

        if (lootDrop != null)
        {
            lootDrop.DropLoot();
        }

        if (roomGroup != null)
        {
            roomGroup.NotifyEnemyDied(this, transform.position);
        }

        if (enemyManager != null)
            enemyManager.RemoveEnemy(this);

        if (ScoreManager.Instance != null)
            ScoreManager.Instance.EnemyKilled(this);

        var ai = GetComponent<EnemyAI>();
        if (ai != null)
            ai.enabled = false;

        var awareness = GetComponent<EnemyAwareness>();
        if (awareness != null)
            awareness.enabled = false;

        var agent = GetComponent<NavMeshAgent>();
        if (agent != null)
            agent.enabled = false;

        var col = GetComponent<Collider>();
        if (col != null)
            col.enabled = false;

        if (anim != null)
            anim.SetTrigger("Die");

        Destroy(gameObject, 3f);
    }

    public void KillForCheat()
    {
        if (isDead)
            return;

        enemyHealth = 0f;
        Die();
    }
}

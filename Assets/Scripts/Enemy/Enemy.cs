using UnityEngine;
using UnityEngine.AI;

public class Enemy : MonoBehaviour
{
    private EnemyManager enemyManager;
    private float enemyHealth = 2f;
    private bool isDead;

    public GameObject gunHitEffect;
    public Animator anim;            // kéo model 3D vào (hoặc để trống, code tự tìm)

    private void Start()
    {
        if (!anim) anim = GetComponentInChildren<Animator>();
        enemyManager = FindObjectOfType<EnemyManager>();
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
    }

    void Die()
    {
        isDead = true;
        enemyManager.RemoveEnemy(this);

        // Tắt AI trước, rồi mới tắt agent để tránh lỗi SetDestination trên agent đã tắt
        GetComponent<EnemyAI>().enabled = false;
        GetComponent<EnemyAwareness>().enabled = false;
        GetComponent<NavMeshAgent>().enabled = false;
        GetComponent<Collider>().enabled = false;

        if (anim) anim.SetTrigger("Die");
        Destroy(gameObject, 3f);     // chờ 3 giây cho animation chết diễn xong
    }
}
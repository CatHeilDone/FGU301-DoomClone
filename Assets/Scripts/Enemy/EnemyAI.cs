using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    public Animator anim;
    public float attackRange = 2f;
    public float attackCooldown = 1.5f;
    public float attackDamage = 10f;
    public float damageDelay = 0.5f;   // độ trễ từ lúc vung tay đến lúc gây sát thương

    private EnemyAwareness enemyAwareness;
    private Transform playersTransform;
    private NavMeshAgent agent;
    private float nextAttackTime;

    private void Start()
    {
        enemyAwareness = GetComponent<EnemyAwareness>();
        playersTransform = FindObjectOfType<PlayerMove>().transform;
        agent = GetComponent<NavMeshAgent>();
        if (!anim) anim = GetComponentInChildren<Animator>();

        agent.stoppingDistance = attackRange * 0.8f;
    }

    private void Update()
    {
        if (enemyAwareness.isAggro)
        {
            float dist = Vector3.Distance(transform.position, playersTransform.position);

            if (dist <= attackRange)
            {
                agent.SetDestination(transform.position);   // đứng lại
                FacePlayer();

                if (Time.time >= nextAttackTime)
                {
                    nextAttackTime = Time.time + attackCooldown;
                    if (anim) anim.SetTrigger("Attack");
                    Invoke(nameof(DealDamage), damageDelay);
                }
            }
            else
            {
                agent.SetDestination(playersTransform.position);
            }
        }
        else
        {
            agent.SetDestination(transform.position);
        }

        if (anim) anim.SetFloat("Speed", agent.velocity.magnitude);
    }

    void FacePlayer()
    {
        Vector3 dir = playersTransform.position - transform.position;
        dir.y = 0;
        if (dir.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.Slerp(transform.rotation,
                                  Quaternion.LookRotation(dir), Time.deltaTime * 8f);
    }

    void DealDamage()
    {
        if (!enabled) return;   // enemy đã chết thì không gây sát thương nữa

        float dist = Vector3.Distance(transform.position, playersTransform.position);
        if (dist > attackRange * 1.2f) return;   // người chơi đã chạy ra xa

        // TODO: trừ máu người chơi ở đây, ví dụ:
        playersTransform.GetComponent<PlayerHealth>().DamagePlayer((int)attackDamage);
        Debug.Log("Enemy đánh trúng người chơi: " + attackDamage);
    }
}
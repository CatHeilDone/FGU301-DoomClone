using UnityEngine;

public class WaterBall : MonoBehaviour
{
    public float speed = 30f;
    public float life = 2f;
    public LayerMask hitMask;            
    public GameObject splashPrefab;      
    [HideInInspector] public float maxDistance = Mathf.Infinity;
    private float traveled;

    void Start() => Destroy(gameObject, life);

    void Update()
    {
        float step = speed * Time.deltaTime;

        bool reachedTarget = traveled + step >= maxDistance;
        if (reachedTarget) step = Mathf.Max(0f, maxDistance - traveled);

        if (step > 0f && Physics.SphereCast(transform.position, 0.15f, transform.forward, out var hit, step,
                               hitMask, QueryTriggerInteraction.Collide))
        {
            Debug.Log("WaterBall trúng: " + hit.collider.name);
            if (splashPrefab) Instantiate(splashPrefab, hit.point, Quaternion.identity);
            Destroy(gameObject);
            return;
        }

        transform.position += transform.forward * step;
        traveled += step;

        if (reachedTarget)
        {
            if (splashPrefab) Instantiate(splashPrefab, transform.position, Quaternion.identity);
            Destroy(gameObject);
        }
    }
}
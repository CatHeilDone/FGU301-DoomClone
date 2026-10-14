using UnityEngine;

public class WaterBall : MonoBehaviour
{
    public float speed = 30f;
    public float life = 2f;
    public LayerMask hitMask;            // chọn cùng mask với raycastLayerMask của Gun
    public GameObject splashPrefab;      // tùy chọn, có thể dùng GunHit

    void Start() => Destroy(gameObject, life);

    void Update()
    {
        float step = speed * Time.deltaTime;

        if (Physics.Raycast(transform.position, transform.forward, out var hit, step,
                            hitMask, QueryTriggerInteraction.Ignore))
        {
            if (splashPrefab) Instantiate(splashPrefab, hit.point, Quaternion.identity);
            Destroy(gameObject);
            return;
        }
        transform.position += transform.forward * step;
    }
}
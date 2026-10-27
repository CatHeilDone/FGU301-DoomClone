using UnityEngine;

public class Gun : MonoBehaviour
{
    private BoxCollider gunTrigger;

    public float range = 20f;
    public float verticalRange = 20f;
    public float fireRate = 1f;
    public float bigDamage = 2f;
    public float smallDamage = 1f;
    public int maxAmmo;
    public int ammo;

    public WeaponVisual weaponVisual;
    public Transform muzzle;
    public GameObject waterBallPrefab;
    private Camera cam;

    public float gunShotRadius = 20f;

    [Header("Ngắm bắn")]
    [Tooltip("Độ dễ tính khi ngắm (mét). Tăng nếu khó bắn trúng, giảm nếu bắn trúng quá dễ.")]
    public float aimTolerance = 0.3f;

    private float nextTimeForFire;
    public EnemyManager enemyManager;
    public LayerMask raycastLayerMask;
    public LayerMask enemyLayerMask;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        gunTrigger = GetComponent<BoxCollider>();
        gunTrigger.size = new Vector3(1, verticalRange, range);
        gunTrigger.center = new Vector3(0, 0, range * 0.5f);

        cam = Camera.main;

        CanvasManager.Instance.UpdateAmmo(ammo);
    }

    // Update is called once per frame
    void Update()
    {
        if (PauseMenu.IsPaused || WakeUpIntro.IsPlaying)
            return;
        if (Input.GetMouseButtonDown(0) && Time.time > nextTimeForFire && ammo > 0)
        {
            Fire();
        }
    }

    void Fire()
    {
        Collider[] enemyColliders;
        enemyColliders = Physics.OverlapSphere(transform.position, gunShotRadius, enemyLayerMask);

        foreach (var enemyCollider in enemyColliders)
        {
            enemyCollider.GetComponent<EnemyAwareness>().isAggro = true;
        }
        GetComponent<AudioSource>().Stop();
        GetComponent<AudioSource>().Play();
        SpawnWaterBall();
        if (weaponVisual)
            weaponVisual.PlayRecoil();
        if (!cam)
            cam = Camera.main;

        // Raycast
        Ray aimRay = new Ray(cam.transform.position, cam.transform.forward);
        float maxDist = range * 1.5f;

        foreach (var enemy in enemyManager.enemiesInTrigger)
        {
            var col = enemy.GetComponent<Collider>();
            if (!col || !col.enabled)
                continue;

            // 1) Tia ngắm có đi qua enemy không? Bắn lên trời / xuống đất thì tia trượt -> không sát thương
            Bounds aimBounds = col.bounds;
            aimBounds.Expand(aimTolerance * 2f);
            if (!aimBounds.IntersectRay(aimRay, out float enter) || enter > maxDist)
                continue;

            // 2) Giữa camera và enemy không có tường chắn
            Vector3 dir = col.bounds.center - aimRay.origin;
            if (
                Physics.Raycast(aimRay.origin, dir, out RaycastHit hit, maxDist, raycastLayerMask)
                && hit.collider.GetComponentInParent<Enemy>() == enemy
            )
            {
                if (hit.distance > range * 0.5f)
                    enemy.TakeDamage(smallDamage);
                else
                    enemy.TakeDamage(bigDamage);
            }
        }
        TryHitBoss(aimRay, maxDist);

        nextTimeForFire = Time.time + fireRate;

        ammo--;
        CanvasManager.Instance.UpdateAmmo(ammo);
    }

    void TryHitBoss(Ray aimRay, float maxDist)
    {
        if (
            !Physics.SphereCast(
                aimRay,
                aimTolerance,
                out RaycastHit hit,
                maxDist,
                raycastLayerMask,
                QueryTriggerInteraction.Ignore
            )
        )
            return;

        var boss = hit.collider.GetComponentInParent<BossHealth>();
        if (!boss)
            return;

        float dmg = hit.distance > range * 0.5f ? smallDamage : bigDamage;
        var weak = hit.collider.GetComponent<WeakPoint>();
        boss.TakeDamage(dmg, weak);
    }

    void SpawnWaterBall()
    {
        if (!waterBallPrefab || !muzzle)
            return;

        // Tìm điểm tâm ngắm đang chỉ vào (tường, enemy...), nếu không có thì lấy điểm xa
        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        Vector3 target = Physics.Raycast(
            ray,
            out var hit,
            range * 1.5f,
            raycastLayerMask,
            QueryTriggerInteraction.Collide
        )
            ? hit.point
            : ray.GetPoint(range * 1.5f);

        Vector3 dir = (target - muzzle.position).normalized;
        var ball = Instantiate(waterBallPrefab, muzzle.position, Quaternion.LookRotation(dir));

        // Viên nước chỉ bay tới điểm đang ngắm rồi biến mất (khớp với chỗ tia sát thương trúng)
        var waterBall = ball.GetComponent<WaterBall>();
        if (waterBall)
            waterBall.maxDistance = Vector3.Distance(muzzle.position, target);
    }

    public void GiveAmmo(int amount, GameObject pickup)
    {
        if (ammo >= maxAmmo)
            return;

        ammo = Mathf.Min(ammo + amount, maxAmmo);
        Destroy(pickup);
        CanvasManager.Instance.UpdateAmmo(ammo);
    }

    // Dùng khi load ván đã lưu
    public void SetAmmo(int value)
    {
        ammo = Mathf.Clamp(value, 0, maxAmmo);
        CanvasManager.Instance.UpdateAmmo(ammo);
    }

    private void OnTriggerEnter(Collider other)
    {
        Enemy enemy = other.transform.GetComponent<Enemy>();

        if (enemy)
        {
            enemyManager.AddEnemy(enemy);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        Enemy enemy = other.transform.GetComponent<Enemy>();

        if (enemy)
        {
            enemyManager.RemoveEnemy(enemy);
        }
    }
}

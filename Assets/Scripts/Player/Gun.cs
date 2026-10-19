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
        foreach (var enemy in enemyManager.enemiesInTrigger)
        {
            var dir = enemy.transform.position - transform.position;
            RaycastHit hit;
            if (Physics.Raycast(transform.position, dir, out hit, range * 1.5f, raycastLayerMask))
            {
                if (hit.transform == enemy.transform)
                {
                    float dist = Vector3.Distance(enemy.transform.position, transform.position);

                    if (dist > range * 0.5f)
                    {
                        enemy.TakeDamage(smallDamage);
                    }
                    else
                    {
                        enemy.TakeDamage(bigDamage);
                    }
                }
            }
        }

        nextTimeForFire = Time.time + fireRate;

        ammo--;
        CanvasManager.Instance.UpdateAmmo(ammo);
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
        Instantiate(waterBallPrefab, muzzle.position, Quaternion.LookRotation(dir));
    }

    public void GiveAmmo(int amount, GameObject pickup)
    {
        if (ammo >= maxAmmo)
            return;

        ammo = Mathf.Min(ammo + amount, maxAmmo);
        Destroy(pickup);
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

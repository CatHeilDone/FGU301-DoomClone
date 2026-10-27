using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Thả enemy theo đợt, đợt sau đông hơn đợt trước.
/// Vị trí spawn lấy từ chỗ đứng ban đầu của các enemy đặt sẵn trong scene,
/// enemy mẫu được nhân bản từ enemy đầu tiên, nên không cần gán prefab.
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    public static EnemySpawner Instance { get; private set; }

    public float firstWaveDelay = 30f;
    public float waveInterval = 25f;
    public float clearedWaveDelay = 4f;     // dọn sạch thì đợt sau tới sớm hơn
    public int baseCount = 2;
    public int extraPerWave = 1;
    public int maxAlive = 15;
    public float minSpawnDistance = 10f;
    public float maxSpawnDistance = 40f;

    public int Wave { get; private set; }
    public event Action<int> WaveStarted;

    readonly List<Vector3> spawnPoints = new List<Vector3>();
    readonly HashSet<Enemy> spawned = new HashSet<Enemy>();
    GameObject templateHolder;
    GameObject template;
    Transform player;
    float nextWaveTime;
    float nextAliveCheck;
    bool waveClearedAwarded = true;

    void Awake()
    {
        Instance = this;
        nextWaveTime = Time.time + firstWaveDelay;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Start()
    {
        var move = FindAnyObjectByType<PlayerMove>();
        if (move) player = move.transform;
    }

    /// <summary>Enemy.Start gọi vào đây để ghi lại vị trí spawn.</summary>
    public void Register(Enemy enemy)
    {
        if (enemy.isBoss) return;
        if (spawned.Contains(enemy)) return;

        spawnPoints.Add(enemy.transform.position);

        if (!template)
        {
            // Nhân bản dưới một parent đang tắt để bản mẫu không chạy Awake/Start
            templateHolder = new GameObject("Enemy Template (spawner)");
            templateHolder.SetActive(false);
            templateHolder.transform.SetParent(transform);
            template = Instantiate(enemy.gameObject, templateHolder.transform);
        }
    }

    void Update()
    {
        var score = ScoreManager.Instance;
        if (score && score.IsGameOver) return;
        if (!template || !player) return;

        if (Time.time >= nextAliveCheck)
        {
            nextAliveCheck = Time.time + 1f;
            spawned.RemoveWhere(e => !e);

            if (Wave > 0 && CountAlive() == 0)
            {
                if (!waveClearedAwarded)
                {
                    waveClearedAwarded = true;
                    if (score) score.WaveCleared(Wave);
                }
                nextWaveTime = Mathf.Min(nextWaveTime, Time.time + clearedWaveDelay);
            }
        }

        if (Time.time >= nextWaveTime)
            StartWave();
    }

    /// <summary>Khi load ván đang dở: đợt tiếp theo nối tiếp số đợt đã lưu và tới sau vài giây.</summary>
    public void RestoreWave(int wave)
    {
        Wave = Mathf.Max(0, wave);
        waveClearedAwarded = true;
        nextWaveTime = Time.time + clearedWaveDelay * 2f;
    }

    void StartWave()
    {
        Wave++;
        nextWaveTime = Time.time + waveInterval;
        waveClearedAwarded = false;

        int count = Mathf.Min(baseCount + extraPerWave * (Wave - 1), maxAlive - CountAlive());
        for (int i = 0; i < count; i++)
            SpawnOne();

        WaveStarted?.Invoke(Wave);
    }

    void SpawnOne()
    {
        if (!TryPickPoint(out Vector3 pos)) return;

        Vector3 look = player.position - pos;
        look.y = 0f;
        Quaternion rot = look.sqrMagnitude > 0.01f ? Quaternion.LookRotation(look) : Quaternion.identity;

        GameObject go = Instantiate(template, pos, rot);
        go.name = "Enemy (wave " + Wave + ")";

        var enemy = go.GetComponent<Enemy>();
        if (enemy) spawned.Add(enemy);

        // Đợt mới đi thẳng tới người chơi
        var awareness = go.GetComponent<EnemyAwareness>();
        if (awareness) awareness.isAggro = true;
    }

    bool TryPickPoint(out Vector3 pos)
    {
        // Ưu tiên điểm không quá gần cũng không quá xa người chơi
        for (int attempt = 0; attempt < 12; attempt++)
        {
            Vector3 p = spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Count)];
            float dist = Vector3.Distance(p, player.position);
            bool inRange = dist >= minSpawnDistance && dist <= maxSpawnDistance;
            if ((inRange || attempt >= 8) && dist >= minSpawnDistance * 0.5f
                && NavMesh.SamplePosition(p, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            {
                pos = hit.position;
                return true;
            }
        }
        pos = default;
        return false;
    }

    int CountAlive()
    {
        int alive = 0;
        foreach (var e in FindObjectsByType<Enemy>())
            if (!e.IsDead && e.gameObject.activeInHierarchy) alive++;
        return alive;
    }
}

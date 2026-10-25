
using System.Collections.Generic;
using UnityEngine;

public class RoomEnemyGroup : MonoBehaviour
{
    [Header("Enemies in this room")]
    [SerializeField] private Enemy[] roomEnemies;

    [Header("Guaranteed reward")]
    [SerializeField] private GameObject blueKeyPrefab;
    [SerializeField] private float dropHeight = 0.5f;

    private readonly HashSet<Enemy> remainingEnemies =
        new HashSet<Enemy>();

    private bool completed;

    private void Start()
    {
        remainingEnemies.Clear();

        foreach (Enemy enemy in roomEnemies)
        {
            if (enemy == null || enemy.IsDead)
                continue;

            remainingEnemies.Add(enemy);
            enemy.SetRoomGroup(this);
        }
    }

    public void NotifyEnemyDied(Enemy enemy, Vector3 deathPosition)
    {
        if (completed)
            return;

        // Chỉ chấp nhận enemy thuộc room này.
        if (!remainingEnemies.Remove(enemy))
            return;

        if (remainingEnemies.Count > 0)
            return;

        completed = true;

        // Quái cuối cùng chắc chắn rơi chìa khóa.
        if (blueKeyPrefab != null)
        {
            Vector3 position =
                deathPosition + Vector3.up * dropHeight;

            Instantiate(
                blueKeyPrefab,
                position,
                Quaternion.identity
            );
        }
        else
        {
            Debug.LogWarning(
                $"[{name}] Chưa gán Blue Key Prefab!"
            );
        }

        Debug.Log($"Room cleared: {name}");
    }
}


using System.Collections.Generic;
using UnityEngine;

public class RoomEnemyGroup : MonoBehaviour
{
    public static RoomEnemyGroup CurrentRoom { get; private set; }

    [Header("Enemies in this room")]
    [SerializeField] private Enemy[] roomEnemies;

    [Header("Guaranteed Reward")]
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

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            CurrentRoom = this;
            Debug.Log("Entered room: " + name);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && CurrentRoom == this)
        {
            CurrentRoom = null;
        }
    }

    private void OnDisable()
    {
        if (CurrentRoom == this)
            CurrentRoom = null;
    }

    public void NotifyEnemyDied(Enemy enemy, Vector3 deathPosition)
    {
        if (completed) return;

        if (!remainingEnemies.Remove(enemy))
            return;

        if (remainingEnemies.Count > 0)
            return;

        completed = true;

        if (blueKeyPrefab != null)
        {
            Instantiate(
                blueKeyPrefab,
                deathPosition + Vector3.up * dropHeight,
                Quaternion.identity
            );
        }
        else
        {
            Debug.LogWarning(
                "[" + name + "] Blue Key Prefab is missing!"
            );
        }

        Debug.Log("ROOM CLEARED: " + name);
    }

    // Cheat: chỉ clear các enemy thuộc phòng này.
    public void CheatClearRoom()
    {
        if (completed)
        {
            Debug.Log("This room is already cleared.");
            return;
        }

        Enemy[] enemies = new Enemy[remainingEnemies.Count];
        remainingEnemies.CopyTo(enemies);

        foreach (Enemy enemy in enemies)
        {
            if (enemy != null && !enemy.IsDead)
            {
                enemy.KillForCheat();
            }
        }

        Debug.Log("CHEAT CLEARROOM executed: " + name);
    }
}

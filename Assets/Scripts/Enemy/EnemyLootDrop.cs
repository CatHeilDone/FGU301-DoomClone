
using UnityEngine;

public class EnemyLootDrop : MonoBehaviour
{
    [System.Serializable]
    public class LootEntry
    {
        public GameObject prefab;

        [Min(0f)]
        public float weight = 1f;
    }

    [Header("Drop Settings")]
    [Range(0f, 100f)]
    public float dropChance = 40f;

    [Tooltip("Độ cao vật phẩm xuất hiện so với vị trí enemy")]
    public float spawnHeight = 0.5f;

    [Tooltip("Độ lệch ngẫu nhiên trên mặt phẳng")]
    public float randomOffset = 0.3f;

    [Header("Loot Table")]
    public LootEntry[] lootTable;

    private bool hasDropped;

    public void DropLoot()
    {
        // Ngăn một enemy rơi đồ nhiều lần
        if (hasDropped) return;
        hasDropped = true;

        if (lootTable == null || lootTable.Length == 0)
            return;

        // Kiểm tra xác suất có rơi vật phẩm hay không
        if (Random.value * 100f >= dropChance)
            return;

        // Tính tổng trọng số của các vật phẩm hợp lệ
        float totalWeight = 0f;

        foreach (LootEntry entry in lootTable)
        {
            if (entry.prefab != null && entry.weight > 0f)
                totalWeight += entry.weight;
        }

        if (totalWeight <= 0f)
            return;

        // Chọn ngẫu nhiên một vật phẩm theo trọng số
        float roll = Random.value * totalWeight;

        foreach (LootEntry entry in lootTable)
        {
            if (entry.prefab == null || entry.weight <= 0f)
                continue;

            roll -= entry.weight;

            if (roll <= 0f)
            {
                SpawnItem(entry.prefab);
                return;
            }
        }
    }

    private void SpawnItem(GameObject prefab)
    {
        Vector2 offset = Random.insideUnitCircle * randomOffset;

        Vector3 spawnPosition = transform.position
            + Vector3.up * spawnHeight
            + new Vector3(offset.x, 0f, offset.y);

        Instantiate(prefab, spawnPosition, Quaternion.identity);
    }
}

using UnityEngine;

public class EnemyAwareness : MonoBehaviour
{
    public float closeSenseRadius = 2f;   // MỚI (thay awarenessRadius): đứng sát enemy thì luôn bị phát hiện, kể cả khi cúi
    public bool isAggro;
    private Transform playersTransform;
    private PlayerMove playerMove;        // MỚI: để đọc NoiseRadius

    private void Start()
    {
        playerMove = FindObjectOfType<PlayerMove>();   // MỚI
        playersTransform = playerMove.transform;
    }

    private void Update()
    {
        var dist = Vector3.Distance(transform.position, playersTransform.position);

        // MỚI: enemy nghe thấy khi Player ồn hơn khoảng cách, hoặc Player đứng quá sát
        if (dist < playerMove.NoiseRadius || dist < closeSenseRadius)
        {
            isAggro = true;
        }
    }
}
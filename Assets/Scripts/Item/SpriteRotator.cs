using UnityEngine;

public class SpriteRotator : MonoBehaviour
{
    private Transform target;

    void Start()
    {
        PlayerMove player = FindObjectOfType<PlayerMove>();

        if (player != null)
        {
            target = player.transform;
        }
    }

    void Update()
    {
        if (target == null)
            return;

        Vector3 direction = target.position - transform.position;
        direction.y = 0;

        transform.rotation = Quaternion.LookRotation(direction);
        transform.Rotate(0, 0, 0);
    }
}
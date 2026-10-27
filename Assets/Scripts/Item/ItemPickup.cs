using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class ItemPickup : MonoBehaviour
{

    public bool isArmor;
    public bool isHealth;
    public bool isAmmo;

    public int amount;

    // ===== MỚI: âm thanh nhặt đồ + thu hút enemy =====
    public AudioClip pickupSound;        // âm thanh khi nhặt
    public float pickupVolume = 1f;
    public float noiseRadius = 12f;      // enemy trong bán kính này (mét) sẽ bị thu hút

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // ===== MỚI: ghi lại số liệu trước khi nhặt, để biết item có thật sự bị nhặt không =====
            var hp = other.GetComponent<PlayerHealth>();
            var gun = other.GetComponentInChildren<Gun>();
            int healthBefore = hp.Health;
            int armorBefore = hp.Armor;
            int ammoBefore = gun.ammo;

            if (isHealth)
            {
                other.GetComponent<PlayerHealth>().GiveHealth(amount, this.gameObject);
            }
            if (isArmor)
            {
                other.GetComponent<PlayerHealth>().GiveArmor(amount, this.gameObject);
            }
            if (isAmmo)
            {
                other.GetComponentInChildren<Gun>().GiveAmmo(amount, this.gameObject);
            }

            // ===== MỚI: máu/giáp/đạn đã đầy thì item không bị nhặt -> không phát tiếng, không thu hút enemy =====
            if (hp.Health != healthBefore || hp.Armor != armorBefore || gun.ammo != ammoBefore)
            {
                PlayPickupNoise(pickupSound, pickupVolume, transform.position, noiseRadius);
            }
        }
    }

    // ===== MỚI: phát âm thanh rồi đánh thức enemy xung quanh =====
    // static để script khác (ví dụ KeyPickup) cũng gọi được mà không cần tạo ItemPickup
    public static void PlayPickupNoise(AudioClip clip, float volume, Vector3 position, float radius)
    {
        // PlayClipAtPoint tạo 1 object âm thanh tạm, nên tiếng vẫn phát dù item đã bị Destroy
        if (clip) AudioSource.PlayClipAtPoint(clip, position, volume);

        // Giống cách Gun.Fire đánh thức enemy bằng tiếng súng
        Collider[] enemyColliders = Physics.OverlapSphere(position, radius, LayerMask.GetMask("Enemy"));
        foreach (var enemyCollider in enemyColliders)
        {
            var awareness = enemyCollider.GetComponent<EnemyAwareness>();
            if (awareness) awareness.isAggro = true;
        }
    }

    // (Tùy chọn) chọn item trong Scene view để thấy vòng tiếng động
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, noiseRadius);
    }
}
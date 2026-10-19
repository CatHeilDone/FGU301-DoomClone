using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerHealth : MonoBehaviour
{
    public int maxHealth;
    private int health;
    public int maxArmor;
    private int armor;
    private bool isDead;

    public bool IsDead => isDead;
    public float HealthPercent => maxHealth > 0 ? (float)health / maxHealth : 0f;
    public int Health => health;
    public int Armor => armor;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        health = maxHealth;
        armor = maxArmor;
        CanvasManager.Instance.UpdateHealth(health);
        CanvasManager.Instance.UpdateArmor(armor); // trước đây gọi nhầm UpdateHealth(armor)
    }

    // Dùng khi load ván đã lưu
    public void SetStats(int newHealth, int newArmor)
    {
        health = Mathf.Clamp(newHealth, 1, maxHealth);
        armor = Mathf.Clamp(newArmor, 0, maxArmor);
        CanvasManager.Instance.UpdateHealth(health);
        CanvasManager.Instance.UpdateArmor(armor);
    }

    public void DamagePlayer(int damage)
    {
        if (damage <= 0 || isDead)
            return;

        if (armor >= damage)
        {
            armor -= damage;
        }
        else
        {
            int remainingDamage = damage - armor;
            armor = 0;
            health -= remainingDamage;
        }

        health = Mathf.Max(health, 0);
        CanvasManager.Instance.UpdateHealth(health);
        CanvasManager.Instance.UpdateArmor(armor);

        if (ScoreManager.Instance) ScoreManager.Instance.PlayerDamaged(damage);

        if (health <= 0)
        {
            isDead = true;

            // Có ScoreManager thì hiện màn hình YOU LOSE, không thì load lại như cũ
            if (ScoreManager.Instance)
                ScoreManager.Instance.TriggerGameOver();
            else
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    public void GiveHealth(int amount, GameObject pickup)
    {
        if (health < maxHealth)
        {
            health += amount;
            Destroy(pickup);
        }

        if (health >= maxHealth)
        {
            health = maxHealth;
        }
        CanvasManager.Instance.UpdateHealth(health);
    }

    public void GiveArmor(int amount, GameObject pickup)
    {
        if (armor < maxArmor)
        {
            armor += amount;
            Destroy(pickup);
        }

        if (armor >= maxArmor)
        {
            armor = maxArmor;
        }
        CanvasManager.Instance.UpdateArmor(armor);
    }
}

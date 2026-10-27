using UnityEngine;

[RequireComponent(typeof(Collider))]
public class WeakPoint : MonoBehaviour
{
    public enum Kind { Eye, Side, Back }

    public Kind kind;
    public float damageMultiplier = 2f;   // hệ số nhân sát thương khi bắn trúng
    public float maxHealth = 20f;         // máu riêng của điểm yếu
    public Color brokenColor = new Color(0.15f, 0.15f, 0.15f);

    [HideInInspector] public BossHealth boss;
    public bool IsBroken { get; private set; }

    float health;
    Collider col;
    Renderer rend;
    Color originalColor;

    void Awake()
    {
        col = GetComponent<Collider>();
        rend = GetComponent<Renderer>();
        health = maxHealth;
        if (rend) originalColor = rend.material.color;
    }

    // Trả về true nếu cú đánh này làm điểm yếu vỡ
    public bool TakeHit(float damage)
    {
        if (IsBroken) return false;

        health -= damage;
        if (health > 0f) return false;

        IsBroken = true;
        col.enabled = false;                       // vỡ rồi thì đạn đi xuyên sang thân boss
        if (rend) rend.material.color = brokenColor;
        return true;
    }

    public void Restore()
    {
        IsBroken = false;
        health = maxHealth;
        col.enabled = true;
        if (rend) rend.material.color = originalColor;
    }
}
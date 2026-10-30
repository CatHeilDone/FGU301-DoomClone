using System;
using System.Collections;
using UnityEngine;

public class BossHealth : MonoBehaviour
{
    [Header("Máu")]
    public float maxHealth = 150f;
    public float currentHealth;

    [Header("Điểm yếu")]
    [Tooltip("Khi một điểm yếu vỡ, boss mất thêm % máu tối đa này")]
    [Range(0f, 0.3f)] public float weakPointBreakBonus = 0.08f;

    [Header("Vỡ băng (khi phá hết 5 điểm yếu)")]
    public float brokenDuration = 8f;
    public float brokenDamageMultiplier = 2f;

    public bool IsDead { get; private set; }
    public bool IsBroken { get; private set; }
    public bool IsPhase2 { get; private set; }

    // Các script khác (BossController, thanh máu...) sẽ lắng nghe những sự kiện này
    public event Action<float, float> OnHealthChanged;   // (máu hiện tại, máu tối đa)
    public event Action<WeakPoint> OnWeakPointHit;
    public event Action<WeakPoint> OnWeakPointBroken;
    public event Action OnBrokenStateStart;
    public event Action OnBrokenStateEnd;
    public event Action OnPhase2;
    public event Action OnDied;

    WeakPoint[] weakPoints;
    int brokenCount;

    void Awake()
    {
        currentHealth = maxHealth;
        weakPoints = GetComponentsInChildren<WeakPoint>();
        foreach (var wp in weakPoints) wp.boss = this;
    }

    public void TakeDamage(float amount, WeakPoint weakPoint = null)
    {
        if (IsDead) return;

        float dmg = amount;
        if (IsBroken) dmg *= brokenDamageMultiplier;

        if (weakPoint != null && !weakPoint.IsBroken)
        {
            dmg *= weakPoint.damageMultiplier;
            OnWeakPointHit?.Invoke(weakPoint);
        }

        Debug.Log($"Boss nhận {dmg:0.0} sát thương" + (weakPoint ? $" vào {weakPoint.name}" : ""));
        ApplyDamage(dmg);
        if (ScoreManager.Instance) ScoreManager.Instance.BossHit(dmg);
        if (IsDead) return;

        if (weakPoint != null && weakPoint.TakeHit(dmg))
        {
            brokenCount++;
            if (ScoreManager.Instance) ScoreManager.Instance.BossWeakPointBroken();
            OnWeakPointBroken?.Invoke(weakPoint);
            ApplyDamage(maxHealth * weakPointBreakBonus);
            if (IsDead) return;

            if (brokenCount >= weakPoints.Length)
                StartCoroutine(BrokenState());
        }
    }

    void ApplyDamage(float dmg)
    {
        currentHealth = Mathf.Max(0f, currentHealth - dmg);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0f)
        {
            IsDead = true;
            StopAllCoroutines();
            Debug.Log("Boss đã bị hạ!");
            OnDied?.Invoke();
        }
        else if (!IsPhase2 && currentHealth <= maxHealth * 0.5f)
        {
            IsPhase2 = true;
            OnPhase2?.Invoke();
        }
    }

    IEnumerator BrokenState()
    {
        IsBroken = true;
        OnBrokenStateStart?.Invoke();

        yield return new WaitForSeconds(brokenDuration);

        // Hết thời gian vỡ băng: các điểm yếu mọc lại để trận đấu tiếp tục
        foreach (var wp in weakPoints) wp.Restore();
        brokenCount = 0;
        IsBroken = false;
        OnBrokenStateEnd?.Invoke();
    }
}
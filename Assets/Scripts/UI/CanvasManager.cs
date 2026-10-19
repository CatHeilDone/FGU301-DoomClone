using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CanvasManager : MonoBehaviour
{
    public TextMeshProUGUI health;
    public TextMeshProUGUI ammo;
    public TextMeshProUGUI armor;

    public Image healthIndicator;

    public Sprite health1; //healthy
    public Sprite health2;
    public Sprite health3;
    public Sprite health4; //dead

    public GameObject blueKey;
    public GameObject orangeKey;
    public GameObject purpleKey;

    public string keyColor;

    [Header("Thanh HUD")]
    public Image healthFill;
    public Image armorFill;
    public Image waterFill;

    [Header("Vệt trễ (tùy chọn)")]
    public Image healthTrail;
    public Image armorTrail;
    public Image waterTrail;

    public float fillSpeed = 6f;
    public float trailSpeed = 0.35f;

    private float healthTarget = 1f, armorTarget = 1f, waterTarget = 1f;
    private PlayerHealth playerHealth;
    private Gun gun;

    private static CanvasManager _instance;
    public static CanvasManager Instance
    {
        get { return _instance; }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            _instance = this;
        }
    }

    private void Update()
    {
        Move(healthFill, healthTarget);
        Move(armorFill, armorTarget);
        Move(waterFill, waterTarget);

        UpdateTrail(healthFill, healthTrail);
        UpdateTrail(armorFill, armorTrail);
        UpdateTrail(waterFill, waterTrail);

        // Máu dưới 25% thì thanh nhấp nháy
        if (healthFill)
        {
            float pulse = healthTarget < 0.25f ? (Mathf.Sin(Time.time * 8f) + 1f) * 0.5f : 0f;
            healthFill.color = Color.Lerp(Color.white, new Color(1f, 0.55f, 0.55f), pulse);
        }
    }

    void Move(Image fill, float target)
    {
        if (fill)
            fill.fillAmount = Mathf.MoveTowards(fill.fillAmount, target, fillSpeed * Time.deltaTime);
    }

    void UpdateTrail(Image fill, Image trail)
    {
        if (!fill || !trail) return;
        if (trail.fillAmount < fill.fillAmount)
            trail.fillAmount = fill.fillAmount;   // khi hồi thì vệt theo ngay
        else
            trail.fillAmount = Mathf.MoveTowards(trail.fillAmount, fill.fillAmount, trailSpeed * Time.deltaTime);
    }

    public void UpdateHealth(int healthValue)
    {
        health.text = healthValue.ToString() + "%";
        if (!playerHealth) playerHealth = FindObjectOfType<PlayerHealth>();
        if (playerHealth && playerHealth.maxHealth > 0)
            healthTarget = Mathf.Clamp01((float)healthValue / playerHealth.maxHealth);
        UpdateHealthIndicator(healthValue);
    }

    public void UpdateArmor(int armorValue)
    {
        armor.text = armorValue.ToString() + "%";
        if (!playerHealth) playerHealth = FindObjectOfType<PlayerHealth>();
        if (playerHealth && playerHealth.maxArmor > 0)
            armorTarget = Mathf.Clamp01((float)armorValue / playerHealth.maxArmor);
    }

    public void UpdateAmmo(int ammoValue)
    {
        ammo.text = ammoValue.ToString();
        if (!gun) gun = FindObjectOfType<Gun>();
        if (gun && gun.maxAmmo > 0)
            waterTarget = Mathf.Clamp01((float)ammoValue / gun.maxAmmo);
    }

    public void UpdateHealthIndicator(int healthValue)
    {
        if (healthValue >= 66)
        {
            healthIndicator.sprite = health1;
        }
        if (healthValue < 66 && healthValue >= 33)
        {
            healthIndicator.sprite = health2;
        }
        if (healthValue < 33 && healthValue > 0)
        {
            healthIndicator.sprite = health3;
        }
        if (healthValue <= 0)
        {
            healthIndicator.sprite = health4;
        }
    }

    public void UpdateKey(string keyColor)
    {
        if (keyColor == "blue")
        {
            blueKey.SetActive(true);
        }
        else if (keyColor == "orange")
        {
            orangeKey.SetActive(true);
        }
        else if (keyColor == "purple")
        {
            purpleKey.SetActive(true);
        }
    }

    public void clearKey()
    {
        blueKey.SetActive(false);
        orangeKey.SetActive(false);
        purpleKey.SetActive(false);
    }
}
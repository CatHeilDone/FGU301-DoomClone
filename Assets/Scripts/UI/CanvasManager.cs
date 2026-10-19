using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;


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

    public TextMeshProUGUI point;
    public int pointCounter = 0;
    public int maxPoints;
    public GameObject winPanel;

    private static CanvasManager _instance;
    public static CanvasManager Instance{
        get 
        {
            return _instance;
        }
    }

    void Start()
    {
        SetPoint();
        maxPoints = FindObjectsByType<Enemy>(FindObjectsInactive.Include).Length;
        SetPoint();
    }

    private void Awake()
    {
        if(_instance !=null && _instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            _instance = this;
        }
    }

    public void UpdateHealth(int healthValue)
    {
        health.text = healthValue.ToString() + "%";
        UpdateHealthIndicator(healthValue);
    }

    public void UpdateArmor(int armorValue)
    {
        armor.text = armorValue.ToString() + "%";
    }

    public void UpdateAmmo(int ammoValue)
    {
        ammo.text = ammoValue.ToString();
    }

    public void UpdateHealthIndicator(int healthValue)
    {
        if(healthValue >= 66)
        {
            healthIndicator.sprite = health1;
        }
        if(healthValue < 66 && healthValue >= 33)
        {
            healthIndicator.sprite = health2;
        }
        if(healthValue < 33 && healthValue >0)
        {
            healthIndicator.sprite = health3;
        }if(healthValue <=0)
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

    public void SetPoint()
    {
        point.text = "Point: " + pointCounter.ToString() + " / " + maxPoints.ToString();
    }

    public void UpdatePoint()
    {
        pointCounter++;
        SetPoint();
        if (pointCounter >= maxPoints)
        {
            WinGame();
        }
    }

    public void SetPointCount(int value)
    {
        pointCounter = value;
        SetPoint();
    }

    void WinGame()
    {
        Debug.Log("You win!");
        PauseMenu.IsPaused = true;
        winPanel.SetActive(true);
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}

using System.IO;
using UnityEngine;

[System.Serializable]
public class SaveData
{
    public int points;
    public int health;
    public int armor;
    public int ammo;
    public bool hasYellow, hasOrange, hasBlue, hasPurple;
    public Vector3 playerPosition;
    public float lookAngle;
}

public class SaveManager : MonoBehaviour
{
    public Transform player;
    public PlayerHealth playerHealth;
    public PlayerInventory playerInventory;
    public Gun gun;
    public MouseLook mouseLook;

    private string path;

    void Awake()
    {
        path = Path.Combine(Application.persistentDataPath, "save.json");
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F5)) Save();
        if (Input.GetKeyDown(KeyCode.F9)) Load();
    }

    public void Save()
    {
        SaveData data = new SaveData();

        data.points = CanvasManager.Instance.pointCounter;
        data.health = playerHealth.health;
        data.armor = playerHealth.armor;
        data.ammo = gun.ammo;

        data.hasYellow = playerInventory.hasYellow;
        data.hasOrange = playerInventory.hasOrange;
        data.hasBlue = playerInventory.hasBlue;
        data.hasPurple = playerInventory.hasPurple;

        data.playerPosition = player.position;
        data.lookAngle = mouseLook.LookingPos;

        File.WriteAllText(path, JsonUtility.ToJson(data, true));
        Debug.Log("Saved to " + path);
    }

    public void Load()
    {
        if (!File.Exists(path))
        {
            Debug.Log("No save file found");
            return;
        }

        SaveData data = JsonUtility.FromJson<SaveData>(File.ReadAllText(path));

        CanvasManager.Instance.SetPointCount(data.points);
        playerHealth.SetStats(data.health, data.armor);
        gun.SetAmmo(data.ammo);

        playerInventory.hasYellow = data.hasYellow;
        playerInventory.hasOrange = data.hasOrange;
        playerInventory.hasBlue = data.hasBlue;
        playerInventory.hasPurple = data.hasPurple;

        // Refresh the key icons on screen
        CanvasManager.Instance.clearKey();
        if (data.hasBlue) CanvasManager.Instance.UpdateKey("blue");
        if (data.hasOrange) CanvasManager.Instance.UpdateKey("orange");
        if (data.hasPurple) CanvasManager.Instance.UpdateKey("purple");

        // The CharacterController would override the new position, so turn it off briefly
        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
        player.position = data.playerPosition;
        if (cc != null) cc.enabled = true;

        mouseLook.SetLookingPos(data.lookAngle);

        Debug.Log("Loaded save");
    }
}
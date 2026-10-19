using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Lưu / load ván đang chơi dở (theo tài khoản): vị trí, máu, giáp, đạn, key, góc nhìn,
/// điểm, style, số đợt. F5 lưu nhanh, F9 load nhanh, hoặc dùng nút trong menu Pause.
/// Khi Main Menu bấm CONTINUE, tự load bản lưu ngay khi vào scene.
/// Gắn trên object "Gameplay Systems" trong DoomScene.
/// </summary>
public class SaveGameController : MonoBehaviour
{
    public bool quickKeys = true;           // F5 / F9
    public TextMeshProUGUI toastText;       // "GAME SAVED" hiện giữa màn hình rồi mờ dần (tùy chọn)
    public float toastTime = 1.6f;

    Transform player;
    PlayerHealth health;
    PlayerInventory inventory;
    MouseLook look;
    Gun gun;
    float toastUntil;

    void Start()
    {
        health = FindAnyObjectByType<PlayerHealth>();
        inventory = FindAnyObjectByType<PlayerInventory>();
        look = FindAnyObjectByType<MouseLook>();
        gun = FindAnyObjectByType<Gun>();
        var move = FindAnyObjectByType<PlayerMove>();
        player = move ? move.transform : health ? health.transform : null;

        if (toastText) toastText.alpha = 0f;

        if (SaveSystem.ContinueRequested)
        {
            SaveSystem.ContinueRequested = false;
            StartCoroutine(LoadAfterStart());
        }
    }

    // Đợi 1 frame để Start của PlayerHealth / Gun đặt giá trị mặc định xong rồi mới ghi đè
    IEnumerator LoadAfterStart()
    {
        yield return null;
        LoadNow();
    }

    void Update()
    {
        if (toastText) toastText.alpha = Mathf.Clamp01((toastUntil - Time.unscaledTime) / 0.4f);

        if (!quickKeys || PauseMenu.IsPaused) return;
        var score = ScoreManager.Instance;
        if (score && score.IsGameOver) return;

        if (Input.GetKeyDown(KeyCode.F5)) SaveNow();
        else if (Input.GetKeyDown(KeyCode.F9)) LoadNow();
    }

    public bool SaveNow()
    {
        if (!player || !health) return false;
        var score = ScoreManager.Instance;
        if (score && score.IsGameOver) return false;

        var run = new SavedRun
        {
            date = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
            scene = SceneManager.GetActiveScene().name,
            score = score ? score.Score : 0,
            kills = score ? score.Kills : 0,
            style = score ? score.Style : 0f,
            timeAlive = score ? score.TimeAlive : 0f,
            highestRank = score ? score.HighestRank : -1,
            wave = EnemySpawner.Instance ? EnemySpawner.Instance.Wave : 0,
            health = health.Health,
            armor = health.Armor,
            ammo = gun ? gun.ammo : 0,
            position = player.position,
            lookAngle = look ? look.LookingPos : 0f,
        };
        if (inventory)
        {
            run.hasYellow = inventory.hasYellow;
            run.hasOrange = inventory.hasOrange;
            run.hasBlue = inventory.hasBlue;
            run.hasPurple = inventory.hasPurple;
        }

        bool ok = SaveSystem.SaveRun(SaveSystem.CurrentUser, run);
        Toast(ok ? "GAME SAVED" : "SAVE FAILED");
        return ok;
    }

    public bool LoadNow()
    {
        var save = SaveSystem.Load(SaveSystem.CurrentUser);
        if (!save.hasSavedRun)
        {
            Toast("NO SAVED GAME");
            return false;
        }

        var run = save.savedRun;
        if (health) health.SetStats(run.health, run.armor);
        if (gun) gun.SetAmmo(run.ammo);

        if (inventory)
        {
            inventory.hasYellow = run.hasYellow;
            inventory.hasOrange = run.hasOrange;
            inventory.hasBlue = run.hasBlue;
            inventory.hasPurple = run.hasPurple;
        }
        var canvas = CanvasManager.Instance;
        if (canvas)
        {
            canvas.clearKey();
            if (run.hasBlue) canvas.UpdateKey("blue");
            if (run.hasOrange) canvas.UpdateKey("orange");
            if (run.hasPurple) canvas.UpdateKey("purple");
        }

        if (player)
        {
            // CharacterController sẽ ghi đè vị trí nếu còn bật, nên tắt tạm
            var cc = player.GetComponent<CharacterController>();
            if (cc) cc.enabled = false;
            player.position = run.position;
            if (cc) cc.enabled = true;
        }
        if (look) look.SetLookingPos(run.lookAngle);

        if (ScoreManager.Instance) ScoreManager.Instance.RestoreRun(run);
        if (EnemySpawner.Instance) EnemySpawner.Instance.RestoreWave(run.wave);

        Toast("GAME LOADED");
        return true;
    }

    void Toast(string text)
    {
        if (!toastText) return;
        toastText.text = text;
        toastUntil = Time.unscaledTime + toastTime;
    }
}

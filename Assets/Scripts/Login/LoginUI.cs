using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Gắn vào LoginManager trong scene Login.
/// Kéo thả các panel, ô nhập, text và toggle vào Inspector.
/// </summary>
public class LoginUI : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] GameObject loginPanel;
    [SerializeField] GameObject signUpPanel;

    [Header("Đăng nhập")]
    [SerializeField] TMP_InputField loginUser;
    [SerializeField] TMP_InputField loginPass;
    [SerializeField] TMP_Text loginMessage;
    [SerializeField] Toggle rememberToggle;

    [Header("Đăng ký")]
    [SerializeField] TMP_InputField signUpUser;
    [SerializeField] TMP_InputField signUpPass;
    [SerializeField] TMP_InputField signUpConfirm;
    [SerializeField] TMP_Text signUpMessage;

    [Header("Scene")]
    [SerializeField] string mainMenuScene = "MainMenu";

    [Header("Màu thông báo")]
    [SerializeField] Color infoColor = new Color32(0x9A, 0x9D, 0xA2, 0xFF);
    [SerializeField] Color errorColor = new Color32(0xE0, 0x66, 0x66, 0xFF);

    bool busy;      // đang chờ Firebase trả lời: bỏ qua các lần bấm thêm

    void Start()
    {
        // Game FPS thường khóa chuột, ở màn hình login cần mở lại.
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 1f;

        // Bộ gõ tiếng Việt (Unikey, EVKey...) gửi thêm phím xoá lùi và ký tự ghép dấu,
        // ô nhập của Unity hiểu sai nên mật khẩu bị thêm ký tự thừa. Tắt IME ở màn hình này
        // và chỉ nhận ký tự ASCII in được cho tên đăng nhập và mật khẩu.
        Input.imeCompositionMode = IMECompositionMode.Off;
        foreach (var field in new[] { loginUser, loginPass, signUpUser, signUpPass, signUpConfirm })
            if (field) field.onValidateInput = AsciiOnly;

        // Điền sẵn tên tài khoản đăng nhập lần trước (nếu có).
        loginUser.text = PlayerPrefs.GetString("last_user", "");

        ShowLogin();

        // Đã bật "Remember me" ở lần trước thì tự đăng nhập rồi vào thẳng menu.
        busy = true;
        Say(loginMessage, "Signing in...", false);
        AuthService.TryAutoLogin((ok, message) =>
        {
            if (this == null) return;
            if (ok) { SceneFader.LoadScene(mainMenuScene); return; }   // giữ busy để không bấm được trong lúc chuyển cảnh
            busy = false;
            Say(loginMessage, message ?? "", !string.IsNullOrEmpty(message));
        });
    }

    void OnDestroy()
    {
        Input.imeCompositionMode = IMECompositionMode.Auto;
    }

    static char AsciiOnly(string text, int index, char c)
    {
        return c >= ' ' && c <= '~' ? c : '\0';   // '\0' = bỏ ký tự này
    }

    // ---- Chuyển panel ----
    public void ShowLogin()
    {
        if (busy) return;
        loginPanel.SetActive(true);
        signUpPanel.SetActive(false);
        loginMessage.text = "";
    }

    public void ShowSignUp()
    {
        if (busy) return;
        loginPanel.SetActive(false);
        signUpPanel.SetActive(true);
        signUpMessage.text = "";
    }

    // ---- Nút bấm ----
    public void OnLoginClicked()
    {
        if (busy) return;
        busy = true;
        Say(loginMessage, "Signing in...", false);

        AuthService.Login(loginUser.text, loginPass.text, (ok, message) =>
        {
            if (this == null) return;   // scene đã đổi trong lúc chờ
            if (ok)
            {
                AuthService.SetRememberMe(rememberToggle != null && rememberToggle.isOn);
                if (!string.IsNullOrEmpty(message)) Debug.LogWarning("[Login] " + message);
                SceneFader.LoadScene(mainMenuScene);   // busy vẫn bật cho tới khi rời scene
            }
            else
            {
                busy = false;
                Say(loginMessage, message, true);
            }
        });
    }

    public void OnSignUpClicked()
    {
        if (busy) return;
        busy = true;
        Say(signUpMessage, "Creating account...", false);

        AuthService.Register(signUpUser.text, signUpPass.text, signUpConfirm.text, (ok, error) =>
        {
            if (this == null) return;
            busy = false;
            if (ok)
            {
                // Đăng ký xong thì quay về đăng nhập, điền sẵn tên tài khoản.
                loginUser.text = signUpUser.text;
                loginPass.text = "";
                ShowLogin();
                Say(loginMessage, "Account created. You can log in now.", false);
            }
            else
            {
                Say(signUpMessage, error, true);
            }
        });
    }

    void Say(TMP_Text target, string text, bool error)
    {
        if (!target) return;
        target.text = text;
        target.color = error ? errorColor : infoColor;
    }
}
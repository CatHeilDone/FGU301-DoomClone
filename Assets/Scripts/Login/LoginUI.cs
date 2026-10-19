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

    void Start()
    {
        // Đã bật "Ghi nhớ đăng nhập" ở lần trước thì vào thẳng menu.
        if (AuthService.TryAutoLogin())
        {
            SceneManager.LoadScene(mainMenuScene);
            return;
        }

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
        loginPanel.SetActive(true);
        signUpPanel.SetActive(false);
        loginMessage.text = "";
    }

    public void ShowSignUp()
    {
        loginPanel.SetActive(false);
        signUpPanel.SetActive(true);
        signUpMessage.text = "";
    }

    // ---- Nút bấm ----
    public void OnLoginClicked()
    {
        if (AuthService.Login(loginUser.text, loginPass.text, out string error))
        {
            AuthService.SetRememberMe(rememberToggle != null && rememberToggle.isOn);
            SceneManager.LoadScene(mainMenuScene);
        }
        else
        {
            loginMessage.text = error;
        }
    }

    public void OnSignUpClicked()
    {
        if (AuthService.Register(signUpUser.text, signUpPass.text, signUpConfirm.text, out string error))
        {
            // Đăng ký xong thì quay về đăng nhập, điền sẵn tên tài khoản.
            loginUser.text = signUpUser.text;
            loginPass.text = "";
            ShowLogin();
            loginMessage.text = "Account created. You can log in now.";
        }
        else
        {
            signUpMessage.text = error;
        }
    }
}
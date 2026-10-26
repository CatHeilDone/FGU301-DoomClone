using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Đăng ký / đăng nhập bằng Firebase Authentication (email + mật khẩu).
/// Người chơi vẫn gõ tên đăng nhập như cũ; tên được đổi thành email
/// "&lt;tên&gt;@&lt;usernameDomain&gt;" trước khi gửi lên Firebase. Gõ thẳng một email cũng được.
/// Sau khi đăng nhập, save trên cloud được tải về (CloudSaveSync) rồi mới báo thành công.
/// </summary>
public static class AuthService
{
    const string LastUserKey = "last_user";
    const string RememberUserKey = "remember_user";
    const string RefreshTokenKey = "fb_refresh_token";

    /// <summary>Tên đăng nhập (chữ thường), dùng làm tên file save trên máy.</summary>
    public static string CurrentUser { get; private set; }
    public static bool IsLoggedIn => !string.IsNullOrEmpty(CurrentUser);

    /// <summary>Có phiên Firebase hợp lệ (uid + token) để gọi Firestore.</summary>
    public static bool IsOnline => IsLoggedIn && !string.IsNullOrEmpty(Uid) && !string.IsNullOrEmpty(refreshToken);

    public static string Uid { get; private set; }
    public static string IdToken { get; private set; }
    static string refreshToken;
    static DateTime tokenExpiresUtc;

    // ---------- Đăng ký ----------

    /// <summary>done(ok, error). Đăng ký xong không tự đăng nhập, giống bản cũ.</summary>
    public static void Register(string username, string password, string confirm, Action<bool, string> done)
    {
        username = Normalize(username);
        string error = ValidateUsername(username);
        if (error == null && password.Length < 6) error = "Password must be at least 6 characters.";
        if (error == null && password != confirm) error = "Passwords do not match.";
        if (error != null) { done(false, error); return; }

        FirebaseRunner.Run(FirebaseAuthClient.SignUp(ToEmail(username), password,
            (session, err) => done(session != null, err)));
    }

    // ---------- Đăng nhập ----------

    /// <summary>done(ok, message): khi ok, message có thể là cảnh báo (vd. không đồng bộ được cloud).</summary>
    public static void Login(string username, string password, Action<bool, string> done)
    {
        username = Normalize(username);
        if (username.Length == 0 || password.Length == 0) { done(false, "Enter your username and password."); return; }

        FirebaseRunner.Run(LoginRoutine(username, password, done));
    }

    static IEnumerator LoginRoutine(string username, string password, Action<bool, string> done)
    {
        FirebaseAuthClient.Session session = null; string error = null;
        yield return FirebaseAuthClient.SignIn(ToEmail(username), password, (s, e) => { session = s; error = e; });
        if (session == null) { done(false, error); yield break; }

        StartSession(username, session);
        PlayerPrefs.SetString(LastUserKey, username);
        PlayerPrefs.Save();

        string warning = null;
        yield return CloudSaveSync.PullOnLogin(username, w => warning = w);
        done(true, warning);
    }

    /// <summary>
    /// Gọi ngay sau khi đăng nhập thành công.
    /// Bật: lưu refresh token để lần sau tự đăng nhập. Tắt: xoá.
    /// </summary>
    public static void SetRememberMe(bool remember)
    {
        if (remember && IsOnline)
        {
            PlayerPrefs.SetString(RememberUserKey, CurrentUser);
            PlayerPrefs.SetString(RefreshTokenKey, refreshToken);
        }
        else
        {
            PlayerPrefs.DeleteKey(RememberUserKey);
            PlayerPrefs.DeleteKey(RefreshTokenKey);
        }
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Tự đăng nhập bằng phiên đã ghi nhớ. done(ok, message):
    /// message có nội dung khi phiên đã hỏng (đã bị xoá) hoặc mất mạng.
    /// </summary>
    public static void TryAutoLogin(Action<bool, string> done)
    {
        if (IsLoggedIn) { done(true, null); return; }

        string user = PlayerPrefs.GetString(RememberUserKey, "");
        string token = PlayerPrefs.GetString(RefreshTokenKey, "");
        if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(token)) { done(false, null); return; }

        FirebaseRunner.Run(AutoLoginRoutine(user, token, done));
    }

    static IEnumerator AutoLoginRoutine(string user, string token, Action<bool, string> done)
    {
        FirebaseAuthClient.Session session = null; string error = null; bool temporary = false;
        yield return FirebaseAuthClient.Refresh(token, (s, e, t) => { session = s; error = e; temporary = t; });
        if (session == null)
        {
            // Mất mạng: giữ phiên để lần mở game sau thử lại.
            // Phiên hỏng (đổi mật khẩu, tài khoản bị xoá / khoá): xoá để không thử mãi.
            if (!temporary) ForgetRememberedSession();
            done(false, temporary ? error : "Your session has expired. Please log in again.");
            yield break;
        }

        StartSession(user, session);
        PlayerPrefs.SetString(RefreshTokenKey, refreshToken);   // Firebase có thể cấp refresh token mới
        PlayerPrefs.Save();

        yield return CloudSaveSync.PullOnLogin(user, w => { });
        done(true, null);
    }

    public static void Logout()
    {
        CurrentUser = null;
        Uid = null;
        IdToken = null;
        refreshToken = null;

        // Xoá ghi nhớ, nếu không màn hình Login sẽ tự đăng nhập lại ngay
        ForgetRememberedSession();
    }

    static void ForgetRememberedSession()
    {
        PlayerPrefs.DeleteKey(RememberUserKey);
        PlayerPrefs.DeleteKey(RefreshTokenKey);
        PlayerPrefs.Save();
    }

    // ---------- Token ----------

    /// <summary>Id token chỉ sống 1 giờ: làm mới nếu sắp hết hạn. done(ok).</summary>
    public static IEnumerator EnsureFreshToken(Action<bool> done)
    {
        if (!IsOnline) { done(false); yield break; }
        if (DateTime.UtcNow < tokenExpiresUtc.AddMinutes(-5)) { done(true); yield break; }

        FirebaseAuthClient.Session session = null; bool temporary = false;
        yield return FirebaseAuthClient.Refresh(refreshToken, (s, e, t) => { session = s; temporary = t; });
        if (session == null)
        {
            if (!temporary)
            {
                // Phiên hỏng giữa chừng: ngừng đồng bộ (save vẫn ghi trên máy, cờ pendingUpload giữ lại)
                Debug.LogWarning("[Auth] Phiên Firebase không còn hợp lệ, tạm ngừng đồng bộ cloud.");
                IdToken = null;
                refreshToken = null;
                ForgetRememberedSession();
            }
            done(false);
            yield break;
        }

        StartSession(CurrentUser, session);
        done(true);
    }

    static void StartSession(string username, FirebaseAuthClient.Session s)
    {
        CurrentUser = username;
        Uid = s.localId;
        IdToken = s.idToken;
        refreshToken = s.refreshToken;
        int seconds = int.TryParse(s.expiresIn, out var v) ? v : 3600;
        tokenExpiresUtc = DateTime.UtcNow.AddSeconds(seconds);
    }

    // ---------- Tên đăng nhập ----------

    static string Normalize(string username) => (username ?? "").Trim().ToLowerInvariant();

    /// <summary>"rin" → "rin@players.watergunfight.app"; email gõ thẳng thì giữ nguyên.</summary>
    static string ToEmail(string username) =>
        username.Contains("@") ? username : username + "@" + FirebaseConfig.Get().usernameDomain;

    static string ValidateUsername(string username)
    {
        if (username.Contains("@")) return null;   // email: để Firebase tự kiểm tra
        if (username.Length < 3) return "Username must be at least 3 characters.";
        if (username.Length > 20) return "Username can be at most 20 characters.";
        foreach (char c in username)
            if (!(c >= 'a' && c <= 'z') && !(c >= '0' && c <= '9') && c != '.' && c != '_' && c != '-')
                return "Username can only use letters, numbers, dot, dash and underscore.";
        return null;
    }
}

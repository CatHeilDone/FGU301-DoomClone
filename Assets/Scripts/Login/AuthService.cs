using System;
using System.Security.Cryptography;
using UnityEngine;

/// <summary>
/// Đăng ký / đăng nhập lưu cục bộ bằng PlayerPrefs.
/// Mật khẩu được băm (PBKDF2 + salt), không lưu dạng chữ thường.
/// Lưu ý: chỉ phù hợp game offline / prototype. Muốn tài khoản online
/// hãy dùng Unity Authentication, Firebase hoặc PlayFab.
/// </summary>
public static class AuthService
{
    const string AccountPrefix = "account_";
    const string LastUserKey = "last_user";
    const string RememberKey = "remember_user";
    const int HashIterations = 10000;

    public static string CurrentUser { get; private set; }
    public static bool IsLoggedIn => !string.IsNullOrEmpty(CurrentUser);

    public static bool Register(string username, string password, string confirm, out string error)
    {
        username = Normalize(username);

        if (username.Length < 3)
        {
            error = "Tên đăng nhập phải có ít nhất 3 ký tự.";
            return false;
        }
        if (password.Length < 6)
        {
            error = "Mật khẩu phải có ít nhất 6 ký tự.";
            return false;
        }
        if (password != confirm)
        {
            error = "Mật khẩu nhập lại không khớp.";
            return false;
        }
        if (PlayerPrefs.HasKey(AccountPrefix + username))
        {
            error = "Tên đăng nhập đã tồn tại.";
            return false;
        }

        string salt = GenerateSalt();
        string hash = Hash(password, salt);
        PlayerPrefs.SetString(AccountPrefix + username, salt + ":" + hash);
        PlayerPrefs.Save();

        error = null;
        return true;
    }

    public static bool Login(string username, string password, out string error)
    {
        username = Normalize(username);
        string key = AccountPrefix + username;

        if (!PlayerPrefs.HasKey(key))
        {
            error = "Tài khoản không tồn tại.";
            return false;
        }

        string[] parts = PlayerPrefs.GetString(key).Split(':');
        if (parts.Length != 2 || Hash(password, parts[0]) != parts[1])
        {
            error = "Sai mật khẩu.";
            return false;
        }

        CurrentUser = username;
        PlayerPrefs.SetString(LastUserKey, username);
        PlayerPrefs.Save();

        error = null;
        return true;
    }

    /// <summary>
    /// Gọi ngay sau khi đăng nhập thành công.
    /// Bật: nhớ tài khoản cho lần mở game sau. Tắt: xóa ghi nhớ.
    /// </summary>
    public static void SetRememberMe(bool remember)
    {
        if (remember && IsLoggedIn)
            PlayerPrefs.SetString(RememberKey, CurrentUser);
        else
            PlayerPrefs.DeleteKey(RememberKey);

        PlayerPrefs.Save();
    }

    /// <summary>
    /// Tự đăng nhập bằng tài khoản đã được ghi nhớ. Trả về true nếu thành công.
    /// </summary>
    public static bool TryAutoLogin()
    {
        if (IsLoggedIn) return true;

        string saved = PlayerPrefs.GetString(RememberKey, "");
        if (string.IsNullOrEmpty(saved) || !PlayerPrefs.HasKey(AccountPrefix + saved))
            return false;

        CurrentUser = saved;
        return true;
    }

    public static void Logout()
    {
        CurrentUser = null;

        // Xóa ghi nhớ, nếu không màn hình Login sẽ tự đăng nhập lại ngay.
        PlayerPrefs.DeleteKey(RememberKey);
        PlayerPrefs.Save();
    }

    static string Normalize(string username)
    {
        return (username ?? "").Trim().ToLowerInvariant();
    }

    static string GenerateSalt()
    {
        byte[] bytes = new byte[16];
        using (var rng = RandomNumberGenerator.Create())
            rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }

    static string Hash(string password, string salt)
    {
        byte[] saltBytes = Convert.FromBase64String(salt);
        using (var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, HashIterations))
            return Convert.ToBase64String(pbkdf2.GetBytes(32));
    }
}
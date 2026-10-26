using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Gọi Firebase Authentication qua REST API (không cần cài Firebase SDK).
/// https://firebase.google.com/docs/reference/rest/auth
/// </summary>
public static class FirebaseAuthClient
{
    [Serializable]
    public class Session
    {
        public string idToken;
        public string refreshToken;
        public string localId;      // uid của tài khoản
        public string expiresIn;    // giây, dạng chuỗi
    }

    [Serializable] class SignRequest { public string email; public string password; public bool returnSecureToken = true; }
    [Serializable] class RefreshResponse { public string id_token; public string refresh_token; public string user_id; public string expires_in; }
    [Serializable] class ErrorWrap { public ErrorBody error; }
    [Serializable] class ErrorBody { public int code; public string message; public string status; }

    const string AuthUrl = "https://identitytoolkit.googleapis.com/v1/accounts:";
    const string TokenUrl = "https://securetoken.googleapis.com/v1/token?key=";

    public static IEnumerator SignUp(string email, string password, Action<Session, string> done)
    {
        yield return PostJson(AuthUrl + "signUp?key=" + Config.apiKey,
            JsonUtility.ToJson(new SignRequest { email = email, password = password }), done);
    }

    public static IEnumerator SignIn(string email, string password, Action<Session, string> done)
    {
        yield return PostJson(AuthUrl + "signInWithPassword?key=" + Config.apiKey,
            JsonUtility.ToJson(new SignRequest { email = email, password = password }), done);
    }

    /// <summary>
    /// Đổi refresh token lấy id token mới (id token chỉ sống 1 giờ).
    /// done(session, error, temporary): temporary = true khi lỗi do mạng / server (nên thử lại sau),
    /// false khi phiên không còn hợp lệ (đổi mật khẩu, tài khoản bị xoá / khoá).
    /// </summary>
    public static IEnumerator Refresh(string refreshToken, Action<Session, string, bool> done)
    {
        if (!Config.IsValid)
        {
            done(null, "Firebase is not set up yet (missing API key).", true);
            yield break;
        }

        var form = "grant_type=refresh_token&refresh_token=" + UnityWebRequest.EscapeURL(refreshToken);
        using (var req = new UnityWebRequest(TokenUrl + Config.apiKey, "POST"))
        {
            req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(form));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/x-www-form-urlencoded");
            req.timeout = Mathf.CeilToInt(Config.requestTimeout);
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                done(null, ErrorText(req), IsTemporary(req));
                yield break;
            }
            var r = JsonUtility.FromJson<RefreshResponse>(req.downloadHandler.text);
            done(new Session { idToken = r.id_token, refreshToken = r.refresh_token, localId = r.user_id, expiresIn = r.expires_in }, null, false);
        }
    }

    static IEnumerator PostJson(string url, string json, Action<Session, string> done)
    {
        if (!Config.IsValid)
        {
            done(null, "Firebase is not set up yet (missing API key).");
            yield break;
        }

        using (var req = new UnityWebRequest(url, "POST"))
        {
            req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.timeout = Mathf.CeilToInt(Config.requestTimeout);
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                done(null, ErrorText(req));
                yield break;
            }
            done(JsonUtility.FromJson<Session>(req.downloadHandler.text), null);
        }
    }

    static FirebaseConfig Config => FirebaseConfig.Get();

    /// <summary>Lỗi do mạng hoặc server (thử lại sau được), không phải do tài khoản / dữ liệu.</summary>
    public static bool IsTemporary(UnityWebRequest req) =>
        req.result != UnityWebRequest.Result.ProtocolError || req.responseCode >= 500 || req.responseCode == 429;

    /// <summary>Đổi mã lỗi của Firebase thành câu thông báo cho người chơi.</summary>
    public static string ErrorText(UnityWebRequest req)
    {
        if (req.result == UnityWebRequest.Result.ConnectionError)
            return "Can't reach the server. Check your internet connection.";

        string code = "", status = "";
        try
        {
            var body = JsonUtility.FromJson<ErrorWrap>(req.downloadHandler.text)?.error;
            code = body?.message ?? "";
            status = body?.status ?? "";
        }
        catch { }
        Debug.LogWarning("[Firebase] HTTP " + req.responseCode + " " + status + " — " + code);

        // Lỗi của Firestore: message là câu mô tả, mã nằm ở status
        switch (status)
        {
            case "PERMISSION_DENIED": return "Cloud save was refused. Check the Firestore Rules.";
            case "UNAUTHENTICATED": return "Your session has expired. Please log in again.";
            case "RESOURCE_EXHAUSTED": return "Firebase free quota is used up for today. Try again later.";
            case "NOT_FOUND":
                if (req.url.Contains("firestore")) return "Firestore database not found. Check the projectId.";
                break;
        }

        // Một số mã có phần giải thích phía sau: "WEAK_PASSWORD : Password should be..."
        int cut = code.IndexOf(' ');
        string key = cut > 0 ? code.Substring(0, cut) : code;

        switch (key)
        {
            case "EMAIL_EXISTS": return "That username is already taken.";
            case "EMAIL_NOT_FOUND":
            case "INVALID_PASSWORD":
            case "INVALID_LOGIN_CREDENTIALS": return "Wrong username or password.";
            case "USER_DISABLED": return "This account has been disabled.";
            case "TOO_MANY_ATTEMPTS_TRY_LATER": return "Too many attempts. Try again in a few minutes.";
            case "WEAK_PASSWORD": return "Password must be at least 6 characters.";
            case "INVALID_EMAIL": return "Username can only use letters, numbers, dot, dash and underscore.";
            case "OPERATION_NOT_ALLOWED": return "Email/password sign-in is not enabled in Firebase.";
            case "TOKEN_EXPIRED":
            case "INVALID_REFRESH_TOKEN":
            case "USER_NOT_FOUND": return "Your session has expired. Please log in again.";
        }
        if (code.StartsWith("API key not valid")) return "Firebase is not set up correctly (invalid API key).";
        return "Something went wrong (" + (string.IsNullOrEmpty(key) ? req.responseCode.ToString() : key) + "). Please try again.";
    }
}

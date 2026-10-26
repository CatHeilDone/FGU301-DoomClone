using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Đọc / ghi document users/{uid} trên Cloud Firestore qua REST API.
/// Cả PlayerSave được lưu thành một chuỗi JSON trong field "data";
/// vài field số (bestScore, gamesPlayed...) để riêng cho bảng xếp hạng sau này.
/// </summary>
public static class FirestoreClient
{
    [Serializable] class StrV { public string stringValue; }
    [Serializable] class IntV { public string integerValue; }
    [Serializable] class TimeV { public string timestampValue; }

    [Serializable]
    class UserFields
    {
        public StrV username;
        public StrV data;
        public IntV bestScore;
        public IntV gamesPlayed;
        public IntV totalKills;
        public TimeV updatedAt;
    }

    [Serializable] class UserDoc { public UserFields fields; }

    static string DocUrl(string uid)
    {
        var cfg = FirebaseConfig.Get();
        return "https://firestore.googleapis.com/v1/projects/" + cfg.projectId +
               "/databases/(default)/documents/users/" + UnityWebRequest.EscapeURL(uid);
    }

    /// <summary>done(save, found, error): found = false khi tài khoản chưa có dữ liệu trên cloud.</summary>
    public static IEnumerator LoadSave(string uid, string idToken, Action<PlayerSave, bool, string> done)
    {
        using (var req = UnityWebRequest.Get(DocUrl(uid)))
        {
            req.SetRequestHeader("Authorization", "Bearer " + idToken);
            req.timeout = Mathf.CeilToInt(FirebaseConfig.Get().requestTimeout);
            yield return req.SendWebRequest();

            if (req.responseCode == 404)
            {
                done(null, false, null);
                yield break;
            }
            if (req.result != UnityWebRequest.Result.Success)
            {
                done(null, false, FirebaseAuthClient.ErrorText(req));
                yield break;
            }

            PlayerSave save = null;
            try
            {
                var doc = JsonUtility.FromJson<UserDoc>(req.downloadHandler.text);
                string json = doc?.fields?.data?.stringValue;
                if (!string.IsNullOrEmpty(json)) save = JsonUtility.FromJson<PlayerSave>(json);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Firestore] Dữ liệu trên cloud lỗi: " + e.Message);
            }
            done(save, save != null, null);
        }
    }

    /// <summary>Ghi đè toàn bộ document của tài khoản. done(error) — null là thành công.</summary>
    public static IEnumerator WriteSave(string uid, string idToken, PlayerSave save, Action<string> done)
    {
        var doc = new UserDoc
        {
            fields = new UserFields
            {
                username = new StrV { stringValue = save.user },
                data = new StrV { stringValue = JsonUtility.ToJson(save) },
                bestScore = new IntV { integerValue = save.bestScore.ToString() },
                gamesPlayed = new IntV { integerValue = save.gamesPlayed.ToString() },
                totalKills = new IntV { integerValue = save.totalKills.ToString() },
                updatedAt = new TimeV { timestampValue = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ") },
            }
        };

        using (var req = new UnityWebRequest(DocUrl(uid), "PATCH"))
        {
            req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonUtility.ToJson(doc)));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("Authorization", "Bearer " + idToken);
            req.timeout = Mathf.CeilToInt(FirebaseConfig.Get().requestTimeout);
            yield return req.SendWebRequest();

            done(req.result == UnityWebRequest.Result.Success ? null : FirebaseAuthClient.ErrorText(req));
        }
    }
}

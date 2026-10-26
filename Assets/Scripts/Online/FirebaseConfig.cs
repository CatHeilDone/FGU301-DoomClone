using System;
using UnityEngine;

/// <summary>
/// Cấu hình Firebase, đọc từ Assets/Resources/FirebaseConfig.json.
/// apiKey là "Web API key" của project Firebase (không phải bí mật — quyền truy cập
/// được khoá bằng Firestore Security Rules), projectId là ID của project.
/// </summary>
[Serializable]
public class FirebaseConfig
{
    public string apiKey;
    public string projectId;
    // Firebase đăng nhập bằng email; tên đăng nhập "rin" được đổi thành "rin@<usernameDomain>"
    public string usernameDomain = "players.watergunfight.app";
    public float requestTimeout = 15f;

    static FirebaseConfig cached;

    public bool IsValid => !string.IsNullOrEmpty(apiKey) && !string.IsNullOrEmpty(projectId);

    public static FirebaseConfig Get()
    {
        if (cached != null) return cached;

        var asset = Resources.Load<TextAsset>("FirebaseConfig");
        if (asset)
        {
            try { cached = JsonUtility.FromJson<FirebaseConfig>(asset.text); }
            catch (Exception e) { Debug.LogError("[Firebase] FirebaseConfig.json lỗi: " + e.Message); }
        }
        if (cached == null) cached = new FirebaseConfig();
        if (!cached.IsValid)
            Debug.LogWarning("[Firebase] Chưa điền apiKey / projectId trong Assets/Resources/FirebaseConfig.json");
        return cached;
    }
}

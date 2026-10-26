using System.Collections;
using UnityEngine;

/// <summary>
/// Object ẩn, sống qua mọi scene, để chạy coroutine gọi mạng từ code static
/// (AuthService, CloudSaveSync). Tự tạo khi cần, không phải đặt vào scene.
/// </summary>
public class FirebaseRunner : MonoBehaviour
{
    static FirebaseRunner instance;

    public static Coroutine Run(IEnumerator routine)
    {
        if (!instance)
        {
            var go = new GameObject("[Firebase]");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<FirebaseRunner>();
        }
        return instance.StartCoroutine(routine);
    }
}

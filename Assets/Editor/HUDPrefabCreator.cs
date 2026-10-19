using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Tools > DoomClone > Create HUD Prefabs: tạo StyleHUD.prefab và GameOverScreen.prefab
/// trong Assets/Resources/UI từ bố cục mặc định của HUDBuilder.
/// Sau khi tạo, mở prefab ra chỉnh tay như bình thường.
/// </summary>
public static class HUDPrefabCreator
{
    const string Folder = "Assets/Resources/UI";

    [MenuItem("Tools/DoomClone/Create HUD Prefabs")]
    static void CreateAll()
    {
        Directory.CreateDirectory(Folder);

        int created = 0;
        created += Save(HUDBuilder.BuildStyleHUD(), GameplayBootstrap.StyleHUDPath) ? 1 : 0;
        created += Save(HUDBuilder.BuildGameOverScreen(), GameplayBootstrap.GameOverPath) ? 1 : 0;

        AssetDatabase.Refresh();
        Debug.Log("[HUD] Đã tạo " + created + " prefab trong " + Folder);
    }

    static bool Save(GameObject go, string resourcePath)
    {
        string path = "Assets/Resources/" + resourcePath + ".prefab";
        try
        {
            if (File.Exists(path) && !EditorUtility.DisplayDialog("Ghi đè prefab?",
                    path + " đã tồn tại. Ghi đè sẽ mất các chỉnh sửa tay trên prefab này.", "Ghi đè", "Giữ nguyên"))
                return false;

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            if (prefab) EditorGUIUtility.PingObject(prefab);
            return prefab;
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Đồng bộ file save trên máy với Firestore.
/// - Đăng nhập: tải bản trên cloud về đè file trên máy (cloud là bản chính).
///   Ngoại lệ: file trên máy còn thay đổi chưa đẩy lên (pendingUpload) và mới hơn → đẩy bản máy lên.
/// - File trên máy thuộc tài khoản Firebase khác (khác uid) → cất sang file .bak, không dùng.
/// - Save cũ từ trước khi dùng Firebase (chưa có uid) → chuyển lên cloud một lần.
/// - Mỗi lần SaveSystem ghi file: đẩy lên cloud chạy ngầm. Lỗi mạng thì giữ cờ pendingUpload,
///   lần đăng nhập sau sẽ đẩy lại.
/// </summary>
public static class CloudSaveSync
{
    static bool pushing;
    static readonly List<string> queue = new List<string>();   // tài khoản đang chờ đẩy, mỗi tên một lần

    /// <summary>Chỉ đồng bộ khi đã đăng nhập bằng Firebase và đúng tài khoản đang chơi.</summary>
    public static bool ShouldSync(string user) =>
        AuthService.IsOnline && !string.IsNullOrEmpty(user) && user == AuthService.CurrentUser;

    /// <summary>Gọi sau khi đăng nhập. done(warning): null là ổn; có chuỗi = vẫn vào game được nhưng dùng dữ liệu trên máy.</summary>
    public static IEnumerator PullOnLogin(string user, Action<string> done)
    {
        const string offline = "Couldn't sync with the cloud. Using data on this computer.";
        string uid = AuthService.Uid;

        // File trên máy của một tài khoản Firebase khác trùng tên: cất đi trước, kể cả khi mất mạng
        bool hasLocal = SaveSystem.HasLocal(user);
        var local = hasLocal ? SaveSystem.LoadLocal(user) : null;
        if (hasLocal && !string.IsNullOrEmpty(local.uid) && local.uid != uid)
        {
            SaveSystem.BackupLocal(user, local.uid);
            hasLocal = false;
            local = null;
        }

        bool ok = false;
        yield return AuthService.EnsureFreshToken(r => ok = r);
        if (!ok) { done(offline); yield break; }

        PlayerSave cloud = null; bool found = false; string error = null;
        yield return FirestoreClient.LoadSave(uid, AuthService.IdToken, (s, f, e) => { cloud = s; found = f; error = e; });
        if (error != null) { done(offline); yield break; }

        bool localNewer = hasLocal && local.pendingUpload &&
                          (!found || string.CompareOrdinal(local.savedAtUtc ?? "", cloud.savedAtUtc ?? "") > 0);

        if (found && !localNewer)
        {
            cloud.user = user;
            cloud.uid = uid;
            cloud.pendingUpload = false;
            SaveSystem.WriteLocal(cloud);
        }
        else if (hasLocal)
        {
            // Chưa có gì trên cloud (tài khoản mới với save cũ trên máy), hoặc bản trên máy mới hơn → đẩy lên
            local.uid = uid;
            local.pendingUpload = true;
            SaveSystem.WriteLocal(local);
            Push(user);
        }
        done(null);
    }

    /// <summary>Đẩy bản trên máy của tài khoản lên cloud, chạy ngầm.</summary>
    public static void Push(string user)
    {
        if (!ShouldSync(user)) return;
        if (!queue.Contains(user)) queue.Add(user);
        if (!pushing) FirebaseRunner.Run(PushRoutine());
    }

    static IEnumerator PushRoutine()
    {
        pushing = true;
        while (queue.Count > 0)
        {
            string user = queue[0];
            queue.RemoveAt(0);
            // Nếu trong lúc đẩy có lần ghi mới, Push() thêm user vào hàng đợi lần nữa → vòng sau đẩy bản mới nhất

            if (!ShouldSync(user)) continue;   // đã đăng xuất: giữ cờ pendingUpload, lần đăng nhập sau đẩy

            bool tokenOk = false;
            yield return AuthService.EnsureFreshToken(r => tokenOk = r);
            if (!tokenOk || !ShouldSync(user)) continue;

            var payload = SaveSystem.LoadLocal(user);
            string version = payload.savedAtUtc;
            payload.pendingUpload = false;

            string error = null;
            yield return FirestoreClient.WriteSave(AuthService.Uid, AuthService.IdToken, payload, e => error = e);

            if (error == null)
            {
                // Bỏ cờ chỉ khi file trên máy vẫn là đúng bản vừa đẩy
                var now = SaveSystem.LoadLocal(user);
                if (now.savedAtUtc == version && now.pendingUpload)
                {
                    now.pendingUpload = false;
                    SaveSystem.WriteLocal(now);
                }
            }
            else
            {
                Debug.LogWarning("[CloudSave] Chưa đẩy được lên cloud, sẽ thử lại lần sau: " + error);
            }
        }
        pushing = false;
    }
}

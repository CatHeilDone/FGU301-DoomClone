
using UnityEngine;

public class CheatCodeManager : MonoBehaviour
{
    private string inputBuffer = "";

    private const string CLEAR_ROOM_CODE = "CLEARROOM";

    private void Update()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD

        if (PauseMenu.IsPaused)
            return;

        foreach (char character in Input.inputString)
        {
            // Bỏ qua ký tự không phải chữ cái.
            if (!char.IsLetter(character))
                continue;

            inputBuffer += char.ToUpperInvariant(character);

            // Giữ buffer ngắn để tránh lưu quá nhiều ký tự.
            if (inputBuffer.Length > CLEAR_ROOM_CODE.Length)
            {
                inputBuffer = inputBuffer.Substring(
                    inputBuffer.Length - CLEAR_ROOM_CODE.Length
                );
            }

            if (inputBuffer == CLEAR_ROOM_CODE)
            {
                ExecuteClearRoom();
                inputBuffer = "";
            }
        }

#endif
    }

    private void ExecuteClearRoom()
    {
        RoomEnemyGroup room = RoomEnemyGroup.CurrentRoom;

        if (room == null)
        {
            Debug.LogWarning(
                "[CHEAT] Player is not inside a registered room!"
            );
            return;
        }

        room.CheatClearRoom();
    }
}

using UnityEngine;

public class ExitGameButton : MonoBehaviour
{
    public void ExitGame()
    {
        Debug.Log("🚪 Thoát game được gọi...");

        // Nếu đang chạy trong trình chỉnh sửa Unity (Editor)
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;

        // Nếu đang chạy bản build (PC, Android, iOS)
#else
        Application.Quit();
#endif
    }
}

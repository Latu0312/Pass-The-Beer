using UnityEngine;

public class ResetPlayerPrefsButton : MonoBehaviour
{
    // Gắn script này vào Button, rồi add OnClick -> ResetAllData()
    public void ResetAllData()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        Debug.Log("🧹 Đã xóa toàn bộ PlayerPrefs!");
    }
}

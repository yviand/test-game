using UnityEngine;
using UnityEngine.SceneManagement; // Bắt buộc phải có để chuyển cảnh

public class MainMenuController : MonoBehaviour
{
    [Header("Scene Names")]
    [SerializeField] private string gameplaySceneName = "Gameplay"; // Tên Scene chính của bạn

    public void StartGame()
    {
        Debug.Log("Starting Game...");
        // Load Scene Gameplay theo tên. Đảm bảo tên này khớp 100% trong Build Settings
        SceneManager.LoadScene(gameplaySceneName);
    }

    public void QuitGame()
    {
        Debug.Log("Exiting Game...");

        // Nếu đang chạy trong Unity Editor
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            // Nếu đã build ra file .exe
            Application.Quit();
        #endif
    }
}
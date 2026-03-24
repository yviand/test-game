using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject pausePanel;

    [Header("Scenes")]
    private const string mainMenuSceneName = "Mainscreen";

    [Header("Options")]
    [SerializeField] private bool pauseAudio = true;

    private bool isPaused;

    public bool IsPaused => isPaused;

    private void Awake()
    {
        SetPaused(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
    }

    private void OnDisable()
    {
        if (isPaused)
        {
            SetPaused(false);
        }
    }

    public void TogglePause()
    {
        SetPaused(!isPaused);
    }

    public void Resume()
    {
        SetPaused(false);
    }

    public void GoToMainMenu()
    {
        SetPaused(false);

        if (string.IsNullOrWhiteSpace(mainMenuSceneName))
        {
            Debug.LogWarning($"{nameof(PauseManager)} is missing a main menu scene name.", this);
            return;
        }

        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void QuitGame()
    {
        SetPaused(false);

        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }

    private void SetPaused(bool paused)
    {
        isPaused = paused;
        Time.timeScale = isPaused ? 0f : 1f;

        if (pauseAudio)
        {
            AudioListener.pause = isPaused;
        }

        if (pausePanel != null)
        {
            pausePanel.SetActive(isPaused);
        }
    }
}

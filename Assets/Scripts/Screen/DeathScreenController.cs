using UnityEngine;
using UnityEngine.SceneManagement;

public class DeathScreenController : MonoBehaviour
{
    [Header("UI Panels")]
    [SerializeField] private GameObject deathPanel;
    [SerializeField] private bool unlockCursorOnDeath = true;
    [SerializeField] private string mainMenuSceneName = "MainMenuSceneName";

    private void Awake()
    {
        Time.timeScale = 1f;
        HideDeathScreen();
    }

    public void ShowDeathScreen()
    {
        if (deathPanel != null)
        {
            deathPanel.SetActive(true);
        }

        if (unlockCursorOnDeath)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }

    public void HideDeathScreen()
    {
        if (deathPanel != null)
        {
            deathPanel.SetActive(false);
        }
    }

    public void OnRespawnClicked()
    {
        Time.timeScale = 1f;

        if (GameController.Instance == null)
        {
            Debug.LogWarning($"{nameof(GameController)} instance was not found. Falling back to scene reload.", this);
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            return;
        }

        GameController.Instance.RespawnPlayer();
    }

    public void OnMainMenuClicked()
    {
        Time.timeScale = 1f;

        if (string.IsNullOrWhiteSpace(mainMenuSceneName))
        {
            Debug.LogWarning($"{nameof(DeathScreenController)} is missing a main menu scene name.", this);
            return;
        }

        SceneManager.LoadScene(mainMenuSceneName);
    }
}

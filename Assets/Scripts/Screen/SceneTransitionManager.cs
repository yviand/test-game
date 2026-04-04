using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class SceneTransitionManager : MonoBehaviour
{
    private static SceneTransitionManager instance;

    public static SceneTransitionManager Instance
    {
        get
        {
            if (instance != null)
            {
                return instance;
            }

            instance = FindFirstObjectByType<SceneTransitionManager>();
            if (instance != null)
            {
                return instance;
            }

            GameObject managerObject = new GameObject(nameof(SceneTransitionManager));
            instance = managerObject.AddComponent<SceneTransitionManager>();
            return instance;
        }
        private set => instance = value;
    }

    [Header("Overlay")]
    [SerializeField] private Canvas transitionCanvas;
    [SerializeField] private CanvasGroup fadeCanvasGroup;
    [SerializeField] private Image fadePanel;
    [SerializeField] private int sortingOrder = 5000;

    [Header("Timing")]
    [SerializeField] private float fadeDuration = 0.35f;

    [Header("Transition Safety")]
    [SerializeField] private bool disablePlayerDuringTransition = true;

    private Coroutine activeTransition;

    public bool IsTransitioning => activeTransition != null;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        EnsureOverlayExists();
        ConfigureOverlay();
        SetFadeInstant(0f, false);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void ChangeScene(string sceneName)
    {
        if (IsTransitioning)
        {
            return;
        }

        if (!IsSceneNameValid(sceneName))
        {
            return;
        }

        activeTransition = StartCoroutine(ChangeSceneRoutine(sceneName));
    }

    private IEnumerator ChangeSceneRoutine(string sceneName)
    {
        PrepareCurrentSceneForTransition();

        yield return FadeTo(1f);

        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(sceneName);
        if (loadOperation == null)
        {
            Debug.LogWarning($"{nameof(SceneTransitionManager)} could not start loading scene '{sceneName}'.", this);
            yield return FadeTo(0f);
            activeTransition = null;
            yield break;
        }

        while (!loadOperation.isDone)
        {
            yield return null;
        }

        // Wait one more frame so the new scene can finish creating its UI/cameras under the black overlay.
        yield return null;

        EnsureOverlayExists();
        ConfigureOverlay();
        SetFadeInstant(1f, true);

        yield return FadeTo(0f);

        activeTransition = null;
    }

    private IEnumerator FadeTo(float targetAlpha)
    {
        EnsureOverlayExists();
        ConfigureOverlay();

        float startAlpha = fadeCanvasGroup != null ? fadeCanvasGroup.alpha : 0f;
        float duration = Mathf.Max(0f, fadeDuration);

        if (duration <= 0f)
        {
            SetFadeInstant(targetAlpha, targetAlpha > 0f);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            SetFadeInstant(alpha, true);
            yield return null;
        }

        SetFadeInstant(targetAlpha, targetAlpha > 0f);
    }

    private void PrepareCurrentSceneForTransition()
    {
        if (!disablePlayerDuringTransition)
        {
            return;
        }

        PlayerStats playerStats = PlayerStats.Instance;
        if (playerStats == null)
        {
            playerStats = FindFirstObjectByType<PlayerStats>();
        }

        if (playerStats == null)
        {
            return;
        }

        PlayerMovement playerMovement = playerStats.GetComponent<PlayerMovement>();
        if (playerMovement != null)
        {
            playerMovement.StopImmediately();
            playerMovement.enabled = false;
        }

        PlayerAttack playerAttack = playerStats.GetComponent<PlayerAttack>();
        if (playerAttack != null)
        {
            playerAttack.enabled = false;
        }

        Rigidbody2D playerRigidbody = playerStats.GetComponent<Rigidbody2D>();
        if (playerRigidbody != null)
        {
            playerRigidbody.linearVelocity = Vector2.zero;
            playerRigidbody.angularVelocity = 0f;
            playerRigidbody.simulated = false;
        }

        Collider2D[] playerColliders = playerStats.GetComponentsInChildren<Collider2D>(true);
        for (int i = 0; i < playerColliders.Length; i++)
        {
            if (playerColliders[i] != null)
            {
                playerColliders[i].enabled = false;
            }
        }
    }

    private void EnsureOverlayExists()
    {
        if (transitionCanvas == null)
        {
            transitionCanvas = GetComponentInChildren<Canvas>(true);
        }

        if (transitionCanvas == null)
        {
            GameObject canvasObject = new GameObject("TransitionCanvas");
            canvasObject.transform.SetParent(transform, false);

            transitionCanvas = canvasObject.AddComponent<Canvas>();
            transitionCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<GraphicRaycaster>();
        }

        if (fadeCanvasGroup == null)
        {
            fadeCanvasGroup = transitionCanvas.GetComponentInChildren<CanvasGroup>(true);
        }

        if (fadePanel == null)
        {
            fadePanel = transitionCanvas.GetComponentInChildren<Image>(true);
        }

        if (fadePanel == null)
        {
            GameObject panelObject = new GameObject("FadePanel");
            panelObject.transform.SetParent(transitionCanvas.transform, false);

            RectTransform rectTransform = panelObject.AddComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            fadePanel = panelObject.AddComponent<Image>();
            fadePanel.color = Color.black;
        }

        if (fadeCanvasGroup == null && fadePanel != null)
        {
            fadeCanvasGroup = fadePanel.GetComponent<CanvasGroup>();
            if (fadeCanvasGroup == null)
            {
                fadeCanvasGroup = fadePanel.gameObject.AddComponent<CanvasGroup>();
            }
        }
    }

    private void ConfigureOverlay()
    {
        if (transitionCanvas != null)
        {
            transitionCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            transitionCanvas.overrideSorting = true;
            transitionCanvas.sortingOrder = sortingOrder;
        }

        if (fadePanel != null)
        {
            fadePanel.color = Color.black;
            fadePanel.raycastTarget = true;
        }
    }

    private void SetFadeInstant(float alpha, bool blockRaycasts)
    {
        if (fadeCanvasGroup == null)
        {
            return;
        }

        fadeCanvasGroup.alpha = Mathf.Clamp01(alpha);
        fadeCanvasGroup.blocksRaycasts = blockRaycasts;
        fadeCanvasGroup.interactable = blockRaycasts;
    }

    private bool IsSceneNameValid(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning($"{nameof(SceneTransitionManager)} received an empty scene name.", this);
            return false;
        }

        if (sceneName == "MainMenuSceneName")
        {
            Debug.LogWarning($"{nameof(SceneTransitionManager)} received the placeholder scene name 'MainMenuSceneName'. Use the real scene name such as 'Mainscreen'.", this);
            return false;
        }

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogWarning($"{nameof(SceneTransitionManager)} cannot load scene '{sceneName}'. Check the exact name and Build Settings.", this);
            return false;
        }

        return true;
    }
}

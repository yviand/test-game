using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(CanvasGroup))]
public class IntroManager : MonoBehaviour, IPointerClickHandler
{
    [Header("UI")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private Button skipButton;

    [Header("Dialogue")]
    [SerializeField] private List<string> dialogueLines = new();

    [Header("Timing")]
    [SerializeField] private float typingDelay = 0.03f;
    [SerializeField] private float fadeDuration = 0.25f;
    [SerializeField] private bool freezeGameplayWhileIntro = true;
    [SerializeField] private bool disablePanelWhenFinished = true;

    private Coroutine typingCoroutine;
    private Coroutine closingCoroutine;
    private float cachedTimeScale = 1f;
    private int currentLineIndex;
    public event Action IntroCompleted;
    public static bool HasCompleted { get; private set; }
    public bool IsPlaying { get; private set; }
    public bool IsTyping => typingCoroutine != null;
    public bool ShouldBlockGameplayStart => isActiveAndEnabled && !HasCompleted;

    private void Awake()
    {
        ResolveReferences();
        SetPanelVisible(false, instant: true);
    }

    private void OnEnable()
    {
        ResolveReferences();

        if (skipButton != null)
        {
            skipButton.onClick.RemoveListener(SkipIntro);
            skipButton.onClick.AddListener(SkipIntro);
        }
    }

    private void OnDisable()
    {
        if (skipButton != null)
        {
            skipButton.onClick.RemoveListener(SkipIntro);
        }

        RestoreTimeScaleIfNeeded();
    }

    public void BeginIntro()
    {
        if (HasCompleted || IsPlaying)
        {
            return;
        }

        ResolveReferences();

        if (dialogueText == null)
        {
            Debug.LogWarning($"{nameof(IntroManager)} is missing a dialogue text reference. Completing intro immediately.", this);
            FinishIntro(immediate: true);
            return;
        }

        if (dialogueLines == null || dialogueLines.Count == 0)
        {
            FinishIntro(immediate: true);
            return;
        }

        if (freezeGameplayWhileIntro)
        {
            cachedTimeScale = Time.timeScale;
            Time.timeScale = 0f;
        }

        currentLineIndex = 0;
        HasCompleted = true;
        IsPlaying = true;

        SetPanelVisible(true, instant: true);
        StartTypingCurrentLine();
    }

    public void AdvanceDialogue()
    {
        if (!IsPlaying || closingCoroutine != null)
        {
            return;
        }

        if (IsTyping)
        {
            CompleteCurrentLineInstantly();
            return;
        }

        currentLineIndex++;
        if (currentLineIndex >= dialogueLines.Count)
        {
            FinishIntro();
            return;
        }

        StartTypingCurrentLine();
    }

    public void SkipIntro()
    {
        if (!IsPlaying || closingCoroutine != null)
        {
            return;
        }

        FinishIntro();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData == null || eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        AdvanceDialogue();
    }

    private void StartTypingCurrentLine()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }

        string currentLine = dialogueLines[currentLineIndex] ?? string.Empty;
        typingCoroutine = StartCoroutine(TypeLine(currentLine));
    }

    private IEnumerator TypeLine(string line)
    {
        dialogueText.text = line;
        dialogueText.maxVisibleCharacters = 0;
        dialogueText.ForceMeshUpdate();

        int visibleCharacterCount = dialogueText.textInfo.characterCount;
        for (int i = 1; i <= visibleCharacterCount; i++)
        {
            dialogueText.maxVisibleCharacters = i;
            yield return new WaitForSecondsRealtime(Mathf.Max(0f, typingDelay));
        }

        dialogueText.maxVisibleCharacters = int.MaxValue;
        typingCoroutine = null;
    }

    private void CompleteCurrentLineInstantly()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        dialogueText.maxVisibleCharacters = int.MaxValue;
    }

    private void FinishIntro(bool immediate = false)
    {
        if (closingCoroutine != null)
        {
            return;
        }

        CompleteCurrentLineInstantly();
        closingCoroutine = StartCoroutine(CloseRoutine(immediate));
    }

    private IEnumerator CloseRoutine(bool immediate)
    {
        if (!immediate && canvasGroup != null && fadeDuration > 0f)
        {
            float startAlpha = canvasGroup.alpha;
            float elapsed = 0f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / fadeDuration);
                canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t);
                yield return null;
            }
        }

        SetPanelVisible(false, instant: true);
        RestoreTimeScaleIfNeeded();

        IsPlaying = false;
        HasCompleted = true;
        closingCoroutine = null;

        IntroCompleted?.Invoke();

        if (disablePanelWhenFinished && panelRoot != null)
        {
            panelRoot.SetActive(false);
        }
    }

    private void ResolveReferences()
    {
        if (panelRoot == null)
        {
            panelRoot = gameObject;
        }

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        if (skipButton == null)
        {
            skipButton = GetComponentInChildren<Button>(true);
        }

        if (dialogueText == null)
        {
            TextMeshProUGUI[] textCandidates = GetComponentsInChildren<TextMeshProUGUI>(true);
            for (int i = 0; i < textCandidates.Length; i++)
            {
                TextMeshProUGUI candidate = textCandidates[i];
                if (candidate == null)
                {
                    continue;
                }

                Button parentButton = candidate.GetComponentInParent<Button>();
                if (skipButton != null && parentButton == skipButton)
                {
                    continue;
                }

                dialogueText = candidate;
                break;
            }
        }
    }

    private void RestoreTimeScaleIfNeeded()
    {
        if (!freezeGameplayWhileIntro)
        {
            return;
        }

        if (!Mathf.Approximately(Time.timeScale, 0f) && !IsPlaying)
        {
            return;
        }

        Time.timeScale = cachedTimeScale;
    }

    private void SetPanelVisible(bool visible, bool instant)
    {
        if (panelRoot != null && visible && !panelRoot.activeSelf)
        {
            panelRoot.SetActive(true);
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.interactable = visible;
            canvasGroup.blocksRaycasts = visible;
        }

        if (dialogueText != null && (!visible || instant))
        {
            dialogueText.maxVisibleCharacters = visible ? 0 : int.MaxValue;
        }
    }
}

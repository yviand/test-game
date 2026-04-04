using System.Collections.Generic;
using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CircleCollider2D))]
public class DoorInteraction : MonoBehaviour
{
    [Header("Scene Transition")]
    [SerializeField] private string targetSceneName = "Mainscreen";

    [Header("Interaction")]
    [SerializeField] private KeyCode interactKey = KeyCode.F;
    [SerializeField] private LayerMask playerLayers = 1 << 3;

    [Header("Prompt")]
    [SerializeField] private GameObject promptRoot;

    [SerializeField] private string targetExitName;
    private readonly HashSet<int> overlappingPlayerColliderIds = new();

    private CircleCollider2D triggerCollider;
    private bool isTransitioning;
    [SerializeField] private float interactionDelay = 0.8f;
    private Animator animator;


    public bool PlayerInRange => overlappingPlayerColliderIds.Count > 0;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        ResolveReferences();
        ConfigureTrigger();
        SetPromptVisible(false);
    }

    private void Update()
    {
        if (!PlayerInRange || isTransitioning)
        {
            return;
        }

        if (Input.GetKeyDown(interactKey))
        {
            StartCoroutine(InteractCoroutine());
        }
    }
    private IEnumerator InteractCoroutine()
    {
        isTransitioning = true;
        SetPromptVisible(false);

        // Kích hoạt Animation mở cửa
        if (animator != null)
        {
            animator.SetTrigger("Open"); 
        }

        if (PlayerStats.Instance != null)
        {
            Animator playerAnim = PlayerStats.Instance.GetComponent<Animator>();
            if (playerAnim != null)
            {
                playerAnim.SetTrigger("DoorIn");
            }
            
            // (Tùy chọn) Khóa di chuyển của Player để không bị trôi khi đang diễn animation
            var movement = PlayerStats.Instance.GetComponent<PlayerMovement>();
            if (movement != null) movement.enabled = false;
        }

        // Đợi một khoảng thời gian cho animation chạy
        yield return new WaitForSeconds(interactionDelay);

        // Bắt đầu chuyển cảnh
        if (GameController.Instance != null)
        {
            Debug.Log("DOOR: Setting last exit name to: " + targetExitName);
            GameController.Instance.lastExitName = targetExitName;
        }
        SceneTransitionManager.Instance.ChangeScene(targetSceneName);

        // Đợi cho đến khi SceneTransitionManager thực sự xong việc (nếu cần)
        yield return new WaitUntil(() => !SceneTransitionManager.Instance.IsTransitioning);

        isTransitioning = false;
        SetPromptVisible(PlayerInRange);
    }
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsPlayerCollider(other))
        {
            return;
        }

        overlappingPlayerColliderIds.Add(other.GetInstanceID());
        SetPromptVisible(true);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!IsPlayerCollider(other))
        {
            return;
        }

        overlappingPlayerColliderIds.Remove(other.GetInstanceID());
        if (PlayerInRange)
        {
            return;
        }

        if (!isTransitioning)
        {
            SetPromptVisible(false);
        }
    }

    private void ResolveReferences()
    {
        if (triggerCollider == null)
        {
            triggerCollider = GetComponent<CircleCollider2D>();
        }

        if (promptRoot == null && transform.childCount > 0)
        {
            Transform promptChild = transform.Find("Prompt");
            if (promptChild != null)
            {
                promptRoot = promptChild.gameObject;
            }
        }
    }

    private void ConfigureTrigger()
    {
        if (triggerCollider == null)
        {
            return;
        }

        triggerCollider.isTrigger = true;
    }

    private void SetPromptVisible(bool visible)
    {
        if (promptRoot != null)
        {
            promptRoot.SetActive(visible);
        }
    }

    private bool IsPlayerCollider(Collider2D other)
    {
        if (other == null)
        {
            return false;
        }

        if (!IsLayerInMask(other.gameObject.layer, playerLayers))
        {
            return false;
        }

        return other.GetComponentInParent<PlayerMovement>() != null
            || other.GetComponentInParent<PlayerStats>() != null;
    }

    private static bool IsLayerInMask(int layer, LayerMask layerMask)
    {
        return (layerMask.value & (1 << layer)) != 0;
    }

    private void OnValidate()
    {
        ResolveReferences();
        ConfigureTrigger();
    }
}

using UnityEngine;
using System.Collections.Generic;

public class GameController : MonoBehaviour
{
    public static GameController Instance { get; private set; }

    [Header("Spawn")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Transform spawnPoint;

    [Header("Systems")]
    [SerializeField] private IntroManager introManager;

    private DeathScreenController deathScreenController;

    [HideInInspector] public static string lastExitName;
    public static HashSet<string> persistentDestroyedObjects = new HashSet<string>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        deathScreenController = FindFirstObjectByType<DeathScreenController>();
        ResolveIntroManager();
    }

    private void Start()
    {
        if (ShouldDelayInitialSpawnForIntro())
        {
            introManager.IntroCompleted -= HandleIntroCompleted;
            introManager.IntroCompleted += HandleIntroCompleted;
            introManager.BeginIntro();
            return;
        }

        SpawnInitialPlayer();
    }

    private void OnDestroy()
    {
        if (introManager != null)
        {
            introManager.IntroCompleted -= HandleIntroCompleted;
        }

        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void Respawn()
    {
        RespawnPlayer();
    }

    public void RespawnPlayer()
    {
        if (HasPlayerInScene())
        {
            // Debug.LogWarning($"{nameof(GameController)} will not respawn because a player already exists in the scene.", this);
            return;
        }

        SpawnPlayer();

        if (deathScreenController != null)
        {
            deathScreenController.HideDeathScreen();
        }

        InventoryManager.Instance?.Refresh();
    }

    private void HandleIntroCompleted()
    {
        if (introManager != null)
        {
            introManager.IntroCompleted -= HandleIntroCompleted;
        }

        SpawnInitialPlayer();
    }

    private void SpawnInitialPlayer()
    {
        if (!HasPlayerInScene())
        {
            SpawnPlayer();
        }

        if (deathScreenController != null)
        {
            deathScreenController.HideDeathScreen();
        }

        InventoryManager.Instance?.Refresh();
    }

    private void SpawnPlayer()
    {
        if (playerPrefab == null)
        {
            Debug.LogError($"{nameof(GameController)} is missing a player prefab reference.", this);
            return;
        }

        // 1. Thiết lập vị trí mặc định ban đầu
        Vector3 spawnPosition = spawnPoint != null ? spawnPoint.position : Vector3.zero;
        Quaternion spawnRotation = spawnPoint != null ? spawnPoint.rotation : Quaternion.identity;

        // 2. KIỂM TRA VỊ TRÍ CỬA (Logic mới)
        // Nếu lastExitName có giá trị, chúng ta đi tìm cái cửa đó trong Scene mới
        if (!string.IsNullOrEmpty(lastExitName))
        {
            GameObject arrivalDoor = GameObject.Find(lastExitName);
            if (arrivalDoor != null)
            {
                spawnPosition = arrivalDoor.transform.position;
                // (Tùy chọn) spawnRotation = arrivalDoor.transform.rotation; 
                
                // Sau khi spawn xong, hãy xóa tên cửa cũ để lần sau load bình thường 
                // nếu bạn không đi qua cửa (ví dụ hồi sinh/reset game)
                lastExitName = null; 
            }
            else
            {
                Debug.LogWarning($"[GameController] Không tìm thấy cửa tên: {lastExitName}. Đang dùng SpawnPoint mặc định.");
            }
        }

        // 3. Thực hiện Instantiate tại vị trí đã xác định
        GameObject playerObject = Instantiate(playerPrefab, spawnPosition, spawnRotation);

        // 4. Các logic kiểm tra stats và bind inventory cũ của bạn
        if (playerObject.GetComponent<PlayerStats>() == null)
        {
            Debug.LogWarning($"{nameof(GameController)} spawned a player prefab without {nameof(PlayerStats)}.", playerObject);
        }

        PlayerInventory playerInventory = playerObject.GetComponent<PlayerInventory>();
        if (playerInventory != null)
        {
            playerInventory.RefreshBinding();
        }
    }
    public InventoryManager GetInventory()
    {
        if (InventoryManager.Instance == null)
        {
            Debug.LogError($"{nameof(GameController)}: {nameof(InventoryManager)} Instance is missing! Inventory system will not function.", this);
        }
        return InventoryManager.Instance;
    }

    private bool HasPlayerInScene()
    {
        PlayerStats[] players = FindObjectsByType<PlayerStats>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        for (int i = 0; i < players.Length; i++)
        {
            PlayerStats playerStats = players[i];
            if (playerStats == null)
            {
                continue;
            }

            if (playerStats.gameObject.scene.IsValid())
            {
                return true;
            }
        }

        return false;
    }

    private void ResolveIntroManager()
    {
        if (introManager == null)
        {
            introManager = GetComponent<IntroManager>();
        }

        if (introManager == null)
        {
            introManager = GetComponentInChildren<IntroManager>(true);
        }

        if (introManager == null)
        {
            introManager = FindFirstObjectByType<IntroManager>();
        }
    }

    private bool ShouldDelayInitialSpawnForIntro()
    {
        ResolveIntroManager();
        return introManager != null && introManager.ShouldBlockGameplayStart;
    }
}

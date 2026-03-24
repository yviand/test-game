using UnityEngine;

public class GameController : MonoBehaviour
{
    public static GameController Instance { get; private set; }

    [Header("Spawn")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Transform spawnPoint;

    [Header("Systems")]
    [SerializeField] private InventoryManager inventoryManager;

    private DeathScreenController deathScreenController;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        deathScreenController = FindFirstObjectByType<DeathScreenController>();
        ResolveInventoryManager();
    }

    private void Start()
    {
        SpawnInitialPlayer();
    }

    private void OnDestroy()
    {
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

        inventoryManager?.Refresh();
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

        inventoryManager?.Refresh();
    }

    private void SpawnPlayer()
    {
        if (playerPrefab == null)
        {
            Debug.LogError($"{nameof(GameController)} is missing a player prefab reference.", this);
            return;
        }

        Vector3 spawnPosition = spawnPoint != null ? spawnPoint.position : Vector3.zero;
        Quaternion spawnRotation = spawnPoint != null ? spawnPoint.rotation : Quaternion.identity;

        GameObject playerObject = Instantiate(playerPrefab, spawnPosition, spawnRotation);
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
        ResolveInventoryManager();
        return inventoryManager;
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

    private void ResolveInventoryManager()
    {
        if (inventoryManager == null)
        {
            inventoryManager = GetComponent<InventoryManager>();
        }

        if (inventoryManager == null)
        {
            inventoryManager = GetComponentInChildren<InventoryManager>(true);
        }

        if (inventoryManager == null)
        {
            Debug.LogWarning($"{nameof(GameController)} is missing an {nameof(InventoryManager)} reference.", this);
        }
    }
}

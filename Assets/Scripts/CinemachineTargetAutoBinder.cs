using Unity.Cinemachine;
using UnityEngine;

public class CinemachineTargetAutoBinder : MonoBehaviour
{
    [SerializeField] private CinemachineCamera targetCamera;
    [SerializeField] private bool bindLookAt = true;

    private void Awake()
    {
        if (targetCamera == null)
        {
            targetCamera = GetComponent<CinemachineCamera>();
        }
    }

    private void OnEnable()
    {
        PlayerStats.OnPlayerSpawned += BindToPlayer;
        BindToPlayer(PlayerStats.Instance);
    }

    private void OnDisable()
    {
        PlayerStats.OnPlayerSpawned -= BindToPlayer;
    }

    private void BindToPlayer(PlayerStats playerStats)
    {
        if (targetCamera == null)
        {
            return;
        }

        Transform target = playerStats != null ? playerStats.transform : null;
        targetCamera.Follow = target;

        if (bindLookAt)
        {
            targetCamera.LookAt = target;
        }
    }
}

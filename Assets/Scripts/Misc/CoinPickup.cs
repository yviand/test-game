using UnityEngine;

public class CoinPickup : MonoBehaviour
{
    [SerializeField] private float pickupRange = 1f;
    [SerializeField] private float magnetSpeed = 6f;
    [SerializeField] private int amount = 1;

    private Transform player;
    private PlayerCoin playerCoin;

    private bool isMagnet;

    void Start()
    {
        player = GameObject.FindWithTag("Player").transform;
        playerCoin = player.GetComponent<PlayerCoin>();
    }

    void Update()
    {
        float distance = Vector2.Distance(transform.position, player.position);

        if (!isMagnet && distance <= pickupRange)
        {
            isMagnet = true;
        }

        if (isMagnet)
        {
            Vector2 direction = (player.position - transform.position).normalized;

            transform.position += (Vector3)(direction * magnetSpeed * Time.deltaTime);

            if (distance < 0.2f)
            {
                playerCoin.AddCoin(amount);
                Destroy(this.gameObject);
            }
        }
    }
}
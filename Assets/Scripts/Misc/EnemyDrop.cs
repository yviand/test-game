using UnityEngine;

public class EnemyDrop : MonoBehaviour
{
    [SerializeField] private GameObject coinPrefab;

    [SerializeField] private float coinDropRate = 0.8f;

    [SerializeField] private int minCoin = 6;
    [SerializeField] private int maxCoin = 15;

    public void DropLoot()
    {
        if (Random.value > coinDropRate)
        {
            return;
        }

        int coinAmount = Random.Range(minCoin, maxCoin + 1);

        for (int i = 0; i < coinAmount; i++)
        {
            Vector2 offset = Random.insideUnitCircle * 0.5f;

            Instantiate(
                coinPrefab,
                (Vector2)transform.position + offset,
                Quaternion.identity
            );
        }
    }
}
using UnityEngine;

public class PlayerCoin : MonoBehaviour
{
    public int currentCoin = 0;

    public void AddCoin(int amount)
    {
        currentCoin += amount;
        Debug.Log("Current coins: " + currentCoin);
    }
}
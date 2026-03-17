using UnityEngine;

public class CurrencyDrop : BaseDrop
{
    [SerializeField] private CurrencyType currencyType = CurrencyType.Coins;
    [SerializeField] private int amount = 1;

    public void SetAmount(int newAmount)
    {
        amount = Mathf.Max(0, newAmount);
    }

    public void SetCurrencyType(CurrencyType newCurrencyType)
    {
        currencyType = newCurrencyType;
    }

    protected override bool Collect(PlayerItem playerItem)
    {
        if (amount <= 0)
        {
            return false;
        }

        playerItem.AddBalance(currencyType, amount);
        return true;
    }
}

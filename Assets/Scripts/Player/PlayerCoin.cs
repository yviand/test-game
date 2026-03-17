public class PlayerCoin : PlayerItem
{
    public int currentCoin => Coins;

    public void AddCoin(int amount)
    {
        AddBalance(amount);
    }
}

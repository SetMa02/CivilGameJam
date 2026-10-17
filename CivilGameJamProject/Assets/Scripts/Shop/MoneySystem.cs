using UnityEngine;
using TMPro;

public class MoneySystem : MonoBehaviour
{
    public int money = 0;
    public int moneyPerClick = 100;

    public TMP_Text moneyText;

    void Start()
    {
        UpdateMoneyText();
        Debug.Log("Current money: " + money);
    }

    // Для UI кнопки
    public void AddMoney()
    {
        AddMoneyAmount(moneyPerClick);
    }

    // Для наград из других скриптов
    public void AddMoneyAmount(int amount)
    {
        money += amount;

        Debug.Log("Reward received: +" + amount);
        Debug.Log("Current money: " + money);

        UpdateMoneyText();
    }

    // Трата денег
    public bool SpendMoney(int amount)
    {
        if (money >= amount)
        {
            money -= amount;

            Debug.Log("Money spent: -" + amount);
            Debug.Log("Current money: " + money);

            UpdateMoneyText();

            return true;
        }

        Debug.Log("Not enough money!");
        Debug.Log("Current money: " + money);

        return false;
    }

    void UpdateMoneyText()
    {
        moneyText.text = "$" + money;
    }
}
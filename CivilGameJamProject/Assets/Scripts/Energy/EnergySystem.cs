using UnityEngine;

public class EnergySystem : MonoBehaviour
{
    [Header("Energy")]
    public int maxEnergy = 2;
    public int currentEnergy = 2;

    void Start()
    {
        currentEnergy = maxEnergy;

        Debug.Log("Current Energy: " + currentEnergy);
    }

    public bool UseEnergy(int amount)
    {
        if (currentEnergy >= amount)
        {
            currentEnergy -= amount;

            Debug.Log("Energy used: -" + amount);
            Debug.Log("Current Energy: " + currentEnergy);

            return true;
        }

        Debug.Log("Not enough energy!");
        Debug.Log("Current Energy: " + currentEnergy);

        return false;
    }

    public void RestoreEnergy(int amount)
    {
        currentEnergy += amount;

        if (currentEnergy > maxEnergy)
        {
            currentEnergy = maxEnergy;
        }

        Debug.Log("Energy restored: +" + amount);
        Debug.Log("Current Energy: " + currentEnergy);
    }

    public void RestoreAllEnergy()
    {
        currentEnergy = maxEnergy;

        Debug.Log("Energy fully restored.");
        Debug.Log("Current Energy: " + currentEnergy);
    }
}
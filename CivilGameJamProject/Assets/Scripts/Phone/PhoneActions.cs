using UnityEngine;

public class PhoneActions : MonoBehaviour
{
    public EnergySystem energySystem;

    public void PhoneAction1()
    {
        if (energySystem.UseEnergy(1))
        {
            Debug.Log("Phone Action 1 activated!");

            // Здесь логика первого действия
        }
    }

    public void PhoneAction2()
    {
        if (energySystem.UseEnergy(1))
        {
            Debug.Log("Phone Action 2 activated!");

            // Здесь логика второго действия
        }
    }
}
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public RegionHealth region1;
    public RegionHealth region2;
    public RegionHealth region3;
    public RegionHealth region4;

    public bool region1Alive;
    public bool region2Alive;
    public bool region3Alive;
    public bool region4Alive;

    public bool playerAlive = true;

    void Update()
    { 
        /*
        region1Alive = region1.hp > 0;
        region2Alive = region2.hp > 0;
        region3Alive = region3.hp > 0;
        region4Alive = region4.hp > 0;

        playerAlive = region1Alive || region2Alive || region3Alive || region4Alive;

        if (!playerAlive)
        {
            Die();
        }
        */
    }

    void Die()
    {
        Debug.Log("Player is dead");
    }
}
using UnityEngine;

public class RegionHealth : MonoBehaviour
{
   // public const int hp = 1;
    public bool region1 = true;
    public bool region2 = true;
    public bool region3 = true;
    public bool region4 = true;

    public bool isAlive;

    void Update()
    {
        isAlive = region1 || region2 || region3 || region4;

        if (!isAlive)
        {
            GameOver();
        }
    }

    void GameOver()
    {
        Debug.Log("Game Over");
    }
}
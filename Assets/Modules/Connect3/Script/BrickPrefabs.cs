using System.Collections.Generic;
using UnityEngine;

public class BrickPrefabs : MonoBehaviour
{
    public static BrickPrefabs I;

    public List<GameObject> Prefabs;

    private void Awake()
    {
        I = this;
    }

    public GameObject GetRandomBrickPrefab()
    {
        if (Prefabs == null || Prefabs.Count == 0)
            return null;

        int randomIndex = Random.Range(0, Prefabs.Count);

        const int BallTypeIdx = 0;
        if (Random.Range(0, 10) == 9)
            randomIndex = BallTypeIdx;

        // Higher chance for balls
        return Prefabs[randomIndex];
    }
}

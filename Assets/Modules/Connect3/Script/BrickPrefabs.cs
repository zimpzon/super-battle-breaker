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

        int randomIndex = Random.Range(1, Prefabs.Count); // from 1, so first one skipped
        return Prefabs[randomIndex];
    }
}

using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class TileRule
{
    public GameObject tile;
    public float weight = 1f;
    public bool canBeBorder;
    public bool requiresNeighbor;
    public string requiredNeighborName;
    public float minHeight;
    public float maxHeight;
}

public class Generator : MonoBehaviour
{
    public TileRule[] tileRules;
    public int width = 10;
    public int depth = 10;
    public float tileSize = 10f;
    
    private GameObject[,] placedTiles;

    void Start()
    {
        placedTiles = new GameObject[width, depth];
        GenerateLevel();
    }

    void GenerateLevel()
    {
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < depth; z++)
            {
                Vector3 pos = new Vector3(x * tileSize, 0, z * tileSize);
                GameObject tilePrefab = SelectTileByRules(x, z);
                
                if (tilePrefab != null)
                {
                    GameObject newTile = Instantiate(tilePrefab, pos, Quaternion.identity, transform);
                    placedTiles[x, z] = newTile;
                }
            }
        }
    }

    GameObject SelectTileByRules(int x, int z)
    {
        // Get eligible tiles based on position and neighbors
        List<TileRule> eligibleTiles = new List<TileRule>();
        float totalWeight = 0f;

        foreach (TileRule rule in tileRules)
        {
            // Border check
            if ((x == 0 || x == width-1 || z == 0 || z == depth-1) && !rule.canBeBorder)
                continue;

            // Neighbor requirement check
            if (rule.requiresNeighbor && !HasRequiredNeighbor(x, z, rule.requiredNeighborName))
                continue;

            eligibleTiles.Add(rule);
            totalWeight += rule.weight;
        }

        if (eligibleTiles.Count == 0)
            return null;

        // Weighted random selection
        float randomValue = Random.value * totalWeight;
        float cumulativeWeight = 0f;

        foreach (TileRule rule in eligibleTiles)
        {
            cumulativeWeight += rule.weight;
            if (randomValue <= cumulativeWeight)
            {
                return rule.tile;
            }
        }

        return eligibleTiles[0].tile;
    }

    bool HasRequiredNeighbor(int x, int z, string requiredName)
    {
        // Check all four directions
        int[][] directions = new int[][] {
            new int[] { -1, 0 }, // West
            new int[] { 1, 0 },  // East
            new int[] { 0, -1 }, // North
            new int[] { 0, 1 }   // South
        };

        foreach (int[] dir in directions)
        {
            int checkX = x + dir[0];
            int checkZ = z + dir[1];

            if (checkX >= 0 && checkX < width && checkZ >= 0 && checkZ < depth)
            {
                if (placedTiles[checkX, checkZ] != null && 
                    placedTiles[checkX, checkZ].name.Contains(requiredName))
                {
                    return true;
                }
            }
        }

        return false;
    }
}
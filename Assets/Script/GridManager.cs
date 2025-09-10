using System.Collections.Generic;
using UnityEngine;

public class GridManager : SingletonBehaviour<GridManager>
{
    public int width = 20;
    public int height = 20;
    public Transform gridParent;
    [Header("Prefabs")]
    public GameObject wayPrefab;
    public GameObject wallPrefab;
    public GameObject startPrefab;
    public GameObject endPrefab;
    public GameObject npcPrefab;

    private int[,] grid;
    private GameObject[,] spawnedTiles;
    private Color[,] originalColors;
    private List<Vector2Int> directions = new List<Vector2Int>
    {
        new Vector2Int(0, 2),
        new Vector2Int(2, 0),
        new Vector2Int(0, -2),
        new Vector2Int(-2, 0)
    };
    protected override void Awake()
    {
        MakeSingleton(false);
    }

    public void GenerateGrid()
    {
        if (gridParent != null)
        {
            foreach (Transform child in gridParent)
            {
                Destroy(child.gameObject);
            }
        }

        grid = new int[width, height];
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                grid[x, y] = 1;

        GenerateMaze(1, 1);

        grid[1, 1] = 2;
        Vector2Int endPos = FindEndOfPath();
        grid[endPos.x, endPos.y] = 3;

        SpawnGrid();
        SpawnNPC();
    }

    private void GenerateMaze(int x, int y)
    {
        grid[x, y] = 0;
        List<Vector2Int> randomizedDirections = new List<Vector2Int>(directions);
        Shuffle(randomizedDirections);

        foreach (Vector2Int dir in randomizedDirections)
        {
            int newX = x + dir.x;
            int newY = y + dir.y;
            if (IsValidPosition(newX, newY) && grid[newX, newY] == 1)
            {
                grid[x + dir.x / 2, y + dir.y / 2] = 0;
                grid[newX, newY] = 0;
                GenerateMaze(newX, newY);
            }
        }

        if (Random.value < 0.3f)
        {
            List<Vector2Int> additionalDirections = new List<Vector2Int>(directions);
            Shuffle(additionalDirections);
            foreach (Vector2Int extraDir in additionalDirections)
            {
                int extraX = x + extraDir.x;
                int extraY = y + extraDir.y;
                if (IsValidPosition(extraX, extraY) && grid[extraX, extraY] == 1)
                {
                    grid[x + extraDir.x / 2, y + extraDir.y / 2] = 0;
                    grid[extraX, extraY] = 0;
                    GenerateMaze(extraX, extraY);
                }
            }
        }
    }

    private bool IsValidPosition(int x, int y)
    {
        return x >= 1 && x < width - 1 && y >= 1 && y < height - 1;
    }

    private Vector2Int FindEndOfPath()
    {
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        bool[,] visited = new bool[width, height];
        List<Vector2Int> pathEnds = new List<Vector2Int>();
        int[] dx = { 0, 0, 1, -1 };
        int[] dy = { 1, -1, 0, 0 };

        queue.Enqueue(new Vector2Int(1, 1));
        visited[1, 1] = true;

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            int neighborCount = 0;

            for (int i = 0; i < 4; i++)
            {
                int newX = current.x + dx[i];
                int newY = current.y + dy[i];
                if (IsValidPosition(newX, newY) && grid[newX, newY] == 0 && !visited[newX, newY])
                {
                    queue.Enqueue(new Vector2Int(newX, newY));
                    visited[newX, newY] = true;
                    neighborCount++;
                }
            }

            if (neighborCount == 0)
                pathEnds.Add(current);
        }

        if (pathEnds.Count == 0)
        {
            Debug.LogWarning("Không tìm thấy điểm cuối! Tạo lại mê cung...");
            GenerateMaze(1, 1);
            return FindEndOfPath();
        }

        Vector2Int bestEnd = pathEnds[0];
        float minDist = Vector2Int.Distance(new Vector2Int(width - 2, height - 2), bestEnd);
        foreach (Vector2Int end in pathEnds)
        {
            float dist = Vector2Int.Distance(new Vector2Int(width - 2, height - 2), end);
            if (dist < minDist)
            {
                minDist = dist;
                bestEnd = end;
            }
        }

        return bestEnd;
    }

    private void SpawnGrid()
    {
        spawnedTiles = new GameObject[width, height];
        originalColors = new Color[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3 position = new Vector3(x, y, 0);
                GameObject prefabToSpawn;

                switch (grid[x, y])
                {
                    case 0:
                        prefabToSpawn = wayPrefab;
                        break;
                    case 2:
                        prefabToSpawn = startPrefab;
                        break;
                    case 3:
                        prefabToSpawn = endPrefab;
                        break;
                    default:
                        prefabToSpawn = wallPrefab;
                        break;
                }

                if (prefabToSpawn != null)
                {
                    GameObject go = Instantiate(prefabToSpawn, position, Quaternion.identity, gridParent);
                    spawnedTiles[x, y] = go;

                    SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
                    if (sr != null)
                        originalColors[x, y] = sr.color;
                    else
                    {
                        Renderer r = go.GetComponent<Renderer>();
                        originalColors[x, y] = r != null ? r.material.color : Color.white;
                    }
                }
            }
        }
    }

    private void SpawnNPC()
    {
        Vector3 startPosition = new Vector3(1, 1, 0);
        GameObject npc = Instantiate(npcPrefab, startPosition, Quaternion.identity, gridParent);
        NPCController npcController = npc.GetComponent<NPCController>();
        if (npcController != null)
            npcController.StartMove();
    }

    private void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            T temp = list[i];
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }

    public int GetGridValue(int x, int y)
    {
        if (x >= 0 && x < width && y >= 0 && y < height)
            return grid[x, y];
        return 1;
    }

    public void HighlightPath(List<Vector2Int> path, Color color)
    {
        if (spawnedTiles == null || path == null) return;

        foreach (Vector2Int p in path)
        {
            if (p.x < 0 || p.x >= width || p.y < 0 || p.y >= height) continue;

            if (grid[p.x, p.y] != 0) continue;

            GameObject go = spawnedTiles[p.x, p.y];
            if (go == null) continue;

            SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.color = color;
                continue;
            }

            Renderer r = go.GetComponent<Renderer>();
            if (r != null)
            {
                r.material.color = color;
                continue;
            }
        }
    }

    public void ClearHighlight()
    {
        if (spawnedTiles == null || originalColors == null) return;

        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                if (grid[x, y] != 0) continue;

                GameObject go = spawnedTiles[x, y];
                if (go == null) continue;

                SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.color = originalColors[x, y];
                    continue;
                }

                Renderer r = go.GetComponent<Renderer>();
                if (r != null)
                {
                    r.material.color = originalColors[x, y];
                    continue;
                }
            }
    }
}

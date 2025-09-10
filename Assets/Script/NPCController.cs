using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class NPCController : MonoBehaviour
{
    public float speed = 2f;
    private GridManager gridManager;
    private Vector3 targetPosition;
    private List<Vector2Int> path;
    private int currentPathIndex = 0;

    [Header("Highlight")]
    public Color highlightColor;
    public float highlightDuration;

    void Start()
    {
        gridManager = GridManager.instance;
        path = new List<Vector2Int>();
    }

    public void StartMove()
    {
        if (gridManager == null) return;

        Vector2Int startPos = FindStartPosition();
        Vector2Int endPos = FindEndPosition();

        if (startPos != Vector2Int.zero && endPos != Vector2Int.zero)
        {
            transform.position = new Vector3(startPos.x, startPos.y, 0);

            List<Vector2Int> foundPath = FindPath(startPos, endPos);
            if (foundPath == null || foundPath.Count == 0) return;

            StartCoroutine(ShowPathThenMove(foundPath));
        }
    }

    private IEnumerator ShowPathThenMove(List<Vector2Int> foundPath)
    {
        if (gridManager != null)
            gridManager.HighlightPath(foundPath, highlightColor);

        if (highlightDuration > 0f)
            yield return new WaitForSeconds(highlightDuration);

        BeginMovement(foundPath);
    }

    private void BeginMovement(List<Vector2Int> foundPath)
    {
        path = foundPath;
        currentPathIndex = 0;
        if (path != null && path.Count > 0)
            targetPosition = new Vector3(path[currentPathIndex].x, path[currentPathIndex].y, 0);
    }

    void Update()
    {
        if (path != null && currentPathIndex < path.Count)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);

            if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
            {
                currentPathIndex++;
                if (currentPathIndex < path.Count)
                {
                    targetPosition = new Vector3(path[currentPathIndex].x, path[currentPathIndex].y, 0);
                }
                else
                {
                    gridManager.ClearHighlight();
                }
            }
        }
    }

    private Vector2Int FindStartPosition()
    {
        for (int x = 0; x < gridManager.width; x++)
            for (int y = 0; y < gridManager.height; y++)
                if (gridManager.GetGridValue(x, y) == 2)
                    return new Vector2Int(x, y);
        return Vector2Int.zero;
    }

    private Vector2Int FindEndPosition()
    {
        for (int x = 0; x < gridManager.width; x++)
            for (int y = 0; y < gridManager.height; y++)
                if (gridManager.GetGridValue(x, y) == 3)
                    return new Vector2Int(x, y);
        return Vector2Int.zero;
    }

    private List<Vector2Int> FindPath(Vector2Int start, Vector2Int end)
    {
        List<Vector2Int> openSet = new List<Vector2Int>();
        Dictionary<Vector2Int, Vector2Int> cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        Dictionary<Vector2Int, float> gScore = new Dictionary<Vector2Int, float>();
        Dictionary<Vector2Int, float> fScore = new Dictionary<Vector2Int, float>();

        openSet.Add(start);
        gScore[start] = 0;
        fScore[start] = Heuristic(start, end);

        while (openSet.Count > 0)
        {
            Vector2Int current = GetLowestFScore(openSet, fScore);
            if (current == end)
                return ReconstructPath(cameFrom, current);

            openSet.Remove(current);

            int[] dx = { 0, 0, 1, -1 };
            int[] dy = { 1, -1, 0, 0 };
            for (int i = 0; i < 4; i++)
            {
                Vector2Int neighbor = new Vector2Int(current.x + dx[i], current.y + dy[i]);
                if (IsValidPosition(neighbor) && gridManager.GetGridValue(neighbor.x, neighbor.y) != 1)
                {
                    float tentativeGScore = gScore[current] + 1;
                    if (!gScore.ContainsKey(neighbor) || tentativeGScore < gScore[neighbor])
                    {
                        cameFrom[neighbor] = current;
                        gScore[neighbor] = tentativeGScore;
                        fScore[neighbor] = gScore[neighbor] + Heuristic(neighbor, end);
                        if (!openSet.Contains(neighbor))
                            openSet.Add(neighbor);
                    }
                }
            }
        }
        return null;
    }

    private bool IsValidPosition(Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < gridManager.width && pos.y >= 0 && pos.y < gridManager.height;
    }

    private float Heuristic(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    private Vector2Int GetLowestFScore(List<Vector2Int> openSet, Dictionary<Vector2Int, float> fScore)
    {
        Vector2Int lowest = openSet[0];
        foreach (Vector2Int pos in openSet)
        {
            if (fScore[pos] < fScore[lowest])
                lowest = pos;
        }
        return lowest;
    }

    private List<Vector2Int> ReconstructPath(Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int current)
    {
        List<Vector2Int> path = new List<Vector2Int>();
        path.Add(current);
        while (cameFrom.ContainsKey(current))
        {
            current = cameFrom[current];
            path.Add(current);
        }
        path.Reverse();
        return path;
    }
}

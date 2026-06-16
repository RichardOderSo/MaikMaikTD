using Godot;
using System;
using System.Collections.Generic;

public partial class GridManager : Node3D
{
    public static GridManager Instance { get; private set; }

    [Export]
    public float CellSize { get; set; } = 1.0f;

    [Export]
    public Vector2I HalfExtents { get; set; } = new Vector2I(50, 50);

    [Export]
    public bool AllowDiagonalMovement { get; set; } = false;

    private readonly Dictionary<Vector2I, GridObstacle> _obstacles = new();

    private static readonly Vector2I[] CardinalDirections =
    {
        new(1, 0),
        new(-1, 0),
        new(0, 1),
        new(0, -1),
    };

    private static readonly Vector2I[] DiagonalDirections =
    {
        new(1, 1),
        new(1, -1),
        new(-1, 1),
        new(-1, -1),
    };

    public override void _EnterTree()
    {
        Instance = this;
    }

    public override void _ExitTree()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public Vector2I WorldToCell(Vector3 worldPosition)
    {
        Vector3 local = worldPosition - GlobalPosition;
        return new Vector2I(
            Mathf.RoundToInt(local.X / CellSize),
            Mathf.RoundToInt(local.Z / CellSize));
    }

    public Vector3 CellToWorld(Vector2I cell, float y = 0.0f)
    {
        return new Vector3(
            GlobalPosition.X + cell.X * CellSize,
            y,
            GlobalPosition.Z + cell.Y * CellSize);
    }

    public Vector3 SnapWorldPosition(Vector3 worldPosition)
    {
        Vector2I cell = WorldToCell(worldPosition);
        return CellToWorld(cell, worldPosition.Y);
    }

    public bool IsCellInside(Vector2I cell)
    {
        return cell.X >= -HalfExtents.X
            && cell.X <= HalfExtents.X
            && cell.Y >= -HalfExtents.Y
            && cell.Y <= HalfExtents.Y;
    }

    public bool CanPlaceObstacle(Vector2I originCell, Vector2I size)
    {
        foreach (Vector2I cell in GetOccupiedCells(originCell, size))
        {
            if (!IsCellInside(cell) || _obstacles.ContainsKey(cell))
            {
                return false;
            }
        }

        return true;
    }

    public bool RegisterObstacle(GridObstacle obstacle)
    {
        if (obstacle == null)
        {
            return false;
        }

        Vector2I originCell = WorldToCell(obstacle.GlobalPosition);
        if (!CanPlaceObstacle(originCell, obstacle.CellSize))
        {
            return false;
        }

        foreach (Vector2I cell in GetOccupiedCells(originCell, obstacle.CellSize))
        {
            _obstacles[cell] = obstacle;
        }

        obstacle.RegisteredOriginCell = originCell;
        return true;
    }

    public void UnregisterObstacle(GridObstacle obstacle)
    {
        if (obstacle == null)
        {
            return;
        }

        List<Vector2I> toRemove = new();
        foreach (KeyValuePair<Vector2I, GridObstacle> entry in _obstacles)
        {
            if (entry.Value == obstacle)
            {
                toRemove.Add(entry.Key);
            }
        }

        foreach (Vector2I cell in toRemove)
        {
            _obstacles.Remove(cell);
        }
    }

    public GridObstacle GetObstacleAt(Vector2I cell)
    {
        _obstacles.TryGetValue(cell, out GridObstacle obstacle);
        return obstacle;
    }

    public bool TryFindPath(
        Vector3 startWorld,
        Vector3 targetWorld,
        float obstacleHealthCostScale,
        out List<Vector3> path)
    {
        Vector2I start = WorldToCell(startWorld);
        Vector2I target = WorldToCell(targetWorld);
        List<Vector2I> cellPath = FindCellPath(start, target, Mathf.Max(0.0f, obstacleHealthCostScale));

        path = new List<Vector3>();
        if (cellPath.Count == 0)
        {
            return false;
        }

        float y = startWorld.Y;
        for (int i = 1; i < cellPath.Count; i++)
        {
            path.Add(CellToWorld(cellPath[i], y));
        }

        return true;
    }

    private List<Vector2I> FindCellPath(Vector2I start, Vector2I target, float obstacleHealthCostScale)
    {
        if (!IsCellInside(start) || !IsCellInside(target))
        {
            return new List<Vector2I>();
        }

        PriorityQueue<Vector2I, float> openSet = new();
        Dictionary<Vector2I, Vector2I> cameFrom = new();
        Dictionary<Vector2I, float> costSoFar = new()
        {
            [start] = 0.0f,
        };

        openSet.Enqueue(start, 0.0f);

        while (openSet.Count > 0)
        {
            Vector2I current = openSet.Dequeue();
            if (current == target)
            {
                return ReconstructPath(cameFrom, current);
            }

            foreach (Vector2I next in GetNeighbors(current))
            {
                if (!IsCellInside(next))
                {
                    continue;
                }

                GridObstacle obstacle = GetObstacleAt(next);
                if (obstacle != null && !obstacle.IsBreakable)
                {
                    continue;
                }

                float newCost = costSoFar[current] + MoveCost(current, next) + GetObstacleCost(obstacle, obstacleHealthCostScale);
                if (!costSoFar.TryGetValue(next, out float existingCost) || newCost < existingCost)
                {
                    costSoFar[next] = newCost;
                    cameFrom[next] = current;
                    openSet.Enqueue(next, newCost + Heuristic(next, target));
                }
            }
        }

        return new List<Vector2I>();
    }

    private IEnumerable<Vector2I> GetNeighbors(Vector2I cell)
    {
        foreach (Vector2I direction in CardinalDirections)
        {
            yield return cell + direction;
        }

        if (!AllowDiagonalMovement)
        {
            yield break;
        }

        foreach (Vector2I direction in DiagonalDirections)
        {
            yield return cell + direction;
        }
    }

    private IEnumerable<Vector2I> GetOccupiedCells(Vector2I originCell, Vector2I size)
    {
        Vector2I safeSize = new(Mathf.Max(1, size.X), Mathf.Max(1, size.Y));
        Vector2I offset = new(safeSize.X / 2, safeSize.Y / 2);

        for (int x = 0; x < safeSize.X; x++)
        {
            for (int y = 0; y < safeSize.Y; y++)
            {
                yield return new Vector2I(originCell.X + x - offset.X, originCell.Y + y - offset.Y);
            }
        }
    }

    private float GetObstacleCost(GridObstacle obstacle, float obstacleHealthCostScale)
    {
        if (obstacle == null)
        {
            return 0.0f;
        }

        return Mathf.Max(0.0f, obstacle.CurrentHealth) * obstacleHealthCostScale;
    }

    private float MoveCost(Vector2I from, Vector2I to)
    {
        return from.X != to.X && from.Y != to.Y ? 1.4142135f : 1.0f;
    }

    private float Heuristic(Vector2I from, Vector2I to)
    {
        int dx = Mathf.Abs(from.X - to.X);
        int dy = Mathf.Abs(from.Y - to.Y);
        return AllowDiagonalMovement ? Mathf.Max(dx, dy) : dx + dy;
    }

    private List<Vector2I> ReconstructPath(Dictionary<Vector2I, Vector2I> cameFrom, Vector2I current)
    {
        List<Vector2I> path = new() { current };
        while (cameFrom.TryGetValue(current, out Vector2I previous))
        {
            current = previous;
            path.Add(current);
        }

        path.Reverse();
        return path;
    }
}

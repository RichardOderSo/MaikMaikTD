using Godot;
using System;
using System.Collections.Generic;

public partial class GridManager : Node3D
{
    public static GridManager Instance { get; private set; }

    [Export]
    public float CellSize { get; set; } = 1.0f;

    [Export]
    public float VerticalCellSize { get; set; } = 1.0f;

    [Export]
    public Vector2I HalfExtents { get; set; } = new Vector2I(50, 50);

    [Export]
    public bool AllowDiagonalMovement { get; set; } = false;

    private readonly Dictionary<Vector3I, GridObstacle> _obstacles = new();

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

    // Converts world coordinates to grid cell indices
    public Vector3I WorldToCell(Vector3 worldPosition)
    {
        Vector3 local = worldPosition - GlobalPosition;
        return new Vector3I(
            Mathf.RoundToInt(local.X / CellSize),
            Mathf.RoundToInt(local.Y / VerticalCellSize),
            Mathf.RoundToInt(local.Z / CellSize));
    }

    // Converts grid cell indices back to world space
    public Vector3 CellToWorld(Vector3I cell)
    {
        return new Vector3(
            GlobalPosition.X + cell.X * CellSize,
            GlobalPosition.Y + cell.Y * VerticalCellSize,
            GlobalPosition.Z + cell.Z * CellSize);
    }

    // Returns the center of the cell for any given world position
    public Vector3 SnapWorldPosition(Vector3 worldPosition)
    {
        Vector3I cell = WorldToCell(worldPosition);
        return CellToWorld(cell);
    }

    // Bounds check to make sure buildings stay within the playable area
    public bool IsCellInside(Vector3I cell)
    {
        return cell.X >= -HalfExtents.X
            && cell.X <= HalfExtents.X
            && cell.Z >= -HalfExtents.Y
            && cell.Z <= HalfExtents.Y;
    }

    public bool CanPlaceObstacle(Vector3I originCell, Vector2I size)
    {
        foreach (Vector3I cell in GetOccupiedCells(originCell, size))
        {
            if (!IsCellInside(cell))
            {
                return false;
            }

            // Simple check: don't allow stacking buildings on top of each other
            foreach (var obstacleCell in _obstacles.Keys)
            {
                if (obstacleCell.X == cell.X && obstacleCell.Z == cell.Z)
                {
                    return false;
                }
            }
        }

        return true;
    }

    // Records a building on the grid so monsters know to path around it
    public bool RegisterObstacle(GridObstacle obstacle)
    {
        if (obstacle == null)
        {
            return false;
        }

        Vector3I originCell = WorldToCell(obstacle.GlobalPosition);
        if (!CanPlaceObstacle(originCell, obstacle.CellSize))
        {
            return false;
        }

        foreach (Vector3I cell in GetOccupiedCells(originCell, obstacle.CellSize))
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

        // Clean up all cells associated with this obstacle
        List<Vector3I> toRemove = new();
        foreach (KeyValuePair<Vector3I, GridObstacle> entry in _obstacles)
        {
            if (entry.Value == obstacle)
            {
                toRemove.Add(entry.Key);
            }
        }

        foreach (Vector3I cell in toRemove)
        {
            _obstacles.Remove(cell);
        }
    }

    public GridObstacle GetObstacleAt(Vector3I cell)
    {
        _obstacles.TryGetValue(cell, out GridObstacle obstacle);
        return obstacle;
    }

    // Standard A* pathfinding for monsters
    public bool TryFindPath(
        Vector3 startWorld,
        Vector3 targetWorld,
        float obstacleHealthCostScale,
        out List<Vector3> path)
    {
        Vector3I start = WorldToCell(startWorld);
        Vector3I target = WorldToCell(targetWorld);
        List<Vector3I> cellPath = FindCellPath(start, target, Mathf.Max(0.0f, obstacleHealthCostScale));

        path = new List<Vector3>();
        if (cellPath.Count == 0)
        {
            return false;
        }

        // Convert cell results back to world coordinates for navigation
        for (int i = 1; i < cellPath.Count; i++)
        {
            path.Add(CellToWorld(cellPath[i]));
        }

        return true;
    }

    private List<Vector3I> FindCellPath(Vector3I start, Vector3I target, float obstacleHealthCostScale)
    {
        if (!IsCellInside(start) || !IsCellInside(target))
        {
            return new List<Vector3I>();
        }

        PriorityQueue<Vector3I, float> openSet = new();
        Dictionary<Vector3I, Vector3I> cameFrom = new();
        Dictionary<Vector3I, float> costSoFar = new()
        {
            [start] = 0.0f,
        };

        openSet.Enqueue(start, 0.0f);

        while (openSet.Count > 0)
        {
            Vector3I current = openSet.Dequeue();
            if (current == target)
            {
                return ReconstructPath(cameFrom, current);
            }

            foreach (Vector3I next in GetNeighbors(current))
            {
                if (!IsCellInside(next))
                {
                    continue;
                }

                GridObstacle obstacle = GetObstacleAt(next);
                // Unbreakable obstacles are true walls
                if (obstacle != null && !obstacle.IsBreakable)
                {
                    continue;
                }

                // Monsters can choose to break through buildings if the path is too long
                float newCost = costSoFar[current] + MoveCost(current, next) + GetObstacleCost(obstacle, obstacleHealthCostScale);
                if (!costSoFar.TryGetValue(next, out float existingCost) || newCost < existingCost)
                {
                    costSoFar[next] = newCost;
                    cameFrom[next] = current;
                    openSet.Enqueue(next, newCost + Heuristic(next, target));
                }
            }
        }

        return new List<Vector3I>();
    }

    private IEnumerable<Vector3I> GetNeighbors(Vector3I cell)
    {
        foreach (Vector2I direction in CardinalDirections)
        {
            yield return cell + new Vector3I(direction.X, 0, direction.Y);
        }

        if (!AllowDiagonalMovement)
        {
            yield break;
        }

        foreach (Vector2I direction in DiagonalDirections)
        {
            yield return cell + new Vector3I(direction.X, 0, direction.Y);
        }
    }

    // Returns all cells covered by a building of a certain size
    private IEnumerable<Vector3I> GetOccupiedCells(Vector3I originCell, Vector2I size)
    {
        Vector2I safeSize = new(Mathf.Max(1, size.X), Mathf.Max(1, size.Y));
        Vector2I offset = new(safeSize.X / 2, safeSize.Y / 2);

        for (int x = 0; x < safeSize.X; x++)
        {
            for (int z = 0; z < safeSize.Y; z++)
            {
                yield return new Vector3I(originCell.X + x - offset.X, originCell.Y, originCell.Z + z - offset.Y);
            }
        }
    }

    private float GetObstacleCost(GridObstacle obstacle, float obstacleHealthCostScale)
    {
        if (obstacle == null)
        {
            return 0.0f;
        }

        // Higher health buildings are harder to break through, so monsters prefer going around them
        return Mathf.Max(0.0f, obstacle.CurrentHealth) * obstacleHealthCostScale;
    }

    private float MoveCost(Vector3I from, Vector3I to)
    {
        // Pythagorean distance for diagonal movement (approx sqrt(2))
        return from.X != to.X && from.Z != to.Z ? 1.4142135f : 1.0f;
    }

    private float Heuristic(Vector3I from, Vector3I to)
    {
        int dx = Mathf.Abs(from.X - to.X);
        int dz = Mathf.Abs(from.Z - to.Z);
        // Use Chebyshev for diagonals, Manhattan for cardinal-only
        return AllowDiagonalMovement ? Mathf.Max(dx, dz) : dx + dz;
    }

    private List<Vector3I> ReconstructPath(Dictionary<Vector3I, Vector3I> cameFrom, Vector3I current)
    {
        List<Vector3I> path = new() { current };
        while (cameFrom.TryGetValue(current, out Vector3I previous))
        {
            current = previous;
            path.Add(current);
        }

        path.Reverse();
        return path;
    }
}

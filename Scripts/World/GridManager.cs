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

	public Vector3I WorldToCell(Vector3 worldPosition, bool ignoreHeight = false)
	{
		Vector3 local = worldPosition - GlobalPosition;
		return new Vector3I(
			Mathf.RoundToInt(local.X / CellSize),
			ignoreHeight == true ? 0 : Mathf.RoundToInt(local.Y / VerticalCellSize),
			Mathf.RoundToInt(local.Z / CellSize));
	}

	public Vector3 CellToWorld(Vector3I cell, bool ignoreHeight = false)
	{
		return new Vector3(
			GlobalPosition.X + cell.X * CellSize,
			ignoreHeight == true ? 0 : GlobalPosition.Y + cell.Y * VerticalCellSize,
			GlobalPosition.Z + cell.Z * CellSize);
	}

	public Vector3 SnapWorldPosition(Vector3 worldPosition)
	{
		Vector3I cell = WorldToCell(worldPosition);
		return CellToWorld(cell);
	}

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

	public IEnumerable<Vector3I> GetNeighbors(Vector3I cell)
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

	public float GetObstacleCost(GridObstacle obstacle, float obstacleHealthCostScale)
	{
		if (obstacle == null)
		{
			return 0.0f;
		}

		return Mathf.Max(0.0f, obstacle.CurrentHealth) * obstacleHealthCostScale;
	}

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

}

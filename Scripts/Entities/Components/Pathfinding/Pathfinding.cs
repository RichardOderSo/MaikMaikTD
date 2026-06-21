using Godot;
using System;
using System.Collections.Generic;


public partial class Pathfinding : NavigationAgent3D
{
	public enum PathType {
		BREAK_OBSTACLE,
		AVOID_OBSTACLE,
		UNREACHABLE

	}
	[Export]
	public bool IgnoreHeight = true;
	protected GridManager _grid = null;
	protected Queue<Vector3> _subTargets = new();
	protected Vector3 _currentSubTarget;

	public override void _Ready() {
		_grid = GridManager.Instance ?? GetTree().CurrentScene?.FindChild("GridManager", true, false) as GridManager;
	}

	public override void _Process(double delta) {
	}

	public bool TryFindPath(
		Vector3 startWorld,
		Vector3 targetWorld,
		float obstacleHealthCostScale) {

		Vector3I start = _grid.WorldToCell(startWorld, IgnoreHeight);
		Vector3I target = _grid.WorldToCell(targetWorld, IgnoreHeight);

		if (BuildPath(start, target, obstacleHealthCostScale)){
			GD.Print("Path Found");
			SetTarget(_subTargets.Dequeue());

			return true;
		}

		return false;
	}

	public bool CheckTargetReached() {
		if(IsTargetReached() && _subTargets.Count == 0){
			return true;
		}

		if(IsTargetReached() && _subTargets.Count != 0){
			SubTargetReached();
			return false;
		}

		return false;
	}

	private void SetTarget(Vector3 newTarget) {
		TargetPosition = newTarget;
		GD.Print("Target set to {}", newTarget);
	}

	private void SubTargetReached(){
		//TODO: This needs to send a signal to its parent so it knows to attack
		
		SetTarget(_subTargets.Dequeue());
		GD.Print("SubTargetReached");
	}

	private bool BuildPath(Vector3I start, Vector3I target, float obstacleHealthCostScale) {
		if (!_grid.IsCellInside(start) || !_grid.IsCellInside(target)) {
			return false;
		}

		PriorityQueue<Vector3I, float> openSet = new();
		Dictionary<Vector3I, Vector3I> cameFrom = new();
		Dictionary<Vector3I, float> costSoFar = new()
		{
			[start] = 0.0f,
		};

		openSet.Enqueue(start, 0.0f);
		GD.Print(openSet);

		while (openSet.Count > 0) {
			Vector3I current = openSet.Dequeue();

			if (current == target) {
				FindSubTargets(cameFrom, current);
				return true;
			}

			foreach (Vector3I next in _grid.GetNeighbors(current)) {
				if (!_grid.IsCellInside(next)) {
					continue;
				}

				GridObstacle obstacle = _grid.GetObstacleAt(next);
				if (obstacle != null && !obstacle.IsBreakable) {
					continue;
				}

				float newCost = costSoFar[current] + MoveCost(current, next) + _grid.GetObstacleCost(obstacle, obstacleHealthCostScale);
				if (!costSoFar.TryGetValue(next, out float existingCost) || newCost < existingCost) {
					costSoFar[next] = newCost;
					cameFrom[next] = current;
					openSet.Enqueue(next, newCost + Heuristic(next, target));
				}
			}
		}

		return false;
	}

	private float MoveCost(Vector3I from, Vector3I to) {
		return from.X != to.X && from.Y != to.Y ? 1.4142135f : 1.0f;
	}

	private float Heuristic(Vector3I from, Vector3I to) {
		int dx = Mathf.Abs(from.X - to.X);
		int dy = Mathf.Abs(from.Y - to.Y);
		return _grid.AllowDiagonalMovement ? Mathf.Max(dx, dy) : dx + dy;
	}


	private bool FindSubTargets(Dictionary<Vector3I, Vector3I> cameFrom, Vector3I current) {
		_subTargets.Clear();
		_subTargets.Enqueue(_grid.CellToWorld(current, IgnoreHeight));

		//Queue all obstacles on Path
		while (cameFrom.TryGetValue(current, out Vector3I previous))
		{
			current = previous;
			if (_grid.GetObstacleAt(current) != null){
				_subTargets.Enqueue(_grid.CellToWorld(current, IgnoreHeight));
			}
		}
		
		return _subTargets.Count != 1;
	}

}

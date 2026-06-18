using Godot;
using System;
using System.Collections.Generic;

public partial class MonsterBase : CharacterBody3D
{
	[Export]
	public float Speed {get; set;} = 2.0f;

	[Export]
	public Vector3 TargetPosition { get; set; } = Vector3.Zero;

	[Export]
	public float RepathInterval { get; set; } = 0.4f;

	[Export]
	public float WaypointReachDistance { get; set; } = 0.12f;

	[Export]
	public float ObstacleAttackRange { get; set; } = 1.05f;

	[Export]
	public int ObstacleDamage { get; set; } = 10;

	[Export]
	public float ObstacleAttackInterval { get; set; } = 1.0f;

	[Export]
	public float ObstacleHealthCostScale { get; set; } = 0.1f;

	protected Health _health = null;
	protected NavigationAgent3D _navAgent = null; 
	protected Pathfinding _pathfinding = null; 
	protected GridManager _grid = null;

	private readonly List<Vector3> _gridPath = new();
	private int _pathIndex;
	private float _timeUntilRepath;
	private float _timeUntilObstacleAttack;


	protected virtual void FindTarget() {
		//TODO decide on base targeting functionality
		if (_pathfinding != null) {
			_pathfinding.TryFindPath(GlobalPosition, TargetPosition, ObstacleHealthCostScale);

			return;
		}
	}
	
	public override void _Ready() {
		_health = GetNode<Health>("Health");
		_pathfinding = GetNodeOrNull<Pathfinding>("Pathfinding");
		FindTarget();
	}

	public override void _PhysicsProcess(double delta) {
		FollowPathfinding(delta);
	}

	private void FollowPathfinding(double delta){
		if (_pathfinding == null) {
			return;
		}

		Vector3 velocity = Velocity;

		if (!IsOnFloor()) {
			velocity += GetGravity() * (float)delta;
		}

		if (_pathfinding.CheckTargetReached()) {
			return;
		}

		if (!_pathfinding.IsTargetReachable()) {
			GD.Print("Target not Reachable, something went terribly wrong idiot");
			return;
		}

		Vector3 nextPosition = _pathfinding.GetNextPathPosition();
		velocity = GlobalPosition.DirectionTo(nextPosition) * Speed;

		Velocity = velocity;
		MoveAndSlide();
	}

	private void AttackObstacle(GridObstacle obstacle, double delta) {
		//TODO should be called when pathfinding sends signal
		//TODO should send signal to all Monsters to recalculate paths
		_timeUntilObstacleAttack -= (float)delta;
		if (_timeUntilObstacleAttack > 0.0f) {
			return;
		}

		_timeUntilObstacleAttack = ObstacleAttackInterval;
		obstacle.ApplyDamage(ObstacleDamage);
	}
}

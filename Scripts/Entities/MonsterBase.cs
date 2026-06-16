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
    protected NavigationAgent3D _pathfinding = null; 
    protected GridManager _grid = null;

    private readonly List<Vector3> _gridPath = new();
    private int _pathIndex;
    private float _timeUntilRepath;
    private float _timeUntilObstacleAttack;


    protected virtual void FindTarget() {
        //TODO decide on base targeting functionality
        if (_pathfinding != null) {
            _pathfinding.TargetPosition = TargetPosition;
        }
    }

	
	public override void _Ready() {
		_health = GetNode<Health>("Health");
        _pathfinding = GetNodeOrNull<NavigationAgent3D>("Pathfinding");
        _grid = GridManager.Instance ?? GetTree().CurrentScene?.FindChild("GridManager", true, false) as GridManager;
        FindTarget();
        RebuildGridPath();
	}

	public override void _PhysicsProcess(double delta) {
        if (_grid != null) {
            FollowGridPath(delta);
            return;
        }

        FollowNavigationAgent(delta);
	}

    private void FollowGridPath(double delta) {
		Vector3 velocity = Velocity;

		// Add the gravity.
		if (!IsOnFloor()) {
			velocity += GetGravity() * (float)delta;
		}

        _timeUntilRepath -= (float)delta;
        if (_timeUntilRepath <= 0.0f) {
            RebuildGridPath();
        }

        if (GlobalPosition.DistanceTo(TargetPosition) <= WaypointReachDistance) {
            Velocity = new Vector3(0.0f, velocity.Y, 0.0f);
            MoveAndSlide();
            return;
        }

        if (_gridPath.Count == 0 || _pathIndex >= _gridPath.Count) {
            Velocity = new Vector3(0.0f, velocity.Y, 0.0f);
            MoveAndSlide();
            return;
        }

        Vector3 nextPosition = _gridPath[_pathIndex];
        GridObstacle obstacle = _grid.GetObstacleAt(_grid.WorldToCell(nextPosition));
        if (obstacle != null) {
            if (GlobalPosition.DistanceTo(nextPosition) <= ObstacleAttackRange) {
                AttackObstacle(obstacle, delta);
                Velocity = new Vector3(0.0f, velocity.Y, 0.0f);
                MoveAndSlide();
                return;
            }
        }
        else if (GlobalPosition.DistanceTo(nextPosition) <= WaypointReachDistance) {
            _pathIndex++;
            return;
        }

        Vector3 direction = GlobalPosition.DirectionTo(nextPosition);
        direction.Y = 0.0f;
        if (direction.LengthSquared() > 0.0001f) {
            direction = direction.Normalized();
        }

        velocity.X = direction.X * Speed;
        velocity.Z = direction.Z * Speed;

		Velocity = velocity;
		MoveAndSlide();
    }

    private void FollowNavigationAgent(double delta) {
        if (_pathfinding == null) {
            return;
        }

        Vector3 velocity = Velocity;

        if (!IsOnFloor()) {
            velocity += GetGravity() * (float)delta;
        }

        if (_pathfinding.IsTargetReached()) {
            return;
        }

        if (!_pathfinding.IsTargetReachable()) {
            GD.Print("Target not Reachable");
            return;
        }

        Vector3 nextPosition = _pathfinding.GetNextPathPosition();
        velocity = GlobalPosition.DirectionTo(nextPosition) * Speed;

		Velocity = velocity;
		MoveAndSlide();
    }

    private void RebuildGridPath() {
        _timeUntilRepath = RepathInterval;
        if (_grid == null) {
            return;
        }

        if (_grid.TryFindPath(GlobalPosition, TargetPosition, ObstacleHealthCostScale, out List<Vector3> newPath)) {
            _gridPath.Clear();
            _gridPath.AddRange(newPath);
            _pathIndex = 0;
        } else {
            _gridPath.Clear();
            _pathIndex = 0;
        }
    }

    private void AttackObstacle(GridObstacle obstacle, double delta) {
        _timeUntilObstacleAttack -= (float)delta;
        if (_timeUntilObstacleAttack > 0.0f) {
            return;
        }

        _timeUntilObstacleAttack = ObstacleAttackInterval;
        obstacle.ApplyDamage(ObstacleDamage);
        RebuildGridPath();
    }
}

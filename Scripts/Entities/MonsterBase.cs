using Godot;
using System;

public partial class MonsterBase : CharacterBody3D
{
	[Export]
	public float Speed {get; set;} = 2.0f;

	protected Health _health = null;
    protected NavigationAgent3D _pathfinding = null; 


    protected virtual void FindTarget() {
        //TODO decide on base targeting functionality
        _pathfinding.TargetPosition = Vector3.Zero;
    }

	
	public override void _Ready() {
		_health = GetNode<Health>("Health");
        _pathfinding = GetNode<NavigationAgent3D>("Pathfinding");
        FindTarget();
	}

	public override void _PhysicsProcess(double delta) {
		Vector3 velocity = Velocity;

		// Add the gravity.
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
}

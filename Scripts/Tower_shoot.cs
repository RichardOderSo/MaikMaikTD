using Godot;
using System;
using System.Collections.Generic;



public partial class Tower_shoot : StaticBody3D
{


	private float _range = 10.0f;

	[ExportGroup("Tower")]
	[Export]
	public float Range { 
		get => _range;
		set {
			_range = value;
			UpdateRange();
		}
	}

	private Area3D _targetingRange = null;

	LinkedList<Node3D> _enemiesInRange = new LinkedList<Node3D>();
	float _currentShortesDistance = 10000;

	

	private void UpdateRange() {
		_targetingRange.Scale = new Vector3(_range, _range, _range);
	}

	private float CalculateDistance(Node3D body) {
		return (body.Position - this.Position).Length();
	}

	private void BodyEntered(Node3D body) {
		GD.Print("entered");
		_enemiesInRange.AddLast(body);
		GD.Print($"{_enemiesInRange.Count} inside");
	}
	private void BodyExited(Node3D body) {
		GD.Print("exited");
		_enemiesInRange.Remove(body);
		GD.Print($"{_enemiesInRange.Count} remaining");
	}

	private void CalculateClosest(){
		float distance;
		Node3D closest = null;

		if (_enemiesInRange.Count == 0) {return;}
		
		foreach (Node3D enemie in _enemiesInRange) { 
			distance = CalculateDistance(enemie);

			if (distance < _currentShortesDistance) {
				_currentShortesDistance = distance;
				closest = enemie;
			}
		}
		if (closest != null) {
			_enemiesInRange.Remove(closest);
			_enemiesInRange.AddFirst(closest);
		}
	}

	private void ShootClosest() {
		if (_enemiesInRange.Count == 0) {
			GD.Print("nothing in range");
			return;
		}

		Node3D closest = _enemiesInRange.First.Value;
		GD.Print($"closest at {closest.Position}");
	}

			


	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
		_targetingRange = GetNode<Area3D>("Targeting_range");

		_targetingRange.BodyEntered += (body) => BodyEntered(body);
		_targetingRange.BodyExited += (body) => BodyExited(body);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta) {
		CalculateClosest();
		ShootClosest();
	}
}

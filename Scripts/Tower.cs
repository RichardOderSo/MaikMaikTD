using Godot;
using System;
using System.Collections.Generic;
public partial class Tower : StaticBody3D
{

	private float _range = 10.0f;
	private float _firingRate = 10.0f;          //projectiles * min^-1
	private float _projectileSpeed = 0.5f;    //m*s^-1

	[ExportGroup("Tower")]
	[Export]
	public float Range { 
		get => _range;
		set {
			_range = value;
			UpdateRange();
		}
	}
	[Export(PropertyHint.None, "suffix:projectiles/min")]
	public float FiringRate{ 
		get => _firingRate;
		set {
			_firingRate = value;
			if (_fireDelay != null) {UpdateFiringRate();}
		}
	}

	[Export(PropertyHint.None, "suffix:m/s")]
	public float ProjectileSpeed{ 
		get => _projectileSpeed;
		set => _projectileSpeed = value;
	}

	[Export]
	public PackedScene BulletScene { get; set; }


	private Area3D _targetingRange = null;
	private Timer _fireDelay = null;
	private Marker3D _bulletSpawnPoint;

	private LinkedList<Node3D> _enemiesInRange = new LinkedList<Node3D>();
	private float _currentShortesDistance = float.PositiveInfinity;
	private Node3D _currentClosest = null;

	private void UpdateRange() {
		_targetingRange.Scale = new Vector3(_range, _range, _range);
	}

	private void UpdateFiringRate() {
		_fireDelay.WaitTime = 1/(_firingRate/60);
	}
	private float CalculateDistance(Node3D body) {
		return (this.GlobalPosition.DistanceTo(body.GlobalPosition));
	}
	private void CalculateClosest(){
		float distance;
		Node3D closest =  null;

		if (_enemiesInRange.Count == 0) {return;}

		if (CalculateDistance(_currentClosest) > _currentShortesDistance) {
			_currentShortesDistance = float.PositiveInfinity;
		}
		
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
			_currentClosest = closest;
		}
	}
	private void ShootClosest() {
		if (_enemiesInRange.Count == 0) {
			GD.Print("nothing in range");
			return;
		}

		CalculateClosest();

		Node3D closest = _enemiesInRange.First.Value;
		GD.Print($"closest at {closest.Position}");

		Projectile bullet = BulletScene.Instantiate<Projectile>();
		bullet.Instatiate(
				_projectileSpeed,
				_bulletSpawnPoint.GlobalPosition,
				_bulletSpawnPoint.GlobalPosition.DirectionTo(closest.GlobalPosition));

		AddChild(bullet);
	}
	private void BodyEntered(Node3D body) {
		if (_currentClosest == null) { _currentClosest = body;}
		_enemiesInRange.AddLast(body);
	}

	private void BodyExited(Node3D body) {
		if (body == _enemiesInRange.First.Value) {
			_enemiesInRange.Remove(body);
			_currentShortesDistance = float.PositiveInfinity;

			CalculateClosest();
			return;
		}
		_enemiesInRange.Remove(body);
	}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
		_targetingRange = GetNode<Area3D>("TargetingRange");
		_fireDelay = GetNode<Timer>("FireDelay");
		_bulletSpawnPoint = GetNode<Marker3D>("ProjectileSpawnPoint");

		UpdateRange();
		UpdateFiringRate();

		_targetingRange.BodyEntered += (body) => BodyEntered(body);
		_targetingRange.BodyExited += (body) => BodyExited(body);

		_fireDelay.Timeout += () => ShootClosest();
		_fireDelay.Start();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta) {
 	}
}

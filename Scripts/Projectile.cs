using Godot;
using System;

public partial class Projectile : Area3D {
	private uint _penetrations = 0;
	private float _speed;
	private Vector3 _direction;
	private Vector3 _spawnPoint;
	
	public void Instatiate(float speed, Vector3 spawnPoint, Vector3 direction) {
		_speed = speed;
		_spawnPoint = spawnPoint;
		_direction = direction;
	}

	private void OnHit(Node3D body) {
		GD.Print($"Hit {body.Owner}");

		//scedule for deletion if no penetrations left
		if (_penetrations == 0) {
			this.QueueFree();
			return;
		}

	}

	private void OnPenetration() {
		//guard against deletion counting as penetration
		if (_penetrations == 0) {return;}

		_penetrations -= 1;
	}

	public override void _Ready() { 
		GlobalPosition = _spawnPoint;
		BodyEntered += (body) => OnHit(body);
		BodyExited += (body) => OnPenetration();

		//TODO set bullet attributes from json?

	}

	public override void _Process(double delta) { 
		GlobalPosition += _direction * _speed * (float)delta; 
	}
}

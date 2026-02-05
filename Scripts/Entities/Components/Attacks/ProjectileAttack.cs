using Godot;
using System;

[GlobalClass]
public partial class ProjectileAttack : Attack 
{
	[Export]
	public PackedScene ProjectileScene { get; set; }

	[Export(PropertyHint.None, "suffix:m/s")]
	public float ProjectileSpeed {get; set;}

	private Marker3D _bulletSpawnPoint = null;

	public void Attack(Vector3 targetPosition) {
		Projectile bullet = ProjectileScene.Instantiate<Projectile>();
		bullet.Instantiate(
				ProjectileSpeed,
				_bulletSpawnPoint.GlobalPosition,
				_bulletSpawnPoint.GlobalPosition.DirectionTo(targetPosition));

		AddChild(bullet);
	}

	public override void _Ready() {
		_bulletSpawnPoint = GetParent().GetNode<Marker3D>("ProjectileSpawnPoint");
		if (_bulletSpawnPoint == null) { GD.PrintErr("No \"ProjectileSpawnPoint\" Node in parent tree");}
		
	}

}

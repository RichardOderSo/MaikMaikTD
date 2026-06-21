using Godot;
using System;

public partial class Monster : MonsterBase {

	PokeAttack _attack = null;

	public override void _Ready() {
		base._Ready();
		_attack = GetNode<PokeAttack>("PokeAttack");
	}
}

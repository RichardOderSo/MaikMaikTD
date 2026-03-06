using Godot;
using System;

public partial class Monster1 : MonsterBase {

	PokeAttack _attack = null;

	public override void _Ready() {
		base._Ready();
		_attack = GetNode<PokeAttack>("PokeAttack");
	}
}

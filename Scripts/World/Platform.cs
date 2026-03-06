using Godot;
using System;

public partial class Platform : NavigationRegion3D
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
		BakeNavigationMesh();
	}

	public override void _Input(InputEvent @event) {
		if (Input.IsActionJustPressed("debug_bake_navigation")) {
			BakeNavigationMesh();
			return;
		}
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}

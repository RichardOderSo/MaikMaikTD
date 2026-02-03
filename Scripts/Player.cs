using Godot;
using System;

public partial class Player : CharacterBody3D
{

	// Movement
	[Export(PropertyHint.Range, "0, 10, 0.1")]
	public float Speed = 5.0f;

	[Export]
	public float JumpVelocity = 4.5f;

	// Mouse movement
	[Export]
	public float MouseSensitivity = 0.002f;

	private Camera3D _camera;
	private float _pitch = 0f;

	public override void _PhysicsProcess(double delta)
	{
		Vector3 velocity = Velocity;

		// Add the gravity.
		if (!IsOnFloor())
		{
			velocity += GetGravity() * (float)delta;
		}

		// Handle Jump.
		if (Input.IsActionJustPressed("move_jump") && IsOnFloor())
		{
			velocity.Y = JumpVelocity;
		}

		// Get the input direction and handle the movement/deceleration.
		// As good practice, you should replace UI actions with custom gameplay actions.
		Vector2 inputDir = Input.GetVector("move_left", "move_right", "move_forward", "move_back");
		Vector3 direction = (Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();
		if (direction != Vector3.Zero)
		{
			velocity.X = direction.X * Speed;
			velocity.Z = direction.Z * Speed;
		}
		else
		{
			velocity.X = Mathf.MoveToward(Velocity.X, 0, Speed);
			velocity.Z = Mathf.MoveToward(Velocity.Z, 0, Speed);
		}

		Velocity = velocity;
		MoveAndSlide();
	}

	public override void _Ready()
	{
		_camera = GetNode<Camera3D>("Camera3D");
		Input.MouseMode = Input.MouseModeEnum.Captured;
	}

	public override void _Input(InputEvent @event)
	{

		if (Input.IsActionJustPressed("ui_cancel"))
		{
			Input.MouseMode = Input.MouseMode == Input.MouseModeEnum.Captured
				? Input.MouseModeEnum.Visible
				: Input.MouseModeEnum.Captured;
			return; 
		}

		if (Input.MouseMode != Input.MouseModeEnum.Captured) return;

		if(@event is InputEventMouseMotion mm)
		{
			// Rotate player left/right
			RotateY(-mm.Relative.X * MouseSensitivity);

		}
	}

}

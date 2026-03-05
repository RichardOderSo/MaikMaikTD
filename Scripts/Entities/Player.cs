using Godot;

public partial class Player : CharacterBody3D {

	// Movement
	[Export(PropertyHint.Range, "0, 20, 0.1")]
	public float Speed = 5.0f;

	[Export]
	public float JumpVelocity = 4.5f;

	// Mouse look
	[Export]
	public float MouseSensitivity = 0.002f;

	[Export]
	public float MinPitchDeg = -60f;

    [Export]
    public float MaxPitchDeg = 35f;

	private Node3D _cameraPivot;
	private SpringArm3D _springArm;
	private Camera3D _camera;

	private float _pitchRad = 0f;

	public override void _Ready()
	{

		_cameraPivot = GetNode<Node3D>("CameraPivot");
		_springArm = _cameraPivot.GetNode<SpringArm3D>("SpringArm3D");
		_camera = _springArm.GetNode<Camera3D>("Camera3D");

		_camera.MakeCurrent();

		Input.MouseMode = Input.MouseModeEnum.Captured;
	}

	public override void _Input(InputEvent @event)
	{
		// ESC toggle
		if (Input.IsActionJustPressed("ui_cancel"))
		{
			Input.MouseMode =
				Input.MouseMode == Input.MouseModeEnum.Captured
					? Input.MouseModeEnum.Visible
					: Input.MouseModeEnum.Captured;
			return;
		}

		if (Input.MouseMode != Input.MouseModeEnum.Captured)
			return;

		if (@event is InputEventMouseMotion mm)
		{
			float yawDelta = -mm.Relative.X * MouseSensitivity;
			float pitchDelta = -mm.Relative.Y * MouseSensitivity;

			// Yaw player left/right
			RotateY(yawDelta);

			// Pitch up/down
			_pitchRad = Mathf.Clamp(_pitchRad + pitchDelta, Mathf.DegToRad(MinPitchDeg), Mathf.DegToRad(MaxPitchDeg));

			var rot = _cameraPivot.Rotation;
			rot.X = _pitchRad;
			_cameraPivot.Rotation = rot;

		}
	}

	public override void _PhysicsProcess(double delta)
	{
		Vector3 velocity = Velocity;

		// Gravity
		if (!IsOnFloor())
			velocity += GetGravity() * (float)delta;

		// Jump
		if (Input.IsActionJustPressed("move_jump") && IsOnFloor())
			velocity.Y = JumpVelocity;

		// Movement
		Vector2 inputDir = Input.GetVector("move_left", "move_right", "move_forward", "move_back");
		// Movement relative to camera yaw
		Basis camBasis = _camera.GlobalTransform.Basis;

		Vector3 forward = camBasis.Z;
		forward.Y = 0;
		forward = forward.Normalized();
	
		Vector3 right = camBasis.X;
		right.Y = 0;
		right = right.Normalized();

        Vector3 moveDir = (right * inputDir.X + forward * inputDir.Y);
        if (moveDir.LengthSquared() > 0.0001f)
			moveDir = moveDir.Normalized();

		// Speed
		if(moveDir != Vector3.Zero)
		{
			velocity.X = moveDir.X * Speed;
			velocity.Z = moveDir.Z * Speed;
		} else
		{
			velocity.X = Mathf.MoveToward(velocity.X, 0, Speed);
			velocity.Z = Mathf.MoveToward(velocity.Z, 0, Speed);
		}

		Velocity = velocity;
		MoveAndSlide();
	}
}

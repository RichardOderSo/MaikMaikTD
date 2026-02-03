using Godot;

public partial class Player : CharacterBody3D {

    // Movement
    [Export(PropertyHint.Range, "0, 10, 0.1")]
    public float Speed = 5.0f;

    [Export]
    public float JumpVelocity = 4.5f;

    // Mouse look
    [Export]
    public float MouseSensitivity = 0.002f;

    private Camera3D _camera;
    private float _pitch = 0f;

    public override void _Ready()
    {
        _camera = GetNode<Camera3D>("Camera3D");
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
            // Yaw
            RotateY(-mm.Relative.X * MouseSensitivity);

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
        Vector3 direction = (Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();

        if (direction != Vector3.Zero)
        {
            velocity.X = direction.X * Speed;
            velocity.Z = direction.Z * Speed;
        }
        else
        {
            velocity.X = Mathf.MoveToward(velocity.X, 0, Speed);
            velocity.Z = Mathf.MoveToward(velocity.Z, 0, Speed);
        }

        Velocity = velocity;
        MoveAndSlide();

    }
}

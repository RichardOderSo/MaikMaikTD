using Godot;

public partial class GridObstacle : Node3D
{
    [Export]
    public Vector2I CellSize { get; set; } = Vector2I.One;

    [Export]
    public bool IsBreakable { get; set; } = true;

    [Export]
    public NodePath HealthPath { get; set; } = new NodePath("");

    [Export]
    public GridManager Grid { get; set; }

    [Export]
    public bool IsEnabled { get; set; } = true;

    public Vector3I RegisteredOriginCell { get; set; }
    public bool IsRegistered { get; private set; }

    private Health _health;
    private Node3D _obstacleRoot;

    public int CurrentHealth => _health?.CurrentHealth ?? 0;

    // Link the obstacle to its parent building and find the GridManager
    public override void _Ready()
    {
        _obstacleRoot = GetParent<Node3D>() ?? this;

        Grid ??= GridManager.Instance ?? FindGridManager();
        _health = ResolveHealth();

        if (_health != null)
        {
            _health.HealthDepleted += OnHealthDepleted;
        }

        if (IsEnabled)
        {
            Register();
        }
    }

    // Attempt to claim space on the grid
    public void Register()
    {
        if (IsRegistered || Grid == null) return;

        _obstacleRoot ??= GetParent<Node3D>() ?? this;
        GlobalPosition = _obstacleRoot.GlobalPosition;
        if (Grid.RegisterObstacle(this))
        {
            IsRegistered = true;
        }
        else
        {
            GD.PrintErr($"{Name} could not register on the grid at {GlobalPosition}.");
        }
    }

    public void Unregister()
    {
        if (!IsRegistered || Grid == null) return;

        Grid.UnregisterObstacle(this);
        IsRegistered = false;
    }

    public override void _ExitTree()
    {
        Unregister();
    }

    public void ApplyDamage(int amount)
    {
        if (!IsBreakable || _health == null)
        {
            return;
        }

        _health.LoseHealth(amount);
    }

    // Try to find a Health component nearby if none was assigned
    private Health ResolveHealth()
    {
        if (HealthPath != null && !HealthPath.IsEmpty)
        {
            return GetNodeOrNull<Health>(HealthPath);
        }

        if (_obstacleRoot == null) return null;

        return _obstacleRoot.GetNodeOrNull<Health>("Health") ?? _obstacleRoot.FindChild("Health", true, false) as Health;
    }

    private GridManager FindGridManager()
    {
        return GetTree().CurrentScene?.FindChild("GridManager", true, false) as GridManager;
    }

    // When the building dies, free its space on the grid before destroying it
    private void OnHealthDepleted()
    {
        Grid?.UnregisterObstacle(this);
        (_obstacleRoot ?? this).QueueFree();
    }
}

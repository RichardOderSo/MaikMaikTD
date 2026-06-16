using Godot;

public partial class GridObstacle : Node3D
{
    [Export]
    public Vector2I CellSize { get; set; } = Vector2I.One;

    [Export]
    public bool IsBreakable { get; set; } = true;

    [Export]
    public NodePath HealthPath { get; set; }

    [Export]
    public GridManager Grid { get; set; }

    public Vector2I RegisteredOriginCell { get; set; }

    private Health _health;
    private Node3D _obstacleRoot;

    public int CurrentHealth => _health?.CurrentHealth ?? 0;

    public override void _Ready()
    {
        _obstacleRoot = GetParent<Node3D>() ?? this;
        GlobalPosition = _obstacleRoot.GlobalPosition;

        Grid ??= GridManager.Instance ?? FindGridManager();
        _health = ResolveHealth();

        if (_health != null)
        {
            _health.HealthDepleted += OnHealthDepleted;
        }

        if (Grid != null && !Grid.RegisterObstacle(this))
        {
            GD.PrintErr($"{Name} could not register on the grid at {GlobalPosition}.");
        }
    }

    public override void _ExitTree()
    {
        Grid?.UnregisterObstacle(this);
    }

    public void ApplyDamage(int amount)
    {
        if (!IsBreakable || _health == null)
        {
            return;
        }

        _health.LoseHealth(amount);
    }

    private Health ResolveHealth()
    {
        if (!HealthPath.IsEmpty)
        {
            return GetNodeOrNull<Health>(HealthPath);
        }

        return _obstacleRoot.GetNodeOrNull<Health>("Health") ?? _obstacleRoot.FindChild("Health", true, false) as Health;
    }

    private GridManager FindGridManager()
    {
        return GetTree().CurrentScene?.FindChild("GridManager", true, false) as GridManager;
    }

    private void OnHealthDepleted()
    {
        Grid?.UnregisterObstacle(this);
        (_obstacleRoot ?? this).QueueFree();
    }
}

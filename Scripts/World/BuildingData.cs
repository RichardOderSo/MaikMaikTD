using Godot;

// Simple data container for building configurations
[GlobalClass]
public partial class BuildingData : Resource
{
    [Export] public string Name { get; set; }
    [Export] public PackedScene Scene { get; set; }
    [Export] public Vector2I CellSize { get; set; } = Vector2I.One;
    [Export] public Vector3 Scale { get; set; } = Vector3.One;
    [Export] public int Health { get; set; } = 40;
    [Export] public bool IsFence { get; set; } = false;
}

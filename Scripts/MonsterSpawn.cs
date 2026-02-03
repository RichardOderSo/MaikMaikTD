using Godot;
using System;

public partial class MonsterSpawn : Node3D
{

    [Export]
    public PackedScene MonsterScene;
	[Export]
	public Node3D MonsterSpawnPoint;
    [Export(PropertyHint.Range, "0,10,0.1")]
    private float SpawnSpacing = 1.5f;

    private int _spawnCount = 0;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
	}

    public override void _Input(InputEvent @event)
    {
        // Debug spawn
        if (Input.IsActionJustPressed("debug_spawn"))
        {
            SpawnMonster();
        }
    }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

    private void SpawnMonster()
    {
        if (MonsterSpawnPoint == null)
        {
            GD.PrintErr("MonsterSpawnPoint not set!");
            return;
        }

        if (MonsterScene == null)
        {
            GD.PrintErr("MonsterScene not set!");
            return;
        }

        Node3D monster = MonsterScene.Instantiate<Node3D>();

        Vector3 basePos = MonsterSpawnPoint.GlobalPosition;
        Vector3 right = MonsterSpawnPoint.GlobalTransform.Basis.X.Normalized();
        Vector3 offset = right * (_spawnCount * SpawnSpacing);
        Vector3 targetPos = basePos + offset;

        monster.GlobalTransform = new Transform3D(monster.Transform.Basis, targetPos);

        GetTree().CurrentScene.AddChild(monster);
        _spawnCount++;
    }

}

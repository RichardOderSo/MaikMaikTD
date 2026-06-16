using Godot;
using System;

public partial class BuildSystem : Node3D
{

	[Export]
	public PackedScene BuildingScene;

	[Export]
	public float GridSize = 1.0f;

	[Export]
	public Node3D BuildingsParent;

    [Export]
    public GridManager Grid;

    [Export]
    public Vector2I BuildingCellSize = Vector2I.One;

    [Export]
    public int BuildingHealth = 40;

    [Export(PropertyHint.Layers3DPhysics)]
    public uint BuildSurfaceCollisionMask = 2;

	public Vector3 currentBuildPosition;

    private Node3D _previewInstance;
    private bool _canPlaceCurrentPreview;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
        Grid ??= GridManager.Instance ?? GetTree().CurrentScene?.FindChild("GridManager", true, false) as GridManager;
        if (Grid != null)
        {
            GridSize = Grid.CellSize;
        }

		CreatePreview();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{

        UpdateBuildPreview();

        if (_previewInstance != null)
        {
            _previewInstance.GlobalPosition = currentBuildPosition;
        }

    }

	void UpdateBuildPreview()
	{

		// Current active cam
		var camera = GetViewport().GetCamera3D();

		// Mouse position (2D coords)
		var mousePos = GetViewport().GetMousePosition();

		// Calculate the start point in the 3D world
		Vector3 from = camera.ProjectRayOrigin(mousePos);

		// Direction vec
		Vector3 dir = camera.ProjectRayNormal(mousePos);

		// Target: from + dir * length
		Vector3 to = from + dir * 1000f;

		// Get physics space of the current world
		var space = GetWorld3D().DirectSpaceState;

		var query = PhysicsRayQueryParameters3D.Create(from, to);
        query.CollisionMask = BuildSurfaceCollisionMask;

		// Ignore player
		var player = GetNode<CharacterBody3D>("../Player");
		query.Exclude = new Godot.Collections.Array<Rid> { player.GetRid() };

        // Raycast => Dictionary with infos about hit
		var result = space.IntersectRay(query);

		if (result.Count == 0)
        {
            _canPlaceCurrentPreview = false;
            return;
        }

        // Get position of hit
        Vector3 pos = (Vector3)result["position"];

        // Place on grid
        if (Grid != null)
        {
            pos = Grid.SnapWorldPosition(pos);
            _canPlaceCurrentPreview = Grid.CanPlaceObstacle(Grid.WorldToCell(pos), BuildingCellSize);
        }
        else
        {
            pos.X = Mathf.Round(pos.X / GridSize) * GridSize;
            pos.Z = Mathf.Round(pos.Z / GridSize) * GridSize;
            _canPlaceCurrentPreview = true;
        }

        currentBuildPosition = pos;

    }

    // Creates the ghost object
	void CreatePreview()
	{
		if(BuildingScene == null) return;

        // instantiate a copy of the building scene
		_previewInstance = BuildingScene.Instantiate<Node3D>();

        // Adds the preview to the tree
		AddChild( _previewInstance );

		MakePreviewTransparent(_previewInstance );
		DisablePreviewCollisions(_previewInstance );

	}
    public void PlaceObject()
    {
        if (BuildingScene == null || BuildingsParent == null)
            return;

        if (!_canPlaceCurrentPreview)
            return;

        var fence = BuildingScene.Instantiate<Node3D>();
        BuildingsParent.AddChild(fence);
        fence.GlobalPosition = Grid != null ? Grid.SnapWorldPosition(currentBuildPosition) : currentBuildPosition;

        if (Grid != null)
        {
            Vector2I cell = Grid.WorldToCell(fence.GlobalPosition);
            if (!Grid.CanPlaceObstacle(cell, BuildingCellSize))
            {
                fence.QueueFree();
                return;
            }
        }

        EnsureBreakableGridObstacle(fence);
    }

    private void EnsureBreakableGridObstacle(Node3D building)
    {
        Health health = building.GetNodeOrNull<Health>("Health");
        if (health == null)
        {
            health = new Health();
            health.Name = "Health";
            building.AddChild(health);
        }

        health.ChangeMaxHealth(BuildingHealth);
        health.ResetToMaxHealth();

        GridObstacle obstacle = building.GetNodeOrNull<GridObstacle>("GridObstacle");
        if (obstacle == null)
        {
            obstacle = new GridObstacle();
            obstacle.Name = "GridObstacle";
            obstacle.Grid = Grid;
            obstacle.CellSize = BuildingCellSize;
            obstacle.IsBreakable = true;
            building.AddChild(obstacle);
            return;
        }

        obstacle.Grid = Grid;
        obstacle.CellSize = BuildingCellSize;
        obstacle.IsBreakable = true;
    }

    void MakePreviewTransparent(Node node)
    {
        foreach (Node child in node.GetChildren())
        {
            MakePreviewTransparent(child);
        }

        if (node is MeshInstance3D meshInstance)
        {
            var material = new StandardMaterial3D();

            // transparent
            material.AlbedoColor = new Color(1f, 1f, 1f, 0.5f);

            // activate transparency
            material.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;

            // material.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;

            // Overrides material of the copy
            meshInstance.MaterialOverride = material;
        }
    }

    void DisablePreviewCollisions(Node node)
    {
        foreach (Node child in node.GetChildren())
        {
            DisablePreviewCollisions(child);
        }

        if (node is CollisionObject3D collisionObject)
        {
            collisionObject.CollisionLayer = 0;
            collisionObject.CollisionMask = 0;
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("build"))
        {
            PlaceObject();
        }
    }

}

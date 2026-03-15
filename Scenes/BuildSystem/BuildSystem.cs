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

	public Vector3 currentBuildPosition;

    private Node3D _previewInstance;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
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

		// Ignore player
		var player = GetNode<CharacterBody3D>("../Player");
		query.Exclude = new Godot.Collections.Array<Rid> { player.GetRid() };

        // Raycast => Dictionary with infos about hit
		var result = space.IntersectRay(query);

		if (result.Count == 0) return;

        // Get position of hit
        Vector3 pos = (Vector3)result["position"];

        // Place on grid
        pos.X = Mathf.Round(pos.X / GridSize) * GridSize;
        pos.Z = Mathf.Round(pos.Z / GridSize) * GridSize;

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

        var fence = BuildingScene.Instantiate<Node3D>();
        BuildingsParent.AddChild(fence);
        fence.GlobalPosition = currentBuildPosition;
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

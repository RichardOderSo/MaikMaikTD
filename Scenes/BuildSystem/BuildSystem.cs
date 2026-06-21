using Godot;
using System;
using System.Collections.Generic;

/*
 * Manages the building placement system, including grid snapping,
 * placement validation, and ghost preview rendering.
 */
public partial class BuildSystem : Node3D
{
    // Array containing all available buildings that can be constructed.
    [Export]
    public Godot.Collections.Array<BuildingData> Buildings = new();

    // The size of the grid used for placement if no GridManager is present.
    [Export]
    public float GridSize = 1.0f;

    // The parent Node3D where instantiated buildings will be placed in the scene tree.
    [Export]
    public Node3D BuildingsParent;

    // Reference to the GridManager for advanced grid interaction and obstacle registration.
    [Export]
    public GridManager Grid;

    // Collision layers that represent valid ground or surfaces for placing buildings.
    [Export(PropertyHint.Layers3DPhysics)]
    public uint BuildSurfaceCollisionMask = 2;

    // The current calculated position for the building preview.
    public Vector3 currentBuildPosition;

    // The current calculated rotation for the building preview.
    public Basis currentBuildRotation;

    private Node3D _previewInstance;
    private bool _canPlaceCurrentPreview;
    private int _currentBuildingIndex = 0;

    // The currently selected BuildingData from the Buildings array.
    private BuildingData CurrentBuilding => (Buildings.Count > 0) ? Buildings[_currentBuildingIndex] : null;

    /*
     * Called when the node enters the scene tree for the first time.
     * Initializes grid connections and creates the initial building preview.
     */
    public override void _Ready()
    {
        // Link up with the grid manager to keep everything snapped correctly
        Grid ??= GridManager.Instance ?? GetTree().CurrentScene?.FindChild("GridManager", true, false) as GridManager;
        if (Grid != null)
        {
            GridSize = Grid.CellSize;
        }

        CreatePreview();
    }

    /*
     * Called every frame. Updates the building preview position and visual state.
     */
    public override void _Process(double delta)
    {
        UpdateBuildPreview();

        // Update the visual representation of the ghost building
        if (_previewInstance != null)
        {
            _previewInstance.GlobalPosition = currentBuildPosition;
            _previewInstance.GlobalBasis = currentBuildRotation;
            _previewInstance.Scale = CurrentBuilding?.Scale ?? Vector3.One;
            UpdatePreviewMaterial(_previewInstance, _canPlaceCurrentPreview);
        }
    }

    /*
     * Recursively updates the material of the preview instance to visually indicate whether placement is valid.
     */
    private void UpdatePreviewMaterial(Node node, bool canPlace)
    {
        foreach (Node child in node.GetChildren())
        {
            UpdatePreviewMaterial(child, canPlace);
        }

        if (node is MeshInstance3D meshInstance)
        {
            StandardMaterial3D mat = meshInstance.MaterialOverride as StandardMaterial3D;
            if (mat == null)
            {
                // Force a transparent material if it doesn't have an override yet
                mat = new StandardMaterial3D();
                mat.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
                meshInstance.MaterialOverride = mat;
            }

            // Visual feedback: Green for valid, Red for blocked
            Color color = canPlace ? new Color(0, 1, 0, 0.5f) : new Color(1, 0, 0, 0.5f);
            mat.AlbedoColor = color;
        }
    }

    /*
     * Calculates the placement position and rotation using raycasting from the camera.
     * Validates the placement position against the grid and specific building rules.
     */
    void UpdateBuildPreview()
    {
        if (CurrentBuilding == null) return;

        var camera = GetViewport().GetCamera3D();
        var mousePos = GetViewport().GetMousePosition();

        // Raycast from camera through mouse position
        Vector3 from = camera.ProjectRayOrigin(mousePos);
        Vector3 dir = camera.ProjectRayNormal(mousePos);
        Vector3 to = from + dir * 1000f;

        var space = GetWorld3D().DirectSpaceState;
        var query = PhysicsRayQueryParameters3D.Create(from, to);
        query.CollisionMask = BuildSurfaceCollisionMask;

        // Don't let the player block their own placement ray
        var player = GetNode<CharacterBody3D>("../Player");
        query.Exclude = new Godot.Collections.Array<Rid> { player.GetRid() };

        var result = space.IntersectRay(query);

        if (result.Count == 0)
        {
            _canPlaceCurrentPreview = false;
            return;
        }

        Vector3 pos = (Vector3)result["position"];
        Vector3 normal = (Vector3)result["normal"];
        
        // Nudge position out from surface slightly to avoid clipping
        float nudgeAmount = normal.Y > 0.9f ? 0.01f : (Grid != null ? Grid.CellSize * 0.1f : 0.1f);
        Vector3 nudgePos = pos + normal * nudgeAmount;

        // Snap rotation to 90-degree increments based on where player is looking
        float playerYaw = player.GlobalRotation.Y;
        float snappedYaw = Mathf.Round(playerYaw / (Mathf.Pi / 2.0f)) * (Mathf.Pi / 2.0f);
        currentBuildRotation = new Basis(Vector3.Up, snappedYaw);

        if (Grid != null)
        {
            // Snap to grid cells
            currentBuildPosition = Grid.SnapWorldPosition(nudgePos);
            currentBuildPosition.Y = pos.Y; // Keep the original hit height
            
            Vector3I cell = Grid.WorldToCell(currentBuildPosition);
            _canPlaceCurrentPreview = Grid.CanPlaceObstacle(cell, CurrentBuilding.CellSize);

            // Extra rules for fences (enforcing gaps or adjacency)
            if (_canPlaceCurrentPreview && CurrentBuilding.IsFence)
            {
                _canPlaceCurrentPreview = CheckFencePlacementRules(cell, snappedYaw);
            }
        }
        else
        {
            // Fallback basic snapping if no GridManager found
            pos = nudgePos;
            pos.X = Mathf.Round(pos.X / GridSize) * GridSize;
            pos.Z = Mathf.Round(pos.Z / GridSize) * GridSize;
            currentBuildPosition = pos;
            _canPlaceCurrentPreview = true;
        }
    }

    /*
     * Validates placement specifically for fence-type buildings.
     * Fences must either connect seamlessly or leave sufficient pathing gaps.
     */
    private bool CheckFencePlacementRules(Vector3I cell, float rotationYaw)
    {
        // Figure out if the fence is oriented N-S or E-W
        Vector3I longitudinalDir = Mathf.Abs(Mathf.Cos(rotationYaw)) > 0.707f
            ? new Vector3I(0, 0, 1) 
            : new Vector3I(1, 0, 0);
        
        int length = longitudinalDir.X > 0 ? CurrentBuilding.CellSize.X : CurrentBuilding.CellSize.Y;
        length = Mathf.Max(1, length);

        // Fences should either connect perfectly or leave a clear path
        bool isAdjacentForward = IsFenceAt(cell + longitudinalDir * length);
        bool isAdjacentBackward = IsFenceAt(cell - longitudinalDir * length);

        if (isAdjacentForward || isAdjacentBackward) 
        {
            return true;
        }

        // Enforce a minimum gap of 'length' between separate fence sections
        for (int dist = 1; dist < length * 2; dist++)
        {
            if (dist == length) continue; 

            if (IsFenceAt(cell + longitudinalDir * dist) || IsFenceAt(cell - longitudinalDir * dist))
            {
                return false;
            }
        }

        return true;
    }

    /*
     * Checks if a fence exists at the specified grid cell.
     */
    private bool IsFenceAt(Vector3I cell)
    {
        var obstacle = Grid.GetObstacleAt(cell);
        if (obstacle == null) return false;
        
        Node3D parent = obstacle.GetParent<Node3D>();
        return parent != null && parent.IsInGroup("Fences");
    }

    /*
     * Instantiates and configures the ghost preview for the currently selected building.
     */
    void CreatePreview()
    {
        // Clean up old preview before making a new one
        if (_previewInstance != null)
        {
            _previewInstance.QueueFree();
            _previewInstance = null;
        }

        if (CurrentBuilding?.Scene == null) return;

        _previewInstance = CurrentBuilding.Scene.Instantiate<Node3D>();
        AddChild(_previewInstance);
        _previewInstance.TopLevel = true;

        MakePreviewTransparent(_previewInstance);
        DisablePreviewCollisions(_previewInstance);
    }

    /*
     * Cycles to the next building in the available buildings list and updates the preview.
     */
    public void SwitchBuilding()
    {
        if (Buildings.Count == 0) return;
        _currentBuildingIndex = (_currentBuildingIndex + 1) % Buildings.Count;
        CreatePreview();
        GD.Print($"Switched to: {CurrentBuilding.Name}");
    }

    /*
     * Finalizes the placement of the current building if the placement is valid.
     * Instantiates the building and registers it with the GridManager.
     */
    public void PlaceObject()
    {
        if (CurrentBuilding == null || BuildingsParent == null)
            return;

        if (!_canPlaceCurrentPreview)
            return;

        var building = CurrentBuilding.Scene.Instantiate<Node3D>();
        BuildingsParent.AddChild(building);
        
        building.GlobalPosition = currentBuildPosition;
        building.GlobalBasis = currentBuildRotation;
        building.Scale = CurrentBuilding.Scale;

        if (CurrentBuilding.IsFence)
        {
            building.AddToGroup("Fences");
        }

        if (Grid != null)
        {
            Vector3I cell = Grid.WorldToCell(building.GlobalPosition);
            // Final sanity check before locking it in
            if (!Grid.CanPlaceObstacle(cell, CurrentBuilding.CellSize))
            {
                building.QueueFree();
                return;
            }
        }

        // Hook up the components needed for the building to be destructible and block pathfinding
        EnsureBreakableGridObstacle(building);
    }

    /*
     * Ensures that the placed building has Health and GridObstacle components,
     * setting them up dynamically if they are not part of the base scene.
     */
    private void EnsureBreakableGridObstacle(Node3D building)
    {
        // Every building needs a health component
        Health health = building.GetNodeOrNull<Health>("Health") ?? building.FindChild("Health", true, false) as Health;
        if (health == null)
        {
            health = new Health();
            health.Name = "Health";
            building.AddChild(health);
        }

        health.ChangeMaxHealth(CurrentBuilding.Health);
        health.ResetToMaxHealth();

        // Every building needs to register as an obstacle for monsters to path around it
        GridObstacle obstacle = building.GetNodeOrNull<GridObstacle>("GridObstacle") ?? building.FindChild("GridObstacle", true, false) as GridObstacle;
        if (obstacle == null)
        {
            obstacle = new GridObstacle();
            obstacle.Name = "GridObstacle";
            obstacle.Grid = Grid;
            obstacle.CellSize = CurrentBuilding.CellSize;
            obstacle.IsBreakable = true;
            building.AddChild(obstacle);
        }
        else
        {
            obstacle.Grid = Grid;
            obstacle.CellSize = CurrentBuilding.CellSize;
            obstacle.IsBreakable = true;
            obstacle.Register();
        }
    }

    /*
     * Recursively applies a transparent material to the given node to create a ghost effect.
     */
    void MakePreviewTransparent(Node node)
    {
        foreach (Node child in node.GetChildren())
        {
            MakePreviewTransparent(child);
        }

        if (node is MeshInstance3D meshInstance)
        {
            var material = new StandardMaterial3D();
            material.AlbedoColor = new Color(1f, 1f, 1f, 0.5f);
            material.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
            meshInstance.MaterialOverride = material;
        }
    }

    /*
     * Recursively disables all collision shapes and grid obstacle registrations for the preview node.
     */
    void DisablePreviewCollisions(Node node)
    {
        foreach (Node child in node.GetChildren())
        {
            DisablePreviewCollisions(child);
        }

        if (node is CollisionObject3D collisionObject)
        {
            // Previews shouldn't block anything or get hit by attacks
            collisionObject.CollisionLayer = 0;
            collisionObject.CollisionMask = 0;
        }

        if (node is GridObstacle obstacle)
        {
            // Don't register the ghost preview on the pathfinding grid
            obstacle.IsEnabled = false;
        }
    }

    /*
     * Handles unhandled input events for building placement and switching buildings.
     */
    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("build"))
        {
            PlaceObject();
        }
        
        if (Input.IsActionJustPressed("switch_building"))
        {
            SwitchBuilding();
        }
    }
}
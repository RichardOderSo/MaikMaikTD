using Godot;
using System;

[GlobalClass]
public partial class Attack : Node {
    //TODO signal structure for publishing attack to specific enemies
    //Currently attacking needs to be handled by parent this, this might be for the better
    public Attack() : base() {}
    //chains default constructor needed by Godot to custom constructor 
    public Attack(int damage) : this() {Damage = damage;}

    [Export]
    public int Damage {get; protected set;}

    public void UpdateDamage(int amount) => Damage = amount;

     
}

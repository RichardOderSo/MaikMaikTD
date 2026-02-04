using Godot;
using System;

[GlobalClass]
public partial class EntityAttributes : Node {


	public Health Health {get; private set;} = null; 
    public Attack Attack {get; private set;} = null;

    public void InstantiateHealth(int maxHealth) {
        Health = new Health(maxHealth);
    }

    public void InstantiateAttack(int damage) {
        Attack = new Attack(damage);
    }

    public bool isImmortal() => Health == null;
    public bool CanAttack() => Attack != null;


	public override void _Ready() { 
	}

	public override void _Process(double delta)
	{
	}
}
public partial class Health : Node {
	[Signal]
	public delegate void HealthDepletedEventHandler();
    
    public Health() : base() {}
    //chains default constructor needed by Godot to custom constructor 
    public Health(int maxHealth) : this() {MaxHealth = maxHealth;}

	public int MaxHealth {get; private set;}
	public int CurrentHealth {get; private set;}

	public void heal(int amount) {
		int health = CurrentHealth + amount;

		if (health > MaxHealth) { CurrentHealth = MaxHealth;}
		else {CurrentHealth = health;}
	}

	public void takeDamage(int amount) {
		CurrentHealth -= amount;
		if (CurrentHealth < 1) { EmitSignal(SignalName.HealthDepleted);}
	}

    public void ChangeMaxHealth(int amount) {
        MaxHealth = amount;
    }

}

public partial class Attack : Node {
    //TODO signal structure for publishing attack to specific enemies
    public Attack() : base() {}
    //chains default constructor needed by Godot to custom constructor 
    public Attack(int damage) : this() {Damage = damage;}

    public int Damage {get; private set;}
     
}

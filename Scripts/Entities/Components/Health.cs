using Godot;
using System;

[GlobalClass]
public partial class Health : Node {
	[Signal]
	public delegate void HealthDepletedEventHandler();
    
    public Health() : base() {}
    //chains default constructor needed by Godot to custom constructor 
    public Health(int maxHealth) : this() {MaxHealth = maxHealth;}

    [Export]
	public int MaxHealth {get; protected set;}
    [Export]
	public int CurrentHealth {get; protected set;}

	public void Heal(int amount) {
		int health = CurrentHealth + amount;

		if (health > MaxHealth) { CurrentHealth = MaxHealth;}
		else {CurrentHealth = health;}
	}

	public virtual void LoseHealth(int amount) {
		CurrentHealth -= amount;
		if (CurrentHealth < 1) { EmitSignal(SignalName.HealthDepleted);}
	}

    public void ChangeMaxHealth(int amount) => MaxHealth = amount;

}

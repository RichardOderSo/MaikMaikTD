using Godot;
using System;

[GlobalClass]
public partial class ArmoredHealth : Health{

    //TODO fix ArmorValue, currently 1 means no armor
    [Export(PropertyHint.Range, "0,1,0.1")]
    public float ArmorValue {get; protected set;} = 0;

	public override void LoseHealth(int amount) {
		CurrentHealth -= (int)Math.Round(ArmorValue * amount);
		if (CurrentHealth < 1) { EmitSignal(SignalName.HealthDepleted);}
	}


}

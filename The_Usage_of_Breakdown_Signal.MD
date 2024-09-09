# The usage of Signal "Breakdown" .md

This file try to track all Signal Breakdown Consumer found in Vanilla Rimworld Dlls.

These are >

CompPowerBattery
```cs

public override void ReceiveCompSignal(string signal)
{
	if (signal == "Breakdown")
	{
		this.DrawPower(this.StoredEnergy);
	}
}
```
Drains all power.

CompPowerTrader
```cs
public override void ReceiveCompSignal(string signal)
{
	if (signal == "FlickedOff" || signal == "ScheduledOff" || signal == "Breakdown" || signal == "AutoPoweredWantsOff")
	{
		this.PowerOn = false;
	}
	if (signal == "RanOutOfFuel" && this.powerLastOutputted)
	{
		this.PowerOn = false;
	}
	this.UpdateOverlays();
}
```
Stop power trading logic.

What? only these two?
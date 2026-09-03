using System;
using Helpers;

namespace Controllers;

public class FerrisWheelViewBlockingEntity : ViewBlockingEntity
{
	public FerrisWheel ferrisWheel;

	protected override int DefaultLayer => LayerHelper.DefaultLayerIndex;

	public override void Awake()
	{
		base.Awake();
		FerrisWheel obj = ferrisWheel;
		obj.onPlayerEntered = (Action)Delegate.Combine(obj.onPlayerEntered, new Action(OnPlayerEntered));
		FerrisWheel obj2 = ferrisWheel;
		obj2.onPlayerLeft = (Action)Delegate.Combine(obj2.onPlayerLeft, new Action(OnPlayerLeft));
		FerrisWheel obj3 = ferrisWheel;
		obj3.onNpcEntered = (Action<CarnivalPedestrian>)Delegate.Combine(obj3.onNpcEntered, new Action<CarnivalPedestrian>(OnNpcEntered));
	}

	private void OnPlayerEntered()
	{
		SetCameraBlockMode(isOn: false);
		base.enabled = false;
	}

	private void OnPlayerLeft()
	{
		base.enabled = true;
	}

	private void OnNpcEntered(CarnivalPedestrian carnivalPedestrian)
	{
		if (isInCameraBlockMode)
		{
			carnivalPedestrian.skinnedMeshRenderer.enabled = false;
		}
	}

	protected override void OnBlockModeChanged(bool isOn)
	{
		base.OnBlockModeChanged(isOn);
		ToggleNpcsVisibility(!isOn);
	}

	private void ToggleNpcsVisibility(bool toggle)
	{
		FerrisWheelCabin[] cabins = ferrisWheel.cabins;
		for (int i = 0; i < cabins.Length; i++)
		{
			CarnivalPedestrian[] carnivalPedestrians = cabins[i].carnivalPedestrians;
			foreach (CarnivalPedestrian carnivalPedestrian in carnivalPedestrians)
			{
				if (carnivalPedestrian != null)
				{
					carnivalPedestrian.skinnedMeshRenderer.enabled = toggle;
				}
			}
		}
	}
}

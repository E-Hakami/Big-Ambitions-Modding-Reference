using System;
using BigAmbitions.Characters.Appearance;
using BigAmbitions.Items;
using Character;
using Entities;
using UnityEngine;

public class HairdresserCustomer : Customer
{
	[HideInInspector]
	public bool hasHairShampooing;

	[HideInInspector]
	public bool hasAnyHairChange;

	public override void Init()
	{
		base.Init();
		SetHasHairShampooing();
		SetHasAnyHairChange();
		behaviorTree.EnableBehavior();
		if (InstanceBehavior<BuildingManager>.Instance.IsPlayerOwnedBusiness)
		{
			GlobalEvents.onExitBuilding = (Action<Address>)Delegate.Combine(GlobalEvents.onExitBuilding, new Action<Address>(base.OnExitBuilding));
		}
	}

	protected override void SetAppearance()
	{
		tpc.appearanceSetter.RandomizeElements(citizenData.Gender, SkinColorHelper.GetRandom(), citizenData.AppearanceTags);
		RemoveBaldness();
		tpc.appearanceSetter.UpdateVisuals();
	}

	public override void Leave()
	{
		ForceFinishOrder();
		base.Leave();
	}

	protected override void ReleaseGameObject()
	{
		InstanceBehavior<BuildingManager>.Instance.customerSpawner.ReleaseCustomer(this, CustomerType.Hairdresser);
	}

	public void SetHasHairShampooing()
	{
		hasHairShampooing = order.entries.Exists((OrderEntry x) => x.itemName == "ba:itemname_hairshampooingfee");
	}

	public void SetHasAnyHairChange()
	{
		hasAnyHairChange = order.entries.Exists((OrderEntry x) => (ItemsGetter.GetByName(x.itemName).type & ItemType.ServiceProduct) != 0 && x.itemName != "ba:itemname_hairshampooingfee");
	}

	public void RemoveBaldness()
	{
		if (tpc.appearanceSetter.IsBald())
		{
			tpc.appearanceSetter.RandomizeElement(AppearanceElementType.Hair, new AppearanceTag[1] { AppearanceTag.All }, randomizeColor: true, excludeCurrentVariant: true);
		}
	}
}

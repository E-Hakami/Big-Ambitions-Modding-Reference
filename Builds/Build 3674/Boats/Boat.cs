using System.Linq;
using Data.VehicleColors;
using Entities;
using Extensions;
using NaughtyAttributes;
using UnityEngine;

namespace Boats;

public class Boat : MonoBehaviour
{
	public string id;

	public BoatTypeName boatTypeName;

	public BoatData data;

	public bool isPlayerOwned;

	[SerializeField]
	private BoatController controller;

	[SerializeField]
	private BoatColorSetter colorSetter;

	public void Load(BoatData boatData)
	{
		data = boatData;
		if (data == null)
		{
			BoatColor random = InstanceBehavior<GlobalReferences>.Instance.boatColors.GetRandom();
			data = new BoatData
			{
				id = id,
				boatColorName = random.name,
				type = boatTypeName
			};
			isPlayerOwned = false;
			colorSetter.SetColor(random);
			return;
		}
		BoatColor boatColor = InstanceBehavior<GlobalReferences>.Instance.boatColors.FirstOrDefault((BoatColor x) => x.name == data.boatColorName);
		if (boatColor == null)
		{
			Debug.LogError("Boat color " + data.boatColorName + " not found. Setting random color.");
			boatColor = InstanceBehavior<GlobalReferences>.Instance.boatColors.GetRandom();
		}
		isPlayerOwned = true;
		controller.ShowPoi();
		colorSetter.SetColor(boatColor);
	}

	public void SetColor(BoatColor color, bool updateVisuals)
	{
		data.boatColorName = color.name;
		if (updateVisuals)
		{
			colorSetter.SetColor(color);
		}
	}

	[Button(null, EButtonEnableMode.Always)]
	public void GenerateID()
	{
		id = UuidHelper.GenerateBase64Uuid();
	}
}

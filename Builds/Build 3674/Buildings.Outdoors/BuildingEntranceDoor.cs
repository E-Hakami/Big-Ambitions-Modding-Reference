using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Buildings.Outdoors;

[Serializable]
public class BuildingEntranceDoor
{
	[FormerlySerializedAs("doorPosition")]
	public Transform doorTransform;

	public int doorId;

	public bool alwaysOpen;
}

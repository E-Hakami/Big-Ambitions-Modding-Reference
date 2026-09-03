using System;
using UnityEngine;

namespace Vehicles.DeliveryDriverJob;

[Serializable]
public class TipChance
{
	[Range(0f, 1f)]
	public float chance;

	public float tip;
}

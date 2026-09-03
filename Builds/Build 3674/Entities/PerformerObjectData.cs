using System;
using NaughtyAttributes;
using UnityEngine;

namespace Entities;

[Serializable]
public class PerformerObjectData
{
	[HideIf("isUpperChestObject")]
	[AllowNesting]
	public bool isHandObject;

	[HideIf("isHandObject")]
	[AllowNesting]
	public bool isUpperChestObject;

	[ShowIf("isHandObject")]
	[HideIf("isUpperChestObject")]
	[AllowNesting]
	public bool isRightHand;

	public string objectName;

	public Vector3 position;

	public Vector3 rotation;
}

using System;
using UnityEngine;

namespace Controllers;

[Serializable]
public class HandObjectData
{
	public Transform handObject;

	public Transform handObjectParent;

	public Vector3 handObjectPosition;

	public Vector3 handObjectRotation;

	public bool disablePhysicsOnHandObject;

	public float secondsUntilGrabbingObject;
}

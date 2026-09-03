using System.Collections.Generic;
using UnityEngine;

public struct AttachedObjectData
{
	public Transform objectTransform;

	public Renderer renderer;

	public IReadOnlyList<Renderer> shadowCasters;

	public Vector3 position;

	public Quaternion rotation;
}

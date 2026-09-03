using System;
using Enums;
using UnityEngine;

namespace Buildings.Outdoors;

[Serializable]
public class WideSignType
{
	public SignType type;

	public Mesh signMesh;

	public Mesh lightMesh;

	public Material[] lightMaterials;
}

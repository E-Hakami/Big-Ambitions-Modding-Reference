using UnityEngine;
using UnityEngine.UI;

namespace UI.Components;

[DisallowMultipleComponent]
public sealed class RaycastTarget : Graphic
{
	public override void SetMaterialDirty()
	{
	}

	public override void SetVerticesDirty()
	{
	}

	protected override void UpdateMaterial()
	{
	}

	protected override void OnPopulateMesh(VertexHelper vh)
	{
		vh.Clear();
	}
}

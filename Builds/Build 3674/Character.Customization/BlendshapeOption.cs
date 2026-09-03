using System;
using BigAmbitions.Characters.Appearance;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Serialization;

namespace Character.Customization;

[Serializable]
public class BlendshapeOption
{
	[FormerlySerializedAs("header")]
	public string headerKey;

	public AppearanceElementType elementType;

	public bool isAffectingTwoBlendshapes;

	[Header("If affecting one blendshape")]
	[HideIf("isAffectingTwoBlendshapes")]
	public string blendshapeName;

	[Header("If affecting two blendshapes")]
	[ShowIf("isAffectingTwoBlendshapes")]
	public string blendshapeNameLow;

	[ShowIf("isAffectingTwoBlendshapes")]
	public string blendshapeNameHigh;
}

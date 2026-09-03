using System;
using System.Collections.Generic;
using BigAmbitions.Characters.Appearance;
using Extensions;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
public class EmployeePreset
{
	[HideInInspector]
	public string id;

	[HideInInspector]
	public string name;

	public bool skillDependent;

	[ShowIf("skillDependent")]
	public string skill;

	[FormerlySerializedAs("male")]
	public List<AppearanceElementData> maleElements = new List<AppearanceElementData>();

	[FormerlySerializedAs("female")]
	public List<AppearanceElementData> femaleElements = new List<AppearanceElementData>();

	public EmployeePreset()
	{
		id = UuidHelper.GenerateBase64Uuid();
	}
}

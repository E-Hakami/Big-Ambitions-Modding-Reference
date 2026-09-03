using System;
using System.Collections.Generic;
using BigAmbitions.Characters;
using BigAmbitions.Characters.Appearance;
using BigAmbitions.Characters.Skills;
using BigAmbitions.Items;
using UnityEngine;

[Serializable]
public class CharacterData
{
	public string name;

	public int ageInDays;

	public Gender gender;

	public float strength = 0.5f;

	public float fatness = 0.5f;

	public Color32 color;

	public Color32 eyesColor;

	public List<FacialBlendshape> blendshapes = new List<FacialBlendshape>();

	public List<AppearanceElementData> elements = new List<AppearanceElementData>();

	public List<Skill> skills = new List<Skill>();

	public ItemInstance itemInHands;

	[Obsolete("Since EA 0.8")]
	public int skinColor;

	public CharacterData Copy()
	{
		return new CharacterData
		{
			name = name,
			ageInDays = ageInDays,
			gender = gender,
			color = color,
			eyesColor = eyesColor,
			strength = strength,
			fatness = fatness,
			elements = elements.Copy(),
			blendshapes = blendshapes.Copy()
		};
	}
}

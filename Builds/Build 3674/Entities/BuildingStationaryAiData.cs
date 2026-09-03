using System;
using BigAmbitions.Characters.Appearance;
using HGAttributes;
using NaughtyAttributes;

namespace Entities;

[Serializable]
public class BuildingStationaryAiData
{
	public AppearanceTag[] appearanceTags;

	public bool isEmployee;

	[ShowIf("isEmployee")]
	[AutocompleteDropdown("Skills")]
	public string skill;

	public bool useScreen;

	[ShowIf("useScreen")]
	[AllowNesting]
	public VideoClipData.VideoType screenVideoType;
}

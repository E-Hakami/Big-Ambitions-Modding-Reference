using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Blueprints.Compatibility;
using Steamworks.Ugc;
using UnityEngine;

namespace Blueprints;

public static class BlueprintParser
{
	private const string MetadataElementsSplitter = ",";

	private const string MetadataElementInfoSplitter = ":";

	private const string MetadataBuildingSizeSplitter = "#";

	public static async Task<Blueprint> ParseItemToBlueprint(Item workshopItem)
	{
		Blueprint blueprint = new Blueprint
		{
			name = workshopItem.Title,
			thumbnailURL = workshopItem.PreviewImageUrl,
			downloads = workshopItem.NumSubscriptions,
			rating = workshopItem.Score,
			releaseDate = workshopItem.Created,
			metadata = new BlueprintMetadata
			{
				itemId = workshopItem.Id
			},
			ownerId = workshopItem.Owner.Id
		};
		ParseItemMetadataToBlueprint(workshopItem.Metadata, blueprint);
		await BlueprintsFolderLoader.ApplyCompatibilityFixes(blueprint, CompatibilityFixScope.Metadata);
		return blueprint;
	}

	private static void ParseItemMetadataToBlueprint(string metadata, Blueprint blueprint)
	{
		string[] array = Regex.Replace(metadata, "\\s+", "").Split(",");
		foreach (string text in array)
		{
			if (string.IsNullOrEmpty(text))
			{
				continue;
			}
			int num = text.IndexOf(":", StringComparison.Ordinal);
			if (num <= 0)
			{
				continue;
			}
			string text2 = text.Substring(0, num);
			if (!Enum.TryParse(typeof(DataElement), text2, out var result))
			{
				Debug.LogError("Couldn't parse item metadata '" + text2 + "'");
				continue;
			}
			string text3 = text.Substring(num + ":".Length);
			switch ((DataElement)result)
			{
			case DataElement.Price:
			{
				if (float.TryParse(text3, out var result5))
				{
					blueprint.metadata.price = result5;
				}
				else
				{
					Debug.LogError("Couldn't parse item price '" + text3 + "'");
				}
				break;
			}
			case DataElement.BuildingType:
				blueprint.metadata.buildingType = text3;
				break;
			case DataElement.BuildingSize:
			{
				string[] array2 = text3.Split("#");
				string buildingSize = array2[0];
				if (int.TryParse(array2[1], out var result6))
				{
					blueprint.metadata.buildingSizeInfo = new BuildingSizeInfo(buildingSize, result6);
				}
				else
				{
					Debug.LogError("Couldn't parse item building size version '" + text3 + "'");
				}
				break;
			}
			case DataElement.InteriorScore:
			{
				if (float.TryParse(text3, out var result9))
				{
					blueprint.metadata.otherData.Add(new BlueprintDataElement(DataElement.InteriorScore, result9.ToString(CultureInfo.InvariantCulture)));
				}
				else
				{
					Debug.LogError("Couldn't parse interior score '" + text3 + "'");
				}
				break;
			}
			case DataElement.Workstations:
			{
				if (int.TryParse(text3, out var result3))
				{
					blueprint.metadata.otherData.Add(new BlueprintDataElement(DataElement.Workstations, result3.ToString()));
				}
				else
				{
					Debug.LogError("Couldn't parse workstations '" + text3 + "'");
				}
				break;
			}
			case DataElement.PalletShelves:
			{
				if (int.TryParse(text3, out var result8))
				{
					blueprint.metadata.otherData.Add(new BlueprintDataElement(DataElement.PalletShelves, result8.ToString()));
				}
				else
				{
					Debug.LogError("Couldn't parse pallet shelves '" + text3 + "'");
				}
				break;
			}
			case DataElement.StorageShelves:
			{
				if (int.TryParse(text3, out var result2))
				{
					blueprint.metadata.otherData.Add(new BlueprintDataElement(DataElement.StorageShelves, result2.ToString()));
				}
				else
				{
					Debug.LogError("Couldn't parse storage shelves '" + text3 + "'");
				}
				break;
			}
			case DataElement.PointsOfSales:
			{
				if (int.TryParse(text3, out var result10))
				{
					blueprint.metadata.otherData.Add(new BlueprintDataElement(DataElement.PointsOfSales, result10.ToString()));
				}
				else
				{
					Debug.LogError("Couldn't parse points of sales '" + text3 + "'");
				}
				break;
			}
			case DataElement.BusinessTypeName:
				blueprint.metadata.otherData.Add(new BlueprintDataElement(DataElement.BusinessTypeName, text3));
				break;
			case DataElement.BuildNumber:
			{
				if (int.TryParse(text3, out var result7))
				{
					blueprint.metadata.buildNumber = result7;
				}
				else
				{
					Debug.LogError("Couldn't parse build number '" + text3 + "'");
				}
				break;
			}
			case DataElement.BlueprintVersion:
			{
				if (int.TryParse(text3, out var result4))
				{
					blueprint.metadata.blueprintVersion = result4;
				}
				else
				{
					Debug.LogError("Couldn't parse blueprint version '" + text3 + "'");
				}
				break;
			}
			case DataElement.RequiredModIds:
			{
				List<string> requiredModIds = (from x in text3.Split('|')
					where !string.IsNullOrWhiteSpace(x)
					select x).ToList();
				blueprint.metadata.requiredModIds = requiredModIds;
				break;
			}
			}
		}
	}

	public static string ParseBlueprintIntoItemMetadata(Blueprint blueprint)
	{
		List<DataElement> list = blueprint.metadata.otherData.Select((BlueprintDataElement x) => x.dataElement).ToList();
		list.Add(DataElement.Price);
		list.Add(DataElement.BuildingType);
		list.Add(DataElement.BuildingSize);
		list.Add(DataElement.CreatorSteamUsername);
		list.Add(DataElement.BuildNumber);
		list.Add(DataElement.BlueprintVersion);
		return ParseBlueprintIntoItemMetadata(blueprint, list);
	}

	private static string ParseBlueprintIntoItemMetadata(Blueprint blueprint, List<DataElement> dataElements)
	{
		string text = "";
		foreach (DataElement dataElement in dataElements)
		{
			text += dataElement;
			text += ":";
			switch (dataElement)
			{
			case DataElement.Price:
				text += blueprint.metadata.price;
				break;
			case DataElement.BuildingType:
				text += blueprint.metadata.buildingType;
				break;
			case DataElement.BuildingSize:
				text += blueprint.metadata.buildingSizeInfo.buildingSize;
				text += "#";
				text += blueprint.metadata.buildingSizeInfo.buildingVersion;
				break;
			case DataElement.BuildNumber:
				text += blueprint.metadata.buildNumber;
				break;
			case DataElement.BlueprintVersion:
				text += blueprint.metadata.blueprintVersion;
				break;
			default:
				text += blueprint.metadata.otherData.FirstOrDefault((BlueprintDataElement x) => x.dataElement == dataElement)?.value.ToString();
				break;
			}
			text += ",";
		}
		List<string> requiredModIds = blueprint.metadata.requiredModIds;
		if (requiredModIds != null && requiredModIds.Count > 0)
		{
			text = text + DataElement.RequiredModIds.ToString() + ":";
			text += string.Join("|", blueprint.metadata.requiredModIds);
			text += ",";
		}
		return text;
	}
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using BlueprintsUI;
using Buildings;
using HGAttributes;
using UnityEngine;

namespace Blueprints;

[Serializable]
public class BlueprintMetadata
{
	public BlueprintType blueprintType;

	public int blueprintVersion;

	public int buildNumber;

	public ulong itemId;

	public float price;

	[AutocompleteDropdown("BuildingTypes")]
	public string buildingType;

	public BuildingSizeInfo buildingSizeInfo;

	public List<string> requiredModIds = new List<string>();

	public List<BlueprintDataElement> otherData = new List<BlueprintDataElement>();

	public bool isWorkshopReviewPending;

	public bool IsDevBlueprint
	{
		get
		{
			BlueprintType blueprintType = this.blueprintType;
			return blueprintType == BlueprintType.DevInteriorDesign || blueprintType == BlueprintType.DevBusinessLayout || blueprintType == BlueprintType.FeedbackSystem;
		}
	}

	public float FullPriceWithInstallation => price + InteriorInstallationFirmHelper.GetInstallationFee(buildingSizeInfo.buildingSize);

	public async Task Serialize(string path)
	{
		Directory.CreateDirectory(Path.GetDirectoryName(path) ?? string.Empty);
		string contents = JsonUtility.ToJson(this, prettyPrint: true);
		await File.WriteAllTextAsync(path, contents);
	}

	public string GetDataElementValue(DataElement dataElement)
	{
		BlueprintDataElement blueprintDataElement = otherData.Find((BlueprintDataElement x) => x.dataElement == dataElement);
		if (blueprintDataElement == null)
		{
			return string.Empty;
		}
		return blueprintDataElement.value;
	}

	public T GetDataElementValue<T>(DataElement dataElement) where T : struct, Enum
	{
		BlueprintDataElement blueprintDataElement = otherData.Find((BlueprintDataElement x) => x.dataElement == dataElement);
		if (!Enum.TryParse<T>((blueprintDataElement != null) ? blueprintDataElement.value : string.Empty, out var result))
		{
			return default(T);
		}
		return result;
	}

	public void RemoveDataElement(DataElement dataElement)
	{
		otherData.RemoveAll((BlueprintDataElement x) => x.dataElement == dataElement);
	}
}

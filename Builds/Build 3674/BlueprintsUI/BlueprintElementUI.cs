using System;
using BigAmbitions.SaveSystem;
using Blueprints;
using Extensions;
using Localizor;
using Localizor.LanguageChangeEvent;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BlueprintsUI;

public class BlueprintElementUI : MonoBehaviour
{
	[SerializeField]
	private TMP_Text titleLabel;

	[SerializeField]
	private BasicTooltip titleTooltip;

	[SerializeField]
	private Image thumbnail;

	[SerializeField]
	private TextLocalizationComponent downloadsLabel;

	[SerializeField]
	private Image ratingImage;

	[SerializeField]
	private GameObject noSteamInfo;

	[SerializeField]
	private GameObject olderGameVersionWarning;

	[Header("Dev")]
	[SerializeField]
	private GameObject devInfo;

	[SerializeField]
	private TMP_Text buildingTypeLabel;

	[SerializeField]
	private TMP_Text businessTypeLabel;

	public Action<Blueprint> onShowElementInfo;

	private Blueprint _blueprint;

	public void Display(Blueprint blueprint)
	{
		_blueprint?.CleanCachedThumbnail();
		_blueprint = blueprint;
		titleLabel.text = blueprint.name;
		titleTooltip.titleKey = blueprint.name;
		blueprint.ShowThumbnail(thumbnail);
		HandleOlderGameVersionWarning(_blueprint.metadata.buildNumber);
		if (_blueprint.metadata.blueprintType == BlueprintType.SavedLocally)
		{
			noSteamInfo.gameObject.SetActive(value: true);
			return;
		}
		BlueprintType blueprintType = _blueprint.metadata.blueprintType;
		if (blueprintType == BlueprintType.DevBusinessLayout || blueprintType == BlueprintType.DevInteriorDesign || blueprintType == BlueprintType.FeedbackSystem)
		{
			devInfo.gameObject.SetActive(value: true);
			string dataElementValue = _blueprint.metadata.GetDataElementValue(DataElement.BusinessTypeName);
			string text = _blueprint.metadata.buildingType.GetIdWithoutType().CapitalizeFirstChar();
			buildingTypeLabel.SetText(_blueprint.metadata.buildingSizeInfo.ToString() + " (" + text + ")");
			if (_blueprint.metadata.buildingType == "ba:buildingtype_residential")
			{
				businessTypeLabel.transform.parent.gameObject.SetActive(value: false);
			}
			else
			{
				businessTypeLabel.SetText(dataElementValue.GetLocalization());
			}
		}
		else
		{
			blueprint.FetchSteamInfo();
			downloadsLabel.transform.parent.gameObject.SetActive(value: true);
			downloadsLabel.SetData(blueprint.GetDownloadsLabel());
			ratingImage.fillAmount = blueprint.rating;
		}
	}

	public void ShowElementInfo()
	{
		onShowElementInfo?.Invoke(_blueprint);
	}

	private void HandleOlderGameVersionWarning(int buildNumber)
	{
		bool active = GameVersion.IsBuildFromOlderVersionGroup(buildNumber);
		olderGameVersionWarning.SetActive(active);
	}

	private void OnDisable()
	{
		_blueprint?.CleanCachedThumbnail();
		_blueprint = null;
	}
}

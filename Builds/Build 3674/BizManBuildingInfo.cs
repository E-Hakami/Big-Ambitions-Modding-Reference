using System.Linq;
using Entities;
using Extensions;
using Localizor.LanguageChangeEvent;
using NaughtyAttributes;
using Streets;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BizManBuildingInfo : MonoBehaviour
{
	[SerializeField]
	private BizManBusiness bizManBusiness;

	[BoxGroup("UI")]
	[SerializeField]
	private Image buildingImage;

	[BoxGroup("UI")]
	[SerializeField]
	private TextLocalizationComponent address;

	[BoxGroup("UI")]
	[SerializeField]
	private TextLocalizationComponent buildingType;

	[BoxGroup("UI")]
	[SerializeField]
	private TextLocalizationComponent taxesInfo;

	[BoxGroup("UI")]
	[SerializeField]
	private TMP_Text taxesValue;

	private Texture2D _cachedBuildingImageTexture;

	public void ShowInfo()
	{
		address.SetValue(bizManBusiness.address.ToFormattedString(), clearKey: true);
		buildingType.Key = bizManBusiness.building.BuildingType;
		taxesInfo.Arguments = new
		{
			daysInterval = SaveGameManager.Current.gameVariables.daysPerYear
		};
		GenerateBuildingImage();
		RealEstate realEstate = SaveGameManager.Current.realEstate.FirstOrDefault((RealEstate x) => x.address == bizManBusiness.address);
		if (realEstate == null)
		{
			taxesValue.text = "-";
		}
		else
		{
			taxesValue.text = realEstate.TaxesAmount.ToCurrencyFormat();
		}
		base.gameObject.SetActive(value: true);
	}

	private void GenerateBuildingImage()
	{
		CityBuildingController cityBuildingController = InstanceBehavior<CityManager>.Instance.FindCityBuildingController(bizManBusiness.address);
		if (!cityBuildingController)
		{
			return;
		}
		cityBuildingController.GenerateOutsideImage(delegate(ScreenshotCaptureController.CaptureCommand command)
		{
			if (buildingImage.sprite != null)
			{
				Object.Destroy(buildingImage.sprite.texture);
				Object.Destroy(buildingImage.sprite);
			}
			if (_cachedBuildingImageTexture != null)
			{
				Object.Destroy(_cachedBuildingImageTexture);
			}
			buildingImage.sprite = Sprite.Create(command.outputTexture, new Rect(0f, 0f, command.outputTexture.width, command.outputTexture.height), new Vector2(0.5f, 0.5f), 100f, 0u, SpriteMeshType.FullRect);
			_cachedBuildingImageTexture = command.outputTexture;
		});
		buildingImage.color = new Color(1f, 1f, 1f, 1f);
	}
}

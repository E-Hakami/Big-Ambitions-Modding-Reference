using System.Collections.Generic;
using BigAmbitions.Tags;
using Dialogs;
using HGAttributes;
using Helpers;
using NaughtyAttributes;
using RoboRyanTron.SearchableEnum;
using UI.Smartphone.Apps.Contacts;
using UnityEngine;

namespace Buildings;

[CreateAssetMenu(menuName = "BigAmbitions/SpecialService/SpecialService")]
public class SpecialService : ScriptableObject
{
	public string businessName;

	public string businessDescription;

	[AutocompleteDropdown("BusinessTypes")]
	public string businessTypeName;

	public bool hasTaxDeductiblePurchases;

	[ShowIf("IsImporter")]
	public bool productsCanGoOnShortage = true;

	[ShowIf("IsImporter")]
	public bool isRawMaterialsImporter;

	[ShowIf("ShowAiBusinessGoodsSource")]
	public AiBusinessGoodsSource businessGoodsSource;

	public string layout;

	public SignAppearanceSettings signAppearanceSettings;

	public LogoSettings logoSettings;

	[ShowIf("ShowPriceIndex")]
	[Range(10f, 200f)]
	public int priceIndex = 100;

	[Header("Settings")]
	[Expandable]
	public SpecialServiceSettings settings;

	[Header("Other")]
	public bool showForSaleVisualsWhenItemsAreForSale;

	public List<ScheduleDay> scheduleDays;

	public Job playerJob;

	[HideInInspector]
	public List<EmployeePreset> uniforms;

	public Sprite overridePoiIcon;

	[Header("Contact")]
	[SearchableEnum]
	public ContactCategoryName contactCategory;

	[SearchableEnum]
	public CallDialogType dialogType;

	[Tooltip("If true, the contact will be added when the unlock all contacts game variable enabled")]
	public bool isBusinessContact = true;

	private bool ShowPriceIndex()
	{
		return BusinessTypeHelper.GetData(businessTypeName).HasTag(TagRef.Businesstag.hidepriceindex);
	}

	private bool IsImporter()
	{
		return businessTypeName == "ba:businesstype_importexport";
	}

	private bool ShowAiBusinessGoodsSource()
	{
		BusinessType data = BusinessTypeHelper.GetData(businessTypeName);
		if (data == null)
		{
			return false;
		}
		return data.suitableBuildingType == "ba:buildingtype_retail";
	}
}

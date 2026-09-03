using System.Collections.Generic;
using System.Linq;
using BigAmbitions.Tags;
using Buildings;
using Extensions;
using Helpers;
using TMPro;
using UI.Elements;
using UnityEngine;

namespace UI.Dialog;

public class DeliveryContractSettings : MonoBehaviour
{
	[SerializeField]
	private Dropdown businessDropdown;

	[SerializeField]
	private TextMeshProUGUI deliveryPriceLabel;

	public BuildingRegistration selectedBusiness;

	private List<BuildingRegistration> _buildingRegistrations;

	private void Start()
	{
		selectedBusiness = null;
		_buildingRegistrations = BuildingHelper.GetPlayerBuildingRegistrations(PlayerBuildingFilter);
		businessDropdown.SetPlaceholder("dialog_select_business");
		businessDropdown.SetOptions(_buildingRegistrations.Select((BuildingRegistration x) => x.GetDisplayName()).ToList(), localize: false);
		businessDropdown.onOptionSelected.AddListener(SelectBusiness);
		WholesaleStoreSettings wholesaleStoreSettings = (WholesaleStoreSettings)BuildingHelper.GetBuilding(DialogController.current.contact.Address).SpecialService.settings;
		deliveryPriceLabel.text = wholesaleStoreSettings.deliveryFee.ToCurrencyFormat();
	}

	private static bool PlayerBuildingFilter(BuildingRegistration buildingRegistration)
	{
		if (buildingRegistration.businessTypeName != "ba:businesstype_empty")
		{
			return BuildingTypeHelper.GetData(buildingRegistration).HasTag(TagRef.Buildingtypetag.acceptswholesaledeliveries);
		}
		return false;
	}

	private void SelectBusiness(int businessIndex)
	{
		selectedBusiness = _buildingRegistrations[businessIndex];
	}
}

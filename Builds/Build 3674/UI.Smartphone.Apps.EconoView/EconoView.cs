using System.Collections.Generic;
using System.Linq;
using BigAmbitions.Tags;
using Buildings;
using Localizor.LanguageChangeEvent;
using Streets;
using TMPro;
using UI.Elements;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Smartphone.Apps.EconoView;

public class EconoView : MonoBehaviour
{
	public EconoViewOverview overview;

	public EconoViewBusinessDetails businessDetails;

	public TextLocalizationComponent businessTypeLabel;

	public UI.Elements.Dropdown businessNameDropdown;

	public TextMeshProUGUI breadcrumbBusinessNameLabel;

	public Image businessLogo;

	[SerializeField]
	private EconoViewBanking banking;

	private List<BuildingRegistration> _businesses;

	public BuildingRegistration selectedBusiness { get; private set; }

	private void Awake()
	{
		businessNameDropdown.onOptionSelected.AddListener(delegate(int index)
		{
			SetBusiness(_businesses[index]);
		});
	}

	public void SetBusiness(BuildingRegistration registration)
	{
		selectedBusiness = registration;
		overview.gameObject.SetActive(registration == null);
		businessDetails.gameObject.SetActive(registration != null);
		if (registration != null)
		{
			businessTypeLabel.Key = registration.businessTypeName;
			int optionId = _businesses.IndexOf(registration);
			businessNameDropdown.SelectOption(optionId);
			breadcrumbBusinessNameLabel.text = registration.BusinessName;
			businessDetails.RefreshData();
			LoadBusinessLogo();
		}
	}

	private void LoadBusinessLogo()
	{
		Texture2D businessLogoTexture = LogoHelper.GetBusinessLogoTexture(selectedBusiness.BusinessName, LogoSize.SquareSign, playerBusiness: true);
		if (businessLogoTexture == null)
		{
			BusinessLogoGenerator.Create(selectedBusiness.BusinessName, selectedBusiness.logoSettings, LogoHelper.GetPlayerBusinessLogoPath(selectedBusiness.BusinessName), selectedBusiness.RentedByPlayer, delegate
			{
				Texture2D businessLogoTexture2 = LogoHelper.GetBusinessLogoTexture(selectedBusiness.BusinessName, LogoSize.SquareSign, playerBusiness: true);
				if (businessLogoTexture2 != null)
				{
					SetBusinessLogo(businessLogoTexture2);
				}
			});
		}
		else
		{
			SetBusinessLogo(businessLogoTexture);
		}
	}

	private void SetBusinessLogo(Texture2D texture)
	{
		if (businessLogo.sprite != null)
		{
			Object.Destroy(businessLogo.sprite);
		}
		Rect rect = new Rect(0f, 0f, texture.width, texture.height);
		businessLogo.sprite = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f));
	}

	public void ResetSelectedBusiness()
	{
		SetBusiness(null);
	}

	private void OnEnable()
	{
		SetBusiness(null);
		IEnumerable<BuildingRegistration> source = SaveGameManager.Current.BuildingRegistrations.Where((BuildingRegistration x) => x.RentedByPlayer && !BuildingTypeHelper.GetData(x).HasTag(TagRef.Buildingtypetag.containsnobusiness));
		_businesses = source.ToList();
		List<string> newOptions = _businesses.Select((BuildingRegistration x) => (!string.IsNullOrEmpty(x.BusinessName)) ? x.BusinessName : x.Address.ToFormattedString()).ToList();
		businessNameDropdown.SetOptions(newOptions, localize: false);
	}

	public void OpenInBizMan()
	{
		InstanceBehavior<UIs>.Instance.fullMenu.bizMan.Open(selectedBusiness.Address);
	}

	public void OpenTaxes()
	{
		SetBusiness(null);
		banking.ShowTaxes();
	}

	public bool HandleEscapeClick()
	{
		if (!overview.fullTransactionsPanel.isActiveAndEnabled)
		{
			return false;
		}
		overview.fullTransactionsPanel.Close();
		return true;
	}
}

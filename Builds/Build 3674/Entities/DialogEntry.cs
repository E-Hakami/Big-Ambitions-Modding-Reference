using System;
using Localizor.LanguageChangeEvent;
using UnityEngine;

namespace Entities;

public class DialogEntry
{
	public enum InputTemplateName
	{
		None,
		RecruitmentSettings,
		BankInvestment,
		BankLoan,
		RecruitmentDeadline,
		MarketingCampaignSettings,
		DeliveryContractSettings,
		DeliveryContractsList,
		ImportPartnershipSettings,
		AutoTowServiceSettings,
		FurnitureDeliverySettings,
		SlotMachine,
		Roulette,
		RouletteBetSettings,
		Blackjack,
		BlackjackBetSettings,
		RecruitmentCampaignsList,
		HealthInsurancePartnershipSettings,
		PlayerOffer,
		InteriorInstallationFirmDesignSettings,
		SalaryOffer,
		MovingServiceSettings,
		MovingServiceContractsList,
		FurnitureDeliveriesList,
		InstallationContractsList,
		VehicleContractSettings,
		VehicleDeliveryContractsList,
		FoodDeliverySettings,
		FoodDeliveriesList,
		PrivateDriverManageContract,
		PrivateDriverManageVehicles
	}

	public enum TemplateType
	{
		Text,
		Input
	}

	public string headerKey;

	public LanguageChangeEventDataHolder messageData;

	public TemplateType Template;

	public Func<DialogEntry> OnConfirm;

	public Func<DialogEntry> OnSecondOption;

	public Action OnCancel;

	public Action OnForcedCancel;

	public Action OnVisible;

	public InputTemplateName InputTemplate;

	public Sprite Icon;

	public Sprite SecondaryIcon;

	public TextMessage onCancelMessage;

	public LanguageChangeEventDataHolder ConfirmTextOverride;

	public string SecondOptionTextOverride;

	public string CancelTextOverride;

	public void ShowEntry()
	{
		DialogController.current.ShowEntry(this);
	}
}

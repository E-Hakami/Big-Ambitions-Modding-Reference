using System.Collections.Generic;
using System.Linq;
using Buildings;
using Entities;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA09;

public class UpdateMarketEvents : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		TransferItemNameFromOldItemNamesList(gameInstance);
		List<BuildingRegistration> importersBuildingRegistrations = SaveGameManager.Current.BuildingRegistrations.Where((BuildingRegistration x) => x.businessTypeName == "ba:businesstype_importexport").ToList();
		for (int num = gameInstance.marketEvents.Count - 1; num >= 0; num--)
		{
			MarketEvent marketEvent = gameInstance.marketEvents[num];
			MarketEventType type = marketEvent.type;
			if (type == MarketEventType.ProductShortage || type == MarketEventType.ProductBackorder)
			{
				if (marketEvent.startDay > gameInstance.Day)
				{
					gameInstance.marketEvents.RemoveAt(num);
				}
				else
				{
					Address importerAddressContainingMarketEventItem = GetImporterAddressContainingMarketEventItem(gameInstance, importersBuildingRegistrations, marketEvent);
					if (importerAddressContainingMarketEventItem == null)
					{
						gameInstance.marketEvents.RemoveAt(num);
					}
					else
					{
						marketEvent.address = importerAddressContainingMarketEventItem;
						marketEvent.neighbourhood = string.Empty;
					}
				}
			}
		}
	}

	private static void TransferItemNameFromOldItemNamesList(GameInstance gameInstance)
	{
		foreach (MarketEvent marketEvent in gameInstance.marketEvents)
		{
			if (marketEvent.itemNames != null && marketEvent.itemNames.Count != 0)
			{
				marketEvent.itemName = marketEvent.itemNames[0];
				marketEvent.itemNames.Clear();
			}
		}
	}

	private static Address GetImporterAddressContainingMarketEventItem(GameInstance gameInstance, List<BuildingRegistration> importersBuildingRegistrations, MarketEvent marketEvent)
	{
		foreach (BuildingRegistration importersBuildingRegistration in importersBuildingRegistrations)
		{
			SpecialService specialService = importersBuildingRegistration.BuildingCached.SpecialService;
			if (!(specialService == null) && specialService.settings is ImportExportSettings importExportSettings && specialService.productsCanGoOnShortage && importExportSettings.GetItemsAvailable(gameInstance).Contains(marketEvent.itemName))
			{
				return importersBuildingRegistration.Address;
			}
		}
		return null;
	}
}

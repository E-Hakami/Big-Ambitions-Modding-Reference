using UnityEngine;

namespace Helpers;

public class ProductMarketSettings : ScriptableObject
{
	public float productShortagePriceMultiplier = 1.25f;

	public float productBackorderPriceMultiplier = 1.5f;

	public int maxShortagesPerAddress = 5;

	public int maxBackordersPerAddress = 2;

	public int chanceOfShortageOrBackOrder = 66;

	[Tooltip("Only applied in story mode")]
	public int startDayOfShortages = 30;

	[Tooltip("Only applied in story mode")]
	public int startDayOfBackOrders = 60;

	public int daysToTriggerShortageAfterHypeStarts = 7;

	public int daysItTakesForAnItemNotToBeOnSaleToTriggerHypeEvents = 21;

	public int daysWithoutExceedingMaxProvidersToCreateEvent = 15;

	public int startDayToEmptyShelvesWhenThereIsAShortage = 7;

	[Range(0f, 100f)]
	public int percentageOrderedToConsiderALargePurchase = 90;

	[Tooltip("Lowest price a rival store can charge, as a multiple of wholesale cost. At 1 they never sell below cost. That also keeps the price customers accept above what you paid for the stock, even in a price war.")]
	[Range(1f, 3f)]
	public float minimumRivalPriceOverWholesale = 1f;
}

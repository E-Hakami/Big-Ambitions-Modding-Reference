using NaughtyAttributes;
using UnityEngine;

namespace Buildings.BuildingTypes.Special.FoodDelivery;

[CreateAssetMenu(fileName = "FoodDeliverySettings", menuName = "BigAmbitions/FoodDeliverySettings")]
public class FoodDeliverySettings : ScriptableObject
{
	[BoxGroup("Pricing")]
	[Tooltip("Flat fee added on top of every order.")]
	[SerializeField]
	private float deliveryFee = 9.99f;

	[BoxGroup("Pricing")]
	[Tooltip("Item price = default market price times this multiplier.")]
	[SerializeField]
	private float itemPriceMultiplier = 1.25f;

	[BoxGroup("Pricing")]
	[Tooltip("Orders below this total are rejected. 0 disables the minimum.")]
	[SerializeField]
	private float minimumOrderCost;

	[BoxGroup("Scheduling")]
	[Tooltip("The earliest slot is the next full hour at least this far away.")]
	[SerializeField]
	private int minutesUntilEarliestDelivery = 45;

	[BoxGroup("Scheduling")]
	[Tooltip("How many hourly slots the delivery time dropdown offers.")]
	[Range(1f, 48f)]
	[SerializeField]
	private int deliverySlotsToShow = 12;

	[BoxGroup("Orders")]
	[Tooltip("Maximum total items in a single order. The whole order is delivered in one paper bag.")]
	[Range(1f, 10f)]
	[SerializeField]
	private int maxItemsPerOrder = 10;

	public float DeliveryFee => deliveryFee;

	public float ItemPriceMultiplier => itemPriceMultiplier;

	public float MinimumOrderCost => minimumOrderCost;

	public int MinutesUntilEarliestDelivery => minutesUntilEarliestDelivery;

	public int DeliverySlotsToShow => deliverySlotsToShow;

	public int MaxItemsPerOrder => maxItemsPerOrder;
}

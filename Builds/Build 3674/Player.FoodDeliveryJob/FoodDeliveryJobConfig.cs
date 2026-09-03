using HGAttributes;
using NaughtyAttributes;
using UnityEngine;
using Vehicles.DeliveryDriverJob;

namespace Player.FoodDeliveryJob;

[CreateAssetMenu(fileName = "FoodDeliveryJobConfig", menuName = "BigAmbitions/FoodDeliveryJob/Config")]
public class FoodDeliveryJobConfig : ScriptableObject
{
	[Header("Locations")]
	[SerializeField]
	[AutocompleteDropdown("BusinessTypes")]
	[Tooltip("Which AI businesses an order can be picked up from. Only these ever show a pickup, like fast food and coffee shops.")]
	private string[] sourceBusinessTypes;

	[SerializeField]
	[AutocompleteDropdown("BuildingTypes")]
	[Tooltip("Which building types an order can be delivered to. Normally homes people live in, not shops or offices.")]
	private string[] destinationBuildingTypes;

	[SerializeField]
	[Tooltip("How far a delivery can reach, in meters. The drop-off is always within this range of the pickup, so orders never send you across the whole map.")]
	private float destinationRadius = 600f;

	[SerializeField]
	[Tooltip("Addresses that never get an order to pick up, even if their business type is in the list above.")]
	private Address[] excludedPickupAddresses;

	[SerializeField]
	[Tooltip("Addresses that never get picked as a drop-off, even if their building type is in the list above.")]
	private Address[] excludedDeliveryAddresses;

	[Header("Offer Board")]
	[SerializeField]
	[Tooltip("How many new orders show up each in-game hour, until the board hits the max below.")]
	private int newOffersPerHour = 3;

	[SerializeField]
	[Tooltip("The most orders that can sit on the board at once, across the whole city. Nothing new spawns once this is full.")]
	private int maxActiveOffers = 6;

	[SerializeField]
	[MinMaxSlider(0f, 1440f)]
	[Tooltip("How long an order stays on the board before it expires. Each one picks a random time in this range. This is how long you have to accept it, not the delivery timer. Expiry is ignored while you're standing inside the pickup business.")]
	private Vector2Int offerActiveMinutes = new Vector2Int(60, 180);

	[Header("Order Contents")]
	[SerializeField]
	[MinMaxSlider(1f, 10f)]
	[Tooltip("How many different items an order asks for. Each order rolls a random count in this range.")]
	private Vector2Int distinctItemsPerOrder = new Vector2Int(1, 3);

	[SerializeField]
	[Tooltip("The most of a single item an order can ask for. Each item rolls between 1 and this number.")]
	private int maxAmountPerItem = 3;

	[Header("Reward")]
	[SerializeField]
	[Tooltip("Flat pay every delivery starts with, before the distance bonus is added on top.")]
	private float baseReward = 30f;

	[SerializeField]
	[Tooltip("Extra pay for each meter between pickup and drop-off. Longer trips pay more. Reward is base plus this times the distance.")]
	private float rewardPerMeter = 0.08f;

	[SerializeField]
	[Tooltip("The tips setup this job uses. It handles the odds and size of tips paid on top of the reward.")]
	private DeliveryJobTipsConfig tipsConfig;

	[Header("Delivery Timer")]
	[SerializeField]
	[Tooltip("Flat minutes every delivery timer starts with, before the distance part. Covers the overhead that is the same on every trip: leaving the pickup, finding the entrance, elevators.")]
	private int baseTimeMinutes = 60;

	[SerializeField]
	[Tooltip("How many minutes on the delivery timer you get per meter of straight-line distance. The street route is longer than the straight line, and jogging covers about 3.5 meters per game minute, so below roughly 0.4 long trips stop being possible on foot.")]
	private float minutesPerMeter = 0.45f;

	[SerializeField]
	[MinMaxSlider(0f, 360f)]
	[Tooltip("The delivery countdown, in minutes. It's 'Base Time Minutes' plus the distance times 'Minutes Per Meter', then squeezed into this range. Left value is the least time any delivery can give, right value is the most.")]
	private Vector2Int timeLimitMinutes = new Vector2Int(60, 360);

	[Header("Backpack")]
	[SerializeField]
	[Tooltip("Where the delivery backpack sits on the player's back. Nudge this until it lines up on the model.")]
	private Vector3 backpackLocalPosition;

	[SerializeField]
	[Tooltip("How the delivery backpack is tilted on the player's back.")]
	private Vector3 backpackLocalRotation;

	[Header("Map")]
	[SerializeField]
	[Tooltip("The scooter icon for the Voogle Maps food delivery filter. The register indicator and the GPS marker during a delivery reuse it too.")]
	private Sprite mapIcon;

	[SerializeField]
	[Tooltip("Background color behind the scooter on the GPS marker while a delivery is ongoing. Same green as the delivery van job's marker by default.")]
	private Color poiColor = new Color(0.26f, 0.55f, 0.83f, 1f);

	public string[] SourceBusinessTypes => sourceBusinessTypes;

	public string[] DestinationBuildingTypes => destinationBuildingTypes;

	public float DestinationRadius => destinationRadius;

	public Address[] ExcludedPickupAddresses => excludedPickupAddresses;

	public Address[] ExcludedDeliveryAddresses => excludedDeliveryAddresses;

	public int NewOffersPerHour => newOffersPerHour;

	public int MaxActiveOffers => maxActiveOffers;

	public Vector2Int OfferActiveMinutes => offerActiveMinutes;

	public Vector2Int DistinctItemsPerOrder => distinctItemsPerOrder;

	public int MaxAmountPerItem => maxAmountPerItem;

	public float BaseReward => baseReward;

	public float RewardPerMeter => rewardPerMeter;

	public DeliveryJobTipsConfig TipsConfig => tipsConfig;

	public int BaseTimeMinutes => baseTimeMinutes;

	public float MinutesPerMeter => minutesPerMeter;

	public Vector2Int TimeLimitMinutes => timeLimitMinutes;

	public Vector3 BackpackLocalPosition => backpackLocalPosition;

	public Vector3 BackpackLocalRotation => backpackLocalRotation;

	public Sprite MapIcon => mapIcon;

	public Color PoiColor => poiColor;
}

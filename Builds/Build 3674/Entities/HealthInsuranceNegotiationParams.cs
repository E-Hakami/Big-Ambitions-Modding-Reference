using NaughtyAttributes;
using UnityEngine;

namespace Entities;

public class HealthInsuranceNegotiationParams : ScriptableObject
{
	[InfoBox("Hours to delay before sending the health insurance plan offer after requesting it:", EInfoBoxType.Normal)]
	public int hourToSendOffer = 12;

	[InfoBox("Minimum number of employees that will be charged by the health insurance plan:", EInfoBoxType.Normal)]
	public int minimumEmployeesInCharge = 10;

	[HorizontalLine(2f, EColor.Gray)]
	[InfoBox("Minimum negotiator skill required for each plan type:", EInfoBoxType.Normal)]
	public float minSkillForSilverPlan = 33f;

	public float minSkillForGoldPlan = 66f;

	[HorizontalLine(2f, EColor.Gray)]
	[InfoBox("Default prices per employee for each plan type:", EInfoBoxType.Normal)]
	public float bronzePlanDefaultPrice = 10f;

	public float silverPlanDefaultPrice = 13f;

	public float goldPlanDefaultPrice = 20f;

	[InfoBox("Price variation range for each plan type. Initial offer price will be between (defaultPrice - planPriceVariation/2) and (defaultPrice + planPriceVariation/2)", EInfoBoxType.Normal)]
	public float planPriceVariation = 3f;

	[HorizontalLine(2f, EColor.Gray)]
	[InfoBox("Offer price is adjusted by HR manager's years of experience, up to:", EInfoBoxType.Normal)]
	public int maxValuableYearsWithInsuranceExperience = 10;

	[InfoBox("Offer price is linearly discounted if plan employees are above this min value, with max discount at this max value:", EInfoBoxType.Normal)]
	public int maxEmployeesInChargeToDiscount = 25;

	public int minEmployeesInChargeToDiscount = 10;

	[InfoBox("Initial offer price is multiplied by a random factor between:", EInfoBoxType.Normal)]
	public float initialOfferMinFactor = 0.88f;

	public float initialOfferMaxFactor = 0.94f;

	[HorizontalLine(2f, EColor.Gray)]
	[InfoBox("When changing mood, a random amount between -moodPenaltyRandomness and +moodPenaltyRandomness will be added to the mood change", EInfoBoxType.Normal)]
	public int moodPenaltyRandomness = 5;

	[InfoBox("When negotiator makes a counter offer, a random amount between -counterOfferRandomnessPercentage% and +counterOfferRandomnessPercentage% will be added to the counter offer", EInfoBoxType.Normal)]
	public int counterOfferRandomnessPercentage = 5;

	[InfoBox("Below this mood, negotiator will refuse to negotiate and will only give options to accept or walk away:", EInfoBoxType.Normal)]
	public int finalOfferMood = 10;

	[HorizontalLine(2f, EColor.Gray)]
	[InfoBox("Any player offer whose difference from last player offer is below provokeThreshold (dollar amount) will make the negotiator hold firm and lose moodDropOnHoldFirm", EInfoBoxType.Normal)]
	public float minPlayerOfferIncreaseToConsiderCounterOffer = 0.03f;

	public int moodDropOnHoldFirm = 30;

	[HorizontalLine(2f, EColor.Gray)]
	[InfoBox("Any player offer less than provokeThreshold% (0-1) of the initial offer will make the negotiator hold firm and lose moodDropOnProvoke", EInfoBoxType.Normal)]
	public float provokeThreshold = 0.7f;

	public int moodDropOnProvoke = 40;

	[HorizontalLine(2f, EColor.Gray)]
	[InfoBox("Negotiator will accept the player's offer if it's within allowanceValue% (0-1) where allowanceValue is between minAllowanceValue (mood 0) and maxAllowanceValue (mood 100)", EInfoBoxType.Normal)]
	public float minAllowanceValue = 0.03f;

	public float maxAllowanceValue = 0.06f;

	[InfoBox("Negotiator will accept the player's offer if it's above thresholdToAcceptPlayerOffer% (0-1) where 0 = player's last offer, 1 = negotiator's last offer", EInfoBoxType.Normal)]
	public float thresholdToAcceptPlayerOffer = 0.33f;

	[HorizontalLine(2f, EColor.Gray)]
	[InfoBox("If player offer is below thresholdDisliked% (0-1) of the initial offer, negotiator will lose moodDropOnDisliked and change their next offer by dislikedOfferChangePercentage% (0-100)", EInfoBoxType.Normal)]
	public float thresholdDisliked = 0.8f;

	public int moodDropOnDisliked = 25;

	public float dislikedOfferChangePercentage = 20f;

	[InfoBox("If player offer is above thresholdDisliked% (0-1) of the initial offer, negotiator will lose moodDropOnLiked and change their next offer by likedOfferChangePercentage% (0-100) multiplied by negotiator's mood", EInfoBoxType.Normal)]
	public int moodDropOnLiked = 15;

	public float likedOfferChangePercentage = 50f;
}

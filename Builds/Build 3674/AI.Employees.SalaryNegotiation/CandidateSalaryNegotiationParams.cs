using NaughtyAttributes;
using UnityEngine;

namespace AI.Employees.SalaryNegotiation;

public class CandidateSalaryNegotiationParams : ScriptableObject
{
	[InfoBox("compromiseThreshold is randomized between min-max on negotiation start. Player offers above compromiseThreshold% (0-1) of the last candidate offer will receive a counter offer", EInfoBoxType.Normal)]
	public float compromiseThresholdMin = 0.8f;

	public float compromiseThresholdMax = 0.9f;

	[InfoBox("Counter offer will be between compromiseCounterOfferMin and compromiseCounterOfferMax (0-1) where 0 is last player offer, 1 is the last candidate offer, 0.5 is the midpoint", EInfoBoxType.Normal)]
	public float compromiseCounterOfferMin = 0.4f;

	public float compromiseCounterOfferMax = 0.6f;

	[InfoBox("Compromise will decrease candidate mood by a random amount between compromiseMoodDropMin and compromiseMoodDropMax", EInfoBoxType.Normal)]
	public int compromiseMoodDropMin = 4;

	public int compromiseMoodDropMax = 6;

	[HorizontalLine(2f, EColor.Gray)]
	[InfoBox("holdFirmThreshold is randomized between min-max on negotiation start. Player offers below compromiseThreshold% and above holdFirmThreshold% (0-1) of the last candidate offer will be refused with no counter offer", EInfoBoxType.Normal)]
	public float holdFirmThresholdMin = 0.6f;

	public float holdFirmThresholdMax = 0.7f;

	[InfoBox("Hold firm will decrease candidate mood by a random amount between firmMoodDropMin and firmMoodDropMax", EInfoBoxType.Normal)]
	public int firmMoodDropMin = 10;

	public int firmMoodDropMax = 20;

	[HorizontalLine(2f, EColor.Gray)]
	[InfoBox("Player offers below holdFirmThreshold% (0-1) of the last candidate offer will be refused and the candidate will raise their counter offer", EInfoBoxType.Normal)]
	public int provokeMoodDropMin = 25;

	public int provokeMoodDropMax = 30;

	[InfoBox("Provoke will increase candidate's counter offer by a random amount between provokeRaiseCounterOfferMin% and provokeRaiseCounterOfferMax% (0-1) of the last candidate offer", EInfoBoxType.Normal)]
	public float provokeRaiseCounterOfferMin = 0.1f;

	public float provokeRaiseCounterOfferMax = 0.2f;

	[HorizontalLine(2f, EColor.Gray)]
	[InfoBox("Candidate will accept player's offer if it's above a certain threshold based on their mood", EInfoBoxType.Normal)]
	public int goodMoodMin = 50;

	public float goodMoodAcceptThreshold = 0.95f;

	public int neutralMoodMin = 25;

	public float neutralMoodAcceptThreshold = 0.975f;

	public float badMoodAcceptThreshold = 0.99f;

	[HorizontalLine(2f, EColor.Gray)]
	[InfoBox("Extra penalty applied when the player reduces their signing bonus compared to their previous offer. 0 keeps current behavior, 1 doubles the hourly-value impact of the removed bonus.", EInfoBoxType.Normal)]
	public float signingBonusReductionPenaltyMultiplier = 0.3f;

	[InfoBox("If candidate's mood drops below declineIfMoodBelow, they will end the negotiation", EInfoBoxType.Normal)]
	public int declineIfMoodBelow = 20;
}

using Entities;
using UnityEngine;

namespace AI.Employees.SalaryNegotiation;

public class CandidateSalaryNegotiator
{
	private const int MaxMood = 100;

	private readonly CandidateSalaryNegotiation _negotiation;

	private float _counterOffer;

	private float _previousPlayerOffer;

	private float _lowestOffer;

	public float MoodPercentageNormalized => Mathf.Clamp01((float)_negotiation.mood / 100f);

	private CandidateSalaryNegotiationParams NegotiationParams => InstanceBehavior<GlobalReferences>.Instance.candidateSalaryNegotiationParams;

	public CandidateSalaryNegotiator(CandidateSalaryNegotiation negotiation)
	{
		_negotiation = negotiation;
	}

	public CandidateSalaryOffer GetCounterOffer()
	{
		return GetLastCandidateOffer();
	}

	public int GetMaxSigningBonus()
	{
		int num = Mathf.RoundToInt(_negotiation.employeeInstance.hourlyWage * 160f * 4f);
		int num2 = Mathf.FloorToInt(SaveGameManager.Current.Money);
		if (num2 <= 0)
		{
			return num;
		}
		return Mathf.Min(num, num2);
	}

	public (bool, string) EvaluateOffer(CandidateSalaryOffer playerOffer)
	{
		_negotiation.InitializeThresholds();
		CandidateSalaryOffer lastCandidateOffer = GetLastCandidateOffer();
		int mood = _negotiation.mood;
		float playerOfferTotalWithSigningBonusReductionPenalty = GetPlayerOfferTotalWithSigningBonusReductionPenalty(playerOffer);
		bool flag = CanAcceptOffer(playerOfferTotalWithSigningBonusReductionPenalty, playerOffer.Total, lastCandidateOffer.Total, out var newCounterOffer);
		LogOfferValues(playerOffer, lastCandidateOffer, playerOfferTotalWithSigningBonusReductionPenalty, newCounterOffer, mood, flag);
		if (flag)
		{
			_negotiation.completed = true;
			_negotiation.hourlyWage = playerOffer.hourlyWage;
			_negotiation.signingBonus = playerOffer.signingBonus;
			return (true, "dialog_candidate_salary_accepted_player_offer");
		}
		if (_negotiation.IsDeclinedByMood)
		{
			_negotiation.completed = true;
			return (false, "dialog_candidate_salary_counter_offer");
		}
		newCounterOffer = Mathf.Ceil(newCounterOffer * 20f) / 20f;
		_negotiation.offers.Add(new CandidateSalaryOffer(newCounterOffer, 0f, fromCandidate: true));
		bool flag2 = Mathf.Abs(newCounterOffer - lastCandidateOffer.Total) < 0.01f;
		return (false, flag2 ? "dialog_candidate_salary_counter_offer_firm" : "dialog_candidate_salary_counter_offer");
	}

	private void LogOfferValues(CandidateSalaryOffer playerOffer, CandidateSalaryOffer lastCandidateOffer, float effectivePlayerOffer, float newCounterOffer, int moodBefore, bool acceptOffer)
	{
	}

	private bool CanAcceptOffer(float effectivePlayerOffer, float playerOffer, float lastCandidateOffer, out float newCounterOffer)
	{
		newCounterOffer = lastCandidateOffer;
		float num = NegotiationParams.badMoodAcceptThreshold;
		if (_negotiation.mood >= NegotiationParams.goodMoodMin)
		{
			num = NegotiationParams.goodMoodAcceptThreshold;
		}
		else if (_negotiation.mood >= NegotiationParams.neutralMoodMin)
		{
			num = NegotiationParams.neutralMoodAcceptThreshold;
		}
		if (effectivePlayerOffer >= lastCandidateOffer * num)
		{
			return true;
		}
		if (effectivePlayerOffer >= lastCandidateOffer * _negotiation.negotiateThreshold)
		{
			float t = Random.Range(NegotiationParams.compromiseCounterOfferMin, NegotiationParams.compromiseCounterOfferMax);
			newCounterOffer = Mathf.Lerp(lastCandidateOffer, playerOffer, t);
			_negotiation.mood -= Random.Range(NegotiationParams.compromiseMoodDropMin, NegotiationParams.compromiseMoodDropMax);
			return false;
		}
		if (effectivePlayerOffer >= lastCandidateOffer * _negotiation.firmThreshold)
		{
			_negotiation.mood -= Random.Range(NegotiationParams.firmMoodDropMin, NegotiationParams.firmMoodDropMax);
			return false;
		}
		_negotiation.mood -= Random.Range(NegotiationParams.provokeMoodDropMin, NegotiationParams.provokeMoodDropMax);
		newCounterOffer += lastCandidateOffer * Random.Range(NegotiationParams.provokeRaiseCounterOfferMin, NegotiationParams.provokeRaiseCounterOfferMax);
		return false;
	}

	public NegotiationOptions[] GetOptions()
	{
		if (_negotiation.IsDeclinedByMood)
		{
			return new NegotiationOptions[1];
		}
		return new NegotiationOptions[3]
		{
			NegotiationOptions.Decline,
			NegotiationOptions.Negotiate,
			NegotiationOptions.Accept
		};
	}

	private CandidateSalaryOffer GetLastCandidateOffer()
	{
		for (int num = _negotiation.offers.Count - 1; num >= 0; num--)
		{
			if (_negotiation.offers[num].fromCandidate)
			{
				return _negotiation.offers[num];
			}
		}
		return default(CandidateSalaryOffer);
	}

	private bool TryGetPreviousPlayerOffer(out CandidateSalaryOffer previousPlayerOffer)
	{
		previousPlayerOffer = default(CandidateSalaryOffer);
		bool flag = false;
		for (int num = _negotiation.offers.Count - 1; num >= 0; num--)
		{
			if (!_negotiation.offers[num].fromCandidate)
			{
				if (flag)
				{
					previousPlayerOffer = _negotiation.offers[num];
					return true;
				}
				flag = true;
			}
		}
		return false;
	}

	private float GetPlayerOfferTotalWithSigningBonusReductionPenalty(CandidateSalaryOffer playerOffer)
	{
		if (!TryGetPreviousPlayerOffer(out var previousPlayerOffer))
		{
			return playerOffer.Total;
		}
		float num = previousPlayerOffer.signingBonus - playerOffer.signingBonus;
		if (num <= 0f)
		{
			return playerOffer.Total;
		}
		float num2 = CandidateSalaryOffer.GetHourlyValueForSigningBonus(num) * NegotiationParams.signingBonusReductionPenaltyMultiplier;
		return playerOffer.Total - num2;
	}
}

using System;
using Extensions;
using JimmysUnityUtilities;
using UnityEngine;

namespace Entities;

public class Negotiator
{
	private readonly string[] _impossibleFeedbackKeys = new string[2] { "dialog_negotiation_impossible_1", "dialog_negotiation_impossible_2" };

	private readonly string[] _badFeedbackKeys = new string[2] { "dialog_negotiation_bad_1", "dialog_negotiation_bad_2" };

	private readonly string[] _acceptableFeedbackKeys = new string[2] { "dialog_negotiation_acceptable_1", "dialog_negotiation_acceptable_2" };

	private readonly NegotiationOffer _offer;

	private readonly int _maxMood;

	private int _mood;

	private float _currentNegotiatorOffer;

	private float _previousPlayerOffer;

	private int _currentNegotiationRound;

	private float _lowestOffer;

	private static int FinalOfferMood => NegotiationParams.finalOfferMood;

	private static int MoodPenaltyRandomness => NegotiationParams.moodPenaltyRandomness;

	private static int CounterOfferRandomnessPercentage => NegotiationParams.counterOfferRandomnessPercentage;

	private static float MinAllowanceValue => NegotiationParams.minAllowanceValue;

	private static float MaxAllowanceValue => NegotiationParams.maxAllowanceValue;

	public float MoodPercentageNormalized => Math.Clamp((float)_mood / (float)_maxMood, 0f, 1f);

	private static HealthInsuranceNegotiationParams NegotiationParams => InstanceBehavior<GlobalReferences>.Instance.healthInsuranceNegotiationParams;

	public bool IsDeclinedByMood => _mood <= 0;

	public Negotiator(int maxMood, NegotiationOffer offer)
	{
		_maxMood = maxMood;
		_mood = maxMood;
		_offer = offer;
		_lowestOffer = offer.minOfferPrice;
		_currentNegotiatorOffer = offer.initialOfferPrice;
		_currentNegotiationRound = 0;
	}

	public float GetCounterOffer()
	{
		return Math.Clamp(_currentNegotiatorOffer, _lowestOffer, _offer.initialOfferPrice);
	}

	public (bool, string) EvaluateOffer(float playerOffer)
	{
		CoroutineUtility.RunAfterOneFrame(delegate
		{
			_currentNegotiationRound++;
			if (playerOffer > _previousPlayerOffer)
			{
				_previousPlayerOffer = playerOffer;
			}
		});
		CalculateLowestOffer(Math.Abs(playerOffer - _previousPlayerOffer));
		if (playerOffer.Difference(_currentNegotiatorOffer) < GetAllowanceValue())
		{
			return (true, "dialog_negotiation_accepted_close_enough");
		}
		if (playerOffer > _currentNegotiatorOffer)
		{
			return (true, "dialog_negotiation_accepted_strange_tactic");
		}
		if (Math.Abs(playerOffer - _currentNegotiatorOffer) < 0.01f)
		{
			return (true, "dialog_negotiation_accepted_glad_you_accepted");
		}
		if (playerOffer < NegotiationParams.provokeThreshold * _offer.initialOfferPrice || playerOffer < _previousPlayerOffer)
		{
			UpdateMoodAndOffer(NegotiationParams.moodDropOnProvoke, 0f, playerOffer);
			return (false, _impossibleFeedbackKeys.GetRandom());
		}
		if (playerOffer - _previousPlayerOffer <= NegotiationParams.minPlayerOfferIncreaseToConsiderCounterOffer)
		{
			UpdateMoodAndOffer(NegotiationParams.moodDropOnHoldFirm, 0f, playerOffer);
			return (false, "dialog_negotiation_declined_take_it_seriously");
		}
		if (playerOffer > _lowestOffer && playerOffer.DifferenceNormalized(_currentNegotiatorOffer, _previousPlayerOffer) >= NegotiationParams.thresholdToAcceptPlayerOffer)
		{
			return (true, "dialog_negotiation_accepted_accept_player_offer");
		}
		if (playerOffer < NegotiationParams.thresholdDisliked * _offer.initialOfferPrice)
		{
			UpdateMoodAndOffer(NegotiationParams.moodDropOnDisliked, NegotiationParams.dislikedOfferChangePercentage, playerOffer);
			return (false, _badFeedbackKeys.GetRandom());
		}
		float offerChangePercentage = MoodPercentageNormalized * NegotiationParams.likedOfferChangePercentage;
		UpdateMoodAndOffer(NegotiationParams.moodDropOnLiked, offerChangePercentage, Math.Min(playerOffer, _lowestOffer));
		return (false, _acceptableFeedbackKeys.GetRandom());
	}

	private void UpdateMoodAndOffer(int moodPenalty, float offerChangePercentage, float playerOffer)
	{
		if (moodPenalty != 0)
		{
			moodPenalty += UnityEngine.Random.Range(-MoodPenaltyRandomness, MoodPenaltyRandomness);
			_mood -= moodPenalty;
		}
		if (offerChangePercentage != 0f)
		{
			offerChangePercentage += (float)UnityEngine.Random.Range(-CounterOfferRandomnessPercentage, CounterOfferRandomnessPercentage);
			_currentNegotiatorOffer = Math.Clamp(_currentNegotiatorOffer.MinusPercentageBetween(playerOffer, Mathf.RoundToInt(offerChangePercentage)), _lowestOffer, _offer.initialOfferPrice);
		}
	}

	private float GetAllowanceValue()
	{
		return MoodPercentageNormalized * (MaxAllowanceValue - MinAllowanceValue) + MinAllowanceValue;
	}

	public NegotiationOptions[] GetOptions()
	{
		if (IsDeclinedByMood)
		{
			return new NegotiationOptions[1];
		}
		if (_mood > FinalOfferMood && !(Math.Abs(_currentNegotiatorOffer - _offer.minOfferPrice) < 0.01f))
		{
			return new NegotiationOptions[3]
			{
				NegotiationOptions.Decline,
				NegotiationOptions.Negotiate,
				NegotiationOptions.Accept
			};
		}
		return new NegotiationOptions[2]
		{
			NegotiationOptions.Decline,
			NegotiationOptions.Accept
		};
	}

	private void CalculateLowestOffer(float differenceBetweenPlayerOffers)
	{
		int currentNegotiationRound = _currentNegotiationRound;
		float lowestOffer = ((currentNegotiationRound >= 2) ? Math.Clamp(_offer.minOfferPrice, _currentNegotiatorOffer - differenceBetweenPlayerOffers, _offer.initialOfferPrice) : (currentNegotiationRound switch
		{
			0 => 0.67f * Math.Abs(_offer.initialOfferPrice - _offer.minOfferPrice) + _offer.minOfferPrice, 
			1 => Math.Clamp(0.33f * Math.Abs(_offer.initialOfferPrice - _offer.minOfferPrice) + _offer.minOfferPrice, _currentNegotiatorOffer - differenceBetweenPlayerOffers, _offer.initialOfferPrice), 
			_ => _lowestOffer, 
		}));
		_lowestOffer = lowestOffer;
	}
}

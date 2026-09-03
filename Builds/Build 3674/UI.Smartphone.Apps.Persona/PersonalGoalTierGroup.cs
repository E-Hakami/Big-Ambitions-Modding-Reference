using System;

namespace UI.Smartphone.Apps.Persona;

public class PersonalGoalTierGroup
{
	private const int TierCount = 3;

	private readonly bool[] _completedTiers = new bool[3];

	private readonly GenericPersonalGoal[] _tiers = new GenericPersonalGoal[3];

	private int _tierCount;

	public bool AreAllTiersCompleted { get; private set; }

	public int CurrentTierIndex { get; private set; } = -1;

	public GenericPersonalGoal DisplayGoal { get; private set; }

	public int HighestCompletedSteamTierIndex { get; private set; } = -1;

	public void Add(GenericPersonalGoal personalGoal)
	{
		if (!(personalGoal == null))
		{
			int tierInsertIndex = GetTierInsertIndex(personalGoal);
			if (tierInsertIndex < 3)
			{
				InsertTier(tierInsertIndex, personalGoal, personalGoal.IsCompleted);
				RefreshCachedState();
			}
		}
	}

	public GenericPersonalGoal GetTier(int index)
	{
		if (index < 0 || index >= _tierCount)
		{
			return null;
		}
		return _tiers[index];
	}

	public bool IsTierCompleted(int index)
	{
		if (index >= 0 && index < 3)
		{
			return _completedTiers[index];
		}
		return false;
	}

	private void RefreshCachedState()
	{
		HighestCompletedSteamTierIndex = GetHighestCompletedSteamTierIndex();
		if (_tierCount == 0)
		{
			DisplayGoal = null;
			CurrentTierIndex = -1;
			AreAllTiersCompleted = false;
			return;
		}
		for (int i = 0; i < _tierCount; i++)
		{
			if (!_completedTiers[i])
			{
				DisplayGoal = _tiers[i];
				CurrentTierIndex = i;
				AreAllTiersCompleted = false;
				return;
			}
		}
		DisplayGoal = _tiers[_tierCount - 1];
		CurrentTierIndex = _tierCount - 1;
		AreAllTiersCompleted = _tierCount == 3;
	}

	private int GetHighestCompletedSteamTierIndex()
	{
		for (int num = _tierCount - 1; num >= 0; num--)
		{
			if (_tiers[num] != null && IsCompletedOnSteam(_tiers[num]))
			{
				return num;
			}
		}
		return -1;
	}

	private static bool IsCompletedOnSteam(GenericPersonalGoal personalGoal)
	{
		try
		{
			return personalGoal.IsCompletedOnSteam();
		}
		catch (Exception)
		{
			return false;
		}
	}

	private int GetTierInsertIndex(GenericPersonalGoal personalGoal)
	{
		float sortValue = personalGoal.GetSortValue();
		for (int i = 0; i < _tierCount; i++)
		{
			if (sortValue < _tiers[i].GetSortValue())
			{
				return i;
			}
		}
		return _tierCount;
	}

	private void InsertTier(int index, GenericPersonalGoal personalGoal, bool isCompleted)
	{
		for (int num = ((_tierCount < 3) ? _tierCount : 2); num > index; num--)
		{
			_tiers[num] = _tiers[num - 1];
			_completedTiers[num] = _completedTiers[num - 1];
		}
		_tiers[index] = personalGoal;
		_completedTiers[index] = isCompleted;
		if (_tierCount < 3)
		{
			_tierCount++;
		}
	}
}

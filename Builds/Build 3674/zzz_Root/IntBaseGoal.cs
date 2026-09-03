using Extensions;
using Localizor.LanguageChangeEvent;
using UnityEngine;

public abstract class IntBaseGoal : GenericPersonalGoal
{
	public int amount;

	private int _lastValue;

	private int _lastValueFrame = -1;

	public override float GetSortValue()
	{
		return amount;
	}

	protected override bool IsInt()
	{
		return true;
	}

	protected override bool CheckIfCompleted()
	{
		return GetValueCached() >= amount;
	}

	public override LanguageChangeEventDataHolder GetTitle()
	{
		LanguageChangeEventDataHolder result = base.GetTitle();
		result.Arguments = new
		{
			amount = GetFormattedAmount()
		};
		return result;
	}

	public override LanguageChangeEventDataHolder GetProgress()
	{
		LanguageChangeEventDataHolder result = base.GetProgress();
		result.Arguments = new
		{
			current = FormatProgressValue(Mathf.Min(GetValueCached(), amount)),
			amount = FormatProgressValue(amount)
		};
		return result;
	}

	protected abstract int GetValue();

	private string GetFormattedAmount()
	{
		return GetFormattedAmount(amount);
	}

	private static string GetFormattedAmount(int value)
	{
		return value.ToString("N0", CultureHelper.CultureInfo);
	}

	protected virtual object FormatProgressValue(int value)
	{
		return GetFormattedAmount(value);
	}

	protected int GetValueCached()
	{
		if (_lastValueFrame == Time.frameCount)
		{
			return _lastValue;
		}
		_lastValueFrame = Time.frameCount;
		_lastValue = GetValue();
		return _lastValue;
	}

	protected override float GetSettingsStateValue()
	{
		return Mathf.Min(GetValueCached(), amount);
	}

	protected override bool ShouldIndicateProgress(out int current, out int max)
	{
		bool num = base.ShouldIndicateProgress(out current, out max);
		max = amount;
		if (!num)
		{
			return false;
		}
		current = (int)GetSettingsStateValue();
		return SteamAPI.GetStateInt(steamStatID) < current;
	}
}

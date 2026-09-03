using Localizor.LanguageChangeEvent;
using UnityEngine;

public abstract class FloatBaseGoal : GenericPersonalGoal
{
	public float amount;

	private float _lastValue;

	private int _lastValueFrame = -1;

	public override float GetSortValue()
	{
		return amount;
	}

	protected override bool CheckIfCompleted()
	{
		return GetValueCached() >= amount;
	}

	public override LanguageChangeEventDataHolder GetTitle()
	{
		LanguageChangeEventDataHolder result = base.GetTitle();
		result.Arguments = new { amount };
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

	protected abstract float GetValue();

	protected virtual object FormatProgressValue(float value)
	{
		return value;
	}

	private float GetValueCached()
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
		max = (int)amount;
		if (!num)
		{
			return false;
		}
		current = (int)GetSettingsStateValue();
		return !(SteamAPI.GetStateFloat(steamStatID) >= (float)current);
	}
}

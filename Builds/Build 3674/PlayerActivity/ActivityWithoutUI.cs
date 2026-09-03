using UI.Elements;

namespace PlayerActivity;

public class ActivityWithoutUI : IPlayerActivity
{
	public bool RequiresEnergy()
	{
		return false;
	}

	public PlayerActivityState GetState()
	{
		return PlayerActivityState.Running;
	}

	public PlayerActivityState GetStateBeforeFinishing()
	{
		return PlayerActivityState.Running;
	}

	public void ChangeState(PlayerActivityState state)
	{
	}

	public void Perform(int minutes)
	{
	}

	public void Finish()
	{
	}

	public LabelInfo GetHeadlineLabel()
	{
		return null;
	}

	public LabelInfo[] GetLabels()
	{
		return new LabelInfo[0];
	}

	public ButtonInfo[] GetButtons()
	{
		return new ButtonInfo[0];
	}

	public bool HasFastForward()
	{
		return false;
	}

	public bool HasTimeMachine()
	{
		return false;
	}

	public int GetRemainingMinutesForTimeMachine()
	{
		return 0;
	}

	public bool HasProgressBar()
	{
		return false;
	}

	public float GetProgressBarPercentageValue()
	{
		return 0f;
	}

	public (string key, object arguments) GetProgressBarLabel()
	{
		return (key: null, arguments: null);
	}

	public bool HasSlider()
	{
		return false;
	}

	public int GetMinSliderValue()
	{
		return 0;
	}

	public int GetMaxSliderValue()
	{
		return 0;
	}

	public float GetCurrentSliderValue()
	{
		return 0f;
	}

	public void OnSliderValueChanged(int value)
	{
	}

	public (string key, object arguments) GetSliderInfo()
	{
		return (key: null, arguments: null);
	}
}

using BigAmbitions.DayNightCycle;
using UI.Elements;

namespace PlayerActivity;

public interface IPlayerActivity
{
	bool RequiresEnergy();

	bool BlocksAutoSave()
	{
		return true;
	}

	PlayerActivityState GetState();

	PlayerActivityState GetStateBeforeFinishing();

	void ChangeState(PlayerActivityState state);

	void Perform(int minutes);

	void Finish();

	LabelInfo GetHeadlineLabel();

	LabelInfo[] GetLabels();

	ButtonInfo[] GetButtons();

	bool HasFastForward();

	bool HasTimeMachine();

	int GetRemainingMinutesForTimeMachine();

	Timestamp GetTimeMachineGoal()
	{
		return TimeHelper.Now().AddMinutes(GetRemainingMinutesForTimeMachine());
	}

	bool HasProgressBar();

	float GetProgressBarPercentageValue();

	(string key, object arguments) GetProgressBarLabel();

	bool HasSlider();

	int GetMinSliderValue();

	int GetMaxSliderValue();

	float GetCurrentSliderValue();

	void OnSliderValueChanged(int value);

	(string key, object arguments) GetSliderInfo();
}

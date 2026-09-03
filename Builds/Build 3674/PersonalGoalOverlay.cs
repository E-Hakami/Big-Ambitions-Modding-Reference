using System.Collections;
using DG.Tweening;
using Localizor.LanguageChangeEvent;
using UI.Smartphone.Apps.Persona;
using UnityEngine;
using UnityEngine.Events;

public class PersonalGoalOverlay : InstanceBehavior<PersonalGoalOverlay>
{
	[SerializeField]
	private CanvasGroup panel;

	[SerializeField]
	private TextLocalizationComponent personalGoalTitle;

	[SerializeField]
	private RandomAudioClipOnSource randomAudioClipOnSource;

	public UnityEvent onGoalCompleted = new UnityEvent();

	private const float timeOnScreen = 3f;

	private void Start()
	{
		panel.alpha = 0f;
		panel.gameObject.SetActive(value: true);
		PersonalGoalsUI.UpdatePersonalGoals(string.Empty);
	}

	public void ShowPersonalGoalCompleted(GenericPersonalGoal genericPersonalGoal)
	{
		personalGoalTitle.SetData(genericPersonalGoal.GetTitle());
		StartCoroutine(StartPersonalGoalDisplay());
		randomAudioClipOnSource.PlayRandomSound();
		onGoalCompleted.Invoke();
	}

	private IEnumerator StartPersonalGoalDisplay()
	{
		panel.DOComplete();
		yield return panel.DOFade(1f, 1f).SetLink(panel.gameObject).SetUpdate(isIndependentUpdate: true)
			.WaitForCompletion();
		yield return new WaitForSecondsRealtime(3f);
		yield return panel.DOFade(0f, 1f).SetLink(panel.gameObject).SetUpdate(isIndependentUpdate: true)
			.WaitForCompletion();
	}
}

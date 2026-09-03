using Character;
using Dancing;
using Extensions;
using Helpers;
using UI.Notification;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Topbar;

public class PlayerDancesUI : MonoBehaviour
{
	[SerializeField]
	private Button danceButton;

	[SerializeField]
	private Transform danceTypeButtonTemplate;

	[SerializeField]
	private ButtonEffects danceButtonEffects;

	public bool IsDanceButtonInteractable => danceButton.interactable;

	private void Start()
	{
		InitDanceTypeButtons();
	}

	public void OnDanceButtonClick()
	{
		base.gameObject.SetActive(value: true);
	}

	private void InitDanceTypeButtons()
	{
		danceTypeButtonTemplate.ResetTemplate();
		DanceType[] allDances = Dances.GetAllDances();
		foreach (DanceType danceType in allDances)
		{
			Transform obj = danceTypeButtonTemplate.CreateElement();
			obj.GetLanguageChangeEventByName("DanceNameLabel").Key = danceType.GetLocalizeKey();
			obj.GetComponent<Button>().onClick.AddListener(delegate
			{
				base.gameObject.SetActive(value: false);
				StartDancing(danceType);
			});
		}
	}

	private void StartDancing(DanceType danceType)
	{
		if (!(InstanceBehavior<GameManager>.Instance.playerController.Character.velocity != Vector3.zero))
		{
			if (InstanceBehavior<GameManager>.Instance.playerController.Character.walkingSpeed == ThirdPersonCharacter.WalkingSpeed.Zombie)
			{
				Notifications.ShowInsufficientEnergy();
				return;
			}
			InstanceBehavior<GameManager>.Instance.playerController.ResetNavigation();
			PlayerDances.StartDancing(danceType);
		}
	}

	public void EnableDances()
	{
		danceButton.interactable = true;
		danceButtonEffects.enabled = true;
	}

	public void DisableDances()
	{
		danceButton.interactable = false;
		danceButtonEffects.enabled = false;
		base.gameObject.SetActive(value: false);
	}
}

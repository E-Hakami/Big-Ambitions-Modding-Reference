using DG.Tweening;
using Localizor;
using Scenes.MainMenu;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class RadioControls : MonoBehaviour
{
	[SerializeField]
	private TextMeshProUGUI currentSongLabel;

	[SerializeField]
	private Button skipButton;

	[SerializeField]
	private Slider volumeSlider;

	[SerializeField]
	private AudioMixer audioMixer;

	[HideInInspector]
	public bool radioPlaying;

	public RectTransform smartphoneContainerRect;

	public CollapsibleWindow smartphoneCollapsibleWindow;

	private CanvasGroup _radioCanvasGroup;

	private void Start()
	{
		_radioCanvasGroup = GetComponent<CanvasGroup>();
		InstanceBehavior<UIs>.Instance.miniMenuUI.onCloseOptionsMenu.AddListener(UpdateCurrentSong);
		InstanceBehavior<GameManager>.Instance.radioPlayer.onNewSong.AddListener(delegate
		{
			UpdateCurrentSong();
		});
		InstanceBehavior<GameManager>.Instance.radioPlayer.onSongsRefreshing.AddListener(UpdateCurrentSong);
		InstanceBehavior<GameManager>.Instance.radioPlayer.onRadioToggle.AddListener(delegate(bool isActive)
		{
			UpdateCurrentSong();
			skipButton.interactable = isActive;
			radioPlaying = isActive;
		});
		volumeSlider.SetValueWithoutNotify(PlayerPrefSettings.RadioVolume);
		volumeSlider.onValueChanged.AddListener(delegate
		{
			PlayerPrefSettings.RadioVolume = volumeSlider.value;
			audioMixer.SetFloat("RadioVolume", Options.GetVolume(volumeSlider.value));
			InstanceBehavior<UIs>.Instance.options.UpdateRadioVolume();
		});
		UpdateVolume();
		UpdateCurrentSong();
		DisableUI();
	}

	public void ChangeVolume(float amountToAdd)
	{
		volumeSlider.value += amountToAdd;
	}

	public void UpdateVolume()
	{
		float radioVolume = PlayerPrefSettings.RadioVolume;
		audioMixer.SetFloat("RadioVolume", Options.GetVolume(radioVolume));
		volumeSlider.SetValueWithoutNotify(radioVolume);
	}

	private void UpdateCurrentSong()
	{
		currentSongLabel.text = GetSongLabel();
	}

	public static string GetSongLabel()
	{
		RadioPlayer radioPlayer = InstanceBehavior<GameManager>.Instance.radioPlayer;
		string result = "-";
		if (radioPlayer.IsMuted)
		{
			return result;
		}
		RadioStationData radioStationData = radioPlayer.GetRadioStationData(radioPlayer.GetCurrentStation());
		if (InstanceBehavior<GameManager>.Instance.radioPlayer.currentClip != null)
		{
			result = radioPlayer.GetCurrentStation().ToString() + "\n" + InstanceBehavior<GameManager>.Instance.radioPlayer.currentClip.name;
		}
		else if (radioStationData.HasPlayableClips && radioStationData.IsLoading)
		{
			result = radioPlayer.GetCurrentStation().ToString() + "\n" + "local_songs_are_loading".GetLocalization();
		}
		return result;
	}

	public void EnableUI()
	{
		RunTransition(show: true);
	}

	public void DisableUI()
	{
		RunTransition(show: false);
	}

	private void RunTransition(bool show)
	{
		float num = (show ? (-1145f) : (-965f));
		float y = (show ? 1230f : 1050f);
		float endValue = (show ? 1f : 0f);
		smartphoneCollapsibleWindow.collapsedPosition.y = num;
		smartphoneCollapsibleWindow.hoverPosition.y = num + 10f;
		Vector2 vector = new Vector2(smartphoneContainerRect.sizeDelta.x, y);
		if (smartphoneCollapsibleWindow.IsCollapsed)
		{
			smartphoneContainerRect.sizeDelta = vector;
			smartphoneContainerRect.anchoredPosition = new Vector2(0f, num);
		}
		else
		{
			smartphoneContainerRect.DOSizeDelta(vector, 0.2f).SetUpdate(isIndependentUpdate: true);
		}
		_radioCanvasGroup.blocksRaycasts = show;
		_radioCanvasGroup.DOFade(endValue, 0.2f).SetUpdate(isIndependentUpdate: true);
	}

	public void PlayNextStation()
	{
		InstanceBehavior<GameManager>.Instance.radioPlayer.PlayNextStation();
	}
}

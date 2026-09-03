using Localizor.LanguageChangeEvent;
using UnityEngine;
using UnityEngine.Events;

public class FpsMeter : MonoBehaviour
{
	private const float UpdateInterval = 0.5f;

	public static UnityEvent<bool> onShowFpsOptionChanged;

	[SerializeField]
	private TextLocalizationComponent label;

	private int _framesDrawnOverTheCurrentInterval;

	private int _lastFPS;

	private bool _showFps;

	private float _timeLeftForCurrentInterval;

	private void Start()
	{
		onShowFpsOptionChanged = new UnityEvent<bool>();
		onShowFpsOptionChanged.AddListener(UpdateVisibility);
		UpdateVisibility(PlayerPrefSettings.showFps);
	}

	private void UpdateVisibility(bool showFps)
	{
		_showFps = showFps;
		label.gameObject.SetActive(_showFps);
	}

	private void Update()
	{
		if (!_showFps)
		{
			return;
		}
		_timeLeftForCurrentInterval -= Time.unscaledDeltaTime;
		_framesDrawnOverTheCurrentInterval++;
		if (!(_timeLeftForCurrentInterval > 0f))
		{
			int num = (int)((float)_framesDrawnOverTheCurrentInterval / (0.5f + Mathf.Abs(_timeLeftForCurrentInterval)));
			_timeLeftForCurrentInterval = 0.5f;
			_framesDrawnOverTheCurrentInterval = 0;
			if (num != _lastFPS)
			{
				_lastFPS = num;
				label.Arguments = new
				{
					fps = num
				};
			}
		}
	}
}

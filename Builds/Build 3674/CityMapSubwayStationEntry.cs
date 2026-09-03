using System.Collections;
using Localizor.LanguageChangeEvent;
using UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CityMapSubwayStationEntry : MonoBehaviour, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler, IPointerClickHandler
{
	public Image background;

	public TextLocalizationComponent stationNameLabel;

	public Button travelButton;

	private SubwayStation _station;

	private Coroutine _delayCoroutine;

	private bool _isCurrentStation;

	private Color _defaultBackgroundColor;

	private void Awake()
	{
		_defaultBackgroundColor = background.color;
	}

	public void Init()
	{
		_isCurrentStation = InstanceBehavior<CityManager>.Instance.subwaySystem?.lastSubwayStation == _station;
		base.gameObject.SetActive(value: true);
		background.color = (_isCurrentStation ? ((Color)InstanceBehavior<GlobalReferences>.Instance.colors.darkGrey) : _defaultBackgroundColor);
		background.enabled = _isCurrentStation;
		travelButton.interactable = !_isCurrentStation;
	}

	public void OnPointerClick(PointerEventData eventData)
	{
		if (!_isCurrentStation && !SubwaySystem.IsRiding)
		{
			InstanceBehavior<CityManager>.Instance.subwaySystem.TravelTo(_station);
		}
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		background.enabled = true;
		_delayCoroutine = StartCoroutine(ActivationDelay());
	}

	private IEnumerator ActivationDelay()
	{
		yield return new WaitForSecondsRealtime(InstanceBehavior<UIs>.Instance.cityMapSubwayStations.cameraPreviewDelay);
		InstanceBehavior<CityManager>.Instance.cityMap.cityMapCam.MoveCameraToTarget(_station.CityMapPoi.target.position);
		_station.OnIoEnter();
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		background.enabled = _isCurrentStation;
		_station.OnIoExit();
		StopCoroutine(_delayCoroutine);
		if (!SubwaySystem.IsRiding)
		{
			InstanceBehavior<CityManager>.Instance.cityMap.cityMapCam.MoveCameraToTarget(Vector3.zero);
		}
	}

	public void Setup(SubwayStation station)
	{
		_station = station;
		stationNameLabel.Key = "subwaystation_" + _station.stationName.ToStringFast();
		base.gameObject.SetActive(value: true);
		travelButton.onClick.AddListener(delegate
		{
			OnPointerClick(null);
		});
	}
}

using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(TMP_Dropdown))]
public class DropdownClickSound : MonoBehaviour, IPointerClickHandler, IEventSystemHandler, ISubmitHandler
{
	[SerializeField]
	private UiSound openSoundType;

	[SerializeField]
	private UiSound valueChangedSoundType;

	public void Awake()
	{
		GetComponent<TMP_Dropdown>().onValueChanged.AddListener(delegate
		{
			UiSoundHelper.Play(valueChangedSoundType);
		});
	}

	public void OnSubmit(BaseEventData eventData)
	{
		UiSoundHelper.Play(openSoundType);
	}

	public void OnPointerClick(PointerEventData eventData)
	{
		UiSoundHelper.Play(openSoundType);
	}
}

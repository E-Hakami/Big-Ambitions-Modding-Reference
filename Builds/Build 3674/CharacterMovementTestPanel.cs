using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterMovementTestPanel : MonoBehaviour
{
	[SerializeField]
	private ThirdPersonCharacter tpc;

	[SerializeField]
	private Toggle moveToggle;

	[SerializeField]
	private TMP_Dropdown movementSpeedTypeDropdown;

	[SerializeField]
	private Slider timeSpeedMultiplierSlider;

	[SerializeField]
	private Transform centerPosition;

	[SerializeField]
	private Transform[] points;

	private int _currentPoint;

	private void OnEnable()
	{
		timeSpeedMultiplierSlider.value = 1f;
		Time.timeScale = 1f;
	}

	private void OnDisable()
	{
		moveToggle.isOn = false;
		tpc.ResetAnimator();
	}

	private void Start()
	{
		moveToggle.onValueChanged.AddListener(Move);
		movementSpeedTypeDropdown.onValueChanged.AddListener(SetMovementSpeed);
		timeSpeedMultiplierSlider.onValueChanged.AddListener(SetTimeSpeed);
	}

	private void Move(bool move)
	{
		if (move)
		{
			tpc.Reset();
			_currentPoint = -1;
			MoveToNextPoint();
		}
		else
		{
			tpc.Reset();
			tpc.ForceToTransform(centerPosition);
			tpc.navmeshAgent.Warp(centerPosition.position);
		}
	}

	private void SetMovementSpeed(int index)
	{
		tpc.SetWalkingSpeed(index switch
		{
			1 => ThirdPersonCharacter.WalkingSpeed.Jog, 
			2 => ThirdPersonCharacter.WalkingSpeed.Run, 
			3 => ThirdPersonCharacter.WalkingSpeed.Zombie, 
			_ => ThirdPersonCharacter.WalkingSpeed.Walk, 
		});
	}

	private void SetTimeSpeed(float multiplier)
	{
		Time.timeScale = multiplier;
	}

	public void ResetTimeSpeed()
	{
		timeSpeedMultiplierSlider.value = 1f;
	}

	private void Update()
	{
		tpc.Move(tpc.navmeshAgent.velocity);
	}

	private void MoveToNextPoint()
	{
		_currentPoint++;
		if (_currentPoint >= points.Length)
		{
			_currentPoint = 0;
		}
		StartCoroutine(tpc.MoveToPosition(points[_currentPoint].forward, points[_currentPoint].position, 0.5f, rotateToLookTarget: true, MoveToNextPoint));
	}
}

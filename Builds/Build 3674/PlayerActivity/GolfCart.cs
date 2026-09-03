using BigAmbitions.Characters;
using UnityEngine;

namespace PlayerActivity;

public class GolfCart : MonoBehaviour
{
	[SerializeField]
	private AppearanceSetter driverAppearanceSetter;

	[SerializeField]
	private Vector3[] points;

	[SerializeField]
	private float speedAverage = 3f;

	[SerializeField]
	private float speedVariation = 0.5f;

	[SerializeField]
	private float turnSpeed;

	[SerializeField]
	private AudioSource audioSource;

	[SerializeField]
	private AudioSource voiceAudioSource;

	[SerializeField]
	private AudioClip voiceMale;

	[SerializeField]
	private AudioClip voiceFemale;

	[SerializeField]
	private float voiceDelay = 0.4f;

	private int _nextPointIndex;

	private int _direction = 1;

	private float _speed;

	public bool IsHit { get; private set; }

	private void Awake()
	{
		for (int i = 0; i < points.Length; i++)
		{
			points[i] = base.transform.TransformPoint(points[i]);
		}
	}

	private void Update()
	{
		Vector3 forward = points[_nextPointIndex] - base.transform.position;
		if (forward.sqrMagnitude < 1f)
		{
			_nextPointIndex = (_nextPointIndex + _direction) % points.Length;
			if (_nextPointIndex < 0)
			{
				_nextPointIndex += points.Length;
			}
		}
		Quaternion to = Quaternion.LookRotation(forward);
		base.transform.rotation = Quaternion.RotateTowards(base.transform.rotation, to, turnSpeed * Time.deltaTime);
		base.transform.position += base.transform.forward * (_speed * Time.deltaTime);
	}

	public void Spawn()
	{
		IsHit = false;
		if (!base.gameObject.activeSelf)
		{
			base.gameObject.SetActive(value: true);
			Rigidbody component = GetComponent<Rigidbody>();
			_nextPointIndex = Random.Range(0, points.Length);
			component.position = points[_nextPointIndex];
			_direction = ((Random.value > 0.5f) ? 1 : (-1));
			_speed = Random.Range(speedAverage - speedVariation, speedAverage + speedVariation);
			_nextPointIndex = (_nextPointIndex + _direction) % points.Length;
			if (_nextPointIndex < 0)
			{
				_nextPointIndex += points.Length;
			}
			Vector3 forward = points[_nextPointIndex] - component.position;
			component.rotation = Quaternion.LookRotation(forward);
			driverAppearanceSetter.SetRandomAppearance();
		}
	}

	public void Despawn()
	{
		base.gameObject.SetActive(value: false);
	}

	public void OnHit()
	{
		if (!IsHit)
		{
			IsHit = true;
			audioSource.Play();
			voiceAudioSource.clip = ((driverAppearanceSetter.data.gender == Gender.Male) ? voiceMale : voiceFemale);
			voiceAudioSource.PlayDelayed(voiceDelay);
			SaveGameManager.Current.achievementsData.golfCartHit = true;
			GameEvent.Invoke("ba:gameevent_hitgolfcart");
		}
	}

	public void OnNewTurn()
	{
		IsHit = false;
	}

	private void OnDrawGizmosSelected()
	{
		if (!Application.isPlaying && points != null && points.Length >= 2)
		{
			Gizmos.color = Color.blue;
			for (int i = 0; i < points.Length; i++)
			{
				Vector3 vector = base.transform.TransformPoint(points[i]);
				Vector3 to = base.transform.TransformPoint(points[(i + 1) % points.Length]);
				Gizmos.DrawLine(vector, to);
			}
		}
	}
}

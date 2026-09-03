using System.Collections.Generic;
using BigAmbitions.SoundSystem;
using DG.Tweening;
using JimmysUnityUtilities;
using NaughtyAttributes;
using UnityEngine;

public class ExitZone : MonoBehaviour
{
	private readonly List<Collider> _colliderInExitZone = new List<Collider>();

	public Transform door;

	public ExitZoneDespawner despawner;

	public Transform playerSpawnPoint;

	public Transform vehicleSpawnPoint;

	private bool _doorOpen;

	private Vector3 _doorClosedRotation;

	private Vector3 _doorOpenRotation;

	public bool isDriveInBay;

	private Vector3 _doorClosedPosition;

	private Vector3 _doorOpenPosition;

	public bool isPrimarySpawnPoint;

	public bool invertRotation;

	public bool isElevator;

	private Vector3 _leftDoorClosedPosition;

	private Vector3 _rightDoorClosedPosition;

	private Vector3 _leftDoorOpenPosition;

	private Vector3 _rightDoorOpenPosition;

	public WarehouseSlotController warehouseSlotController;

	public bool ignoreDoor;

	public bool playDoorOpenCloseSound = true;

	[ShowIf("playDoorOpenCloseSound")]
	public SoundType doorCloseSound = SoundType.DoorClose;

	[ShowIf("playDoorOpenCloseSound")]
	public SoundType doorOpenSound = SoundType.DoorOpen;

	private Material[] _doorMaterials;

	private void Awake()
	{
		GetComponentInChildren<MeshRenderer>()?.gameObject.SetActive(value: false);
		if (ignoreDoor)
		{
			return;
		}
		warehouseSlotController = GetComponentInChildren<WarehouseSlotController>();
		Vector3 forward = door.forward;
		int worldRotationOffset = ((!(Mathf.Abs(forward.x) > Mathf.Abs(forward.z))) ? ((forward.z > 0f) ? (-90) : 90) : ((forward.x > 0f) ? 180 : 0));
		if (isDriveInBay)
		{
			_doorClosedPosition = door.position;
			MeshRenderer component = door.GetComponent<MeshRenderer>();
			_doorOpenPosition = _doorClosedPosition + new Vector3(0f, component.bounds.size.y * 0.7f, 0f);
			component.materials.ForEach(delegate(Material doorMaterial)
			{
				doorMaterial.SetFloat(DoorController.WorldRotationOffsetId, worldRotationOffset);
			});
		}
		else if (isElevator)
		{
			_leftDoorClosedPosition = door.GetChild(0).transform.position;
			_rightDoorClosedPosition = door.GetChild(1).transform.position;
			_leftDoorOpenPosition = door.GetChild(0).transform.position + door.GetChild(0).transform.right * 0.9f;
			_rightDoorOpenPosition = door.GetChild(1).transform.position - door.GetChild(1).transform.right * 0.9f;
			door.GetChild(0).GetComponent<MeshRenderer>().materials.ForEach(delegate(Material doorMaterial)
			{
				doorMaterial.SetFloat(DoorController.WorldRotationOffsetId, worldRotationOffset);
			});
			door.GetChild(1).GetComponent<MeshRenderer>().materials.ForEach(delegate(Material doorMaterial)
			{
				doorMaterial.SetFloat(DoorController.WorldRotationOffsetId, worldRotationOffset);
			});
		}
		else
		{
			_doorMaterials = door.GetComponent<MeshRenderer>().materials;
			_doorClosedRotation = door.localRotation.eulerAngles;
			_doorOpenRotation = _doorClosedRotation + new Vector3(0f, invertRotation ? 90 : (-90), 0f);
			if (_doorMaterials != null)
			{
				_doorMaterials.ForEach(delegate(Material mat)
				{
					mat.SetFloat(DoorController.WorldRotationOffsetId, worldRotationOffset);
				});
			}
		}
		InvokeRepeating("UpdateDoorState", 1f, 1f);
	}

	private void OnDisable()
	{
		_colliderInExitZone.Clear();
		if (isElevator)
		{
			door.GetChild(0).DOComplete();
			door.GetChild(1).DOComplete();
		}
		else
		{
			door.DOComplete();
		}
		UpdateDoorState();
		if (isElevator)
		{
			door.GetChild(0).DOComplete();
			door.GetChild(1).DOComplete();
		}
		else
		{
			door.DOComplete();
		}
	}

	private void UpdateDoorState()
	{
		if (ignoreDoor)
		{
			return;
		}
		_colliderInExitZone.RemoveAll((Collider x) => x == null || !x.gameObject.activeSelf);
		bool flag = _colliderInExitZone.Count > 0;
		if (flag == _doorOpen)
		{
			return;
		}
		if (isElevator)
		{
			door.GetChild(0).DOKill();
			door.GetChild(1).DOKill();
		}
		else
		{
			door.DOKill();
		}
		_doorOpen = flag;
		if (_doorMaterials != null)
		{
			_doorMaterials.ForEach(delegate(Material doorMaterial)
			{
				doorMaterial.SetFloat(DoorController.IsDoorOpenId, _doorOpen ? 1 : 0);
			});
		}
		if (_colliderInExitZone.Count > 0)
		{
			if (isElevator)
			{
				door.GetChild(0).DOMove(_leftDoorOpenPosition, 0.7f).SetLink(door.GetChild(0).gameObject);
				door.GetChild(1).DOMove(_rightDoorOpenPosition, 0.7f).SetLink(door.GetChild(1).gameObject);
			}
			else if (isDriveInBay)
			{
				door.DOMove(_doorOpenPosition, 1.2f).SetLink(door.gameObject);
			}
			else
			{
				door.DOLocalRotate(_doorOpenRotation, 1f).SetLink(door.gameObject);
			}
			PlaySound(doorOpenSound);
		}
		else if (isElevator)
		{
			door.GetChild(0).DOMove(_leftDoorClosedPosition, 0.7f).SetLink(door.GetChild(0).gameObject);
			door.GetChild(1).DOMove(_rightDoorClosedPosition, 0.7f).SetLink(door.GetChild(1).gameObject);
			PlaySound(doorCloseSound);
		}
		else if (isDriveInBay)
		{
			door.DOMove(_doorClosedPosition, 1.2f).SetLink(door.gameObject);
			PlaySound(doorCloseSound);
		}
		else
		{
			door.DOLocalRotate(_doorClosedRotation, 1f).SetLink(door.gameObject).OnComplete(delegate
			{
				PlaySound(doorCloseSound);
			});
		}
	}

	private void OnTriggerEnter(Collider other)
	{
		_colliderInExitZone.Add(other);
		UpdateDoorState();
	}

	private void OnTriggerExit(Collider other)
	{
		_colliderInExitZone.Remove(other);
		UpdateDoorState();
	}

	public void Validate()
	{
		if (!ignoreDoor)
		{
			if (!door)
			{
				Debug.Log("Exit Zone does not have Door Assigned", this);
			}
			if (door.gameObject.isStatic && !isElevator)
			{
				Debug.LogError("Exit Zone Door is Static", door);
			}
		}
	}

	public void PlaySound(SoundType type)
	{
		if (playDoorOpenCloseSound)
		{
			InstanceBehavior<SfxManager>.Instance?.PlayAudio(type, door.position);
		}
	}
}

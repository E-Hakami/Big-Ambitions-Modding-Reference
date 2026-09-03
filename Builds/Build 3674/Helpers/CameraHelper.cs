using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cinemachine;
using UnityEngine;

namespace Helpers;

public static class CameraHelper
{
	public const float CameraRotationSpeed = 3f;

	private static CinemachineBrain _brain;

	private static List<CinemachineVirtualCameraBase> AllCameras;

	public static void Init(List<CinemachineVirtualCameraBase> allCameras)
	{
		AllCameras = allCameras;
	}

	public static CinemachineVirtualCameraBase GetCurrentCamera()
	{
		return AllCameras.FirstOrDefault((CinemachineVirtualCameraBase x) => (object)x != null && x.Priority == 1);
	}

	public static CinemachineBrain GetCinemachineBrain()
	{
		if (_brain == null)
		{
			_brain = GameManager.GetMainCamera().GetComponent<CinemachineBrain>();
		}
		return _brain;
	}

	public static IEnumerator SetCameraRoutine(CinemachineVirtualCameraBase camera)
	{
		yield return camera.StartCoroutine(CameraTransition(camera));
	}

	public static void SetCamera(CinemachineVirtualCameraBase camera)
	{
		camera.StartCoroutine(CameraTransition(camera));
	}

	private static IEnumerator CameraTransition(CinemachineVirtualCameraBase camera)
	{
		GetCinemachineBrain();
		CinemachineVirtualCameraBase previousCam = null;
		foreach (CinemachineVirtualCameraBase allCamera in AllCameras)
		{
			if (!(allCamera == null))
			{
				if (allCamera.Priority == 1)
				{
					previousCam = allCamera;
				}
				allCamera.Priority = ((allCamera == camera) ? 1 : 0);
			}
		}
		if (camera != previousCam)
		{
			previousCam?.StopAllCoroutines();
			if (InstanceBehavior<GameManager>.Instance != null && camera != InstanceBehavior<GameManager>.Instance.dummyCamera)
			{
				yield return new WaitUntil(() => !_brain.IsLive(previousCam));
			}
		}
		yield return null;
	}

	public static IEnumerator SetCameraInstant(CinemachineVirtualCameraBase camera)
	{
		CinemachineBrain brain = GetCinemachineBrain();
		CinemachineBlendDefinition prevBlend = brain.m_DefaultBlend;
		brain.m_DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Style.Cut, 0f);
		CinemachineBlenderSettings.CustomBlend[] customBlends = brain.m_CustomBlends.m_CustomBlends;
		CinemachineBlendDefinition.Style[] prevCustomBlendStyles = new CinemachineBlendDefinition.Style[customBlends.Length];
		float[] prevCustomBlendTimes = new float[customBlends.Length];
		for (int i = 0; i < customBlends.Length; i++)
		{
			prevCustomBlendStyles[i] = customBlends[i].m_Blend.m_Style;
			prevCustomBlendTimes[i] = customBlends[i].m_Blend.m_Time;
			customBlends[i].m_Blend.m_Style = CinemachineBlendDefinition.Style.Cut;
			customBlends[i].m_Blend.m_Time = 0f;
		}
		foreach (CinemachineVirtualCameraBase allCamera in AllCameras)
		{
			if (!(allCamera == null))
			{
				if (allCamera.Priority == 1)
				{
					allCamera.StopAllCoroutines();
				}
				allCamera.Priority = ((allCamera == camera) ? 1 : 0);
			}
		}
		yield return null;
		brain.ActiveBlend = null;
		foreach (CinemachineVirtualCameraBase allCamera2 in AllCameras)
		{
			allCamera2.PreviousStateIsValid = false;
			allCamera2.CancelDamping();
		}
		brain.m_DefaultBlend = prevBlend;
		for (int j = 0; j < customBlends.Length; j++)
		{
			customBlends[j].m_Blend.m_Style = prevCustomBlendStyles[j];
			customBlends[j].m_Blend.m_Time = prevCustomBlendTimes[j];
		}
	}

	public static void SetBlendDuration(float duration, string cameraFrom, string cameraTo)
	{
		int num = Array.FindIndex(_brain.m_CustomBlends.m_CustomBlends, (CinemachineBlenderSettings.CustomBlend x) => x.m_From == cameraFrom && x.m_To == cameraTo);
		CinemachineBlenderSettings.CustomBlend customBlend = _brain.m_CustomBlends.m_CustomBlends[num];
		customBlend.m_Blend.m_Time = duration;
		_brain.m_CustomBlends.m_CustomBlends[num] = customBlend;
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetStaticData()
	{
		_brain = null;
		AllCameras = null;
	}
}

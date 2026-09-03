using UnityEngine;

namespace Character;

public class AnimationObjectSpawner : MonoBehaviour
{
	[SerializeField]
	private ThirdPersonCharacter tpc;

	public void SpawnObject(string obj)
	{
		tpc.AddHandObject(obj);
	}

	public void RemoveObject()
	{
		tpc.RemoveHandObject();
	}
}

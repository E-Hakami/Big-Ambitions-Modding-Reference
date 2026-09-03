using UnityEngine;

namespace Culling;

public interface ICullable
{
	void OnLod0();

	void OnLod2();

	void OnLod1();

	BoundingSphere GetCullingBoundingSphere();
}

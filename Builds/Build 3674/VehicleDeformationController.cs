using System;
using System.Collections.Generic;
using System.Linq;
using NaughtyAttributes;
using Unity.Mathematics;
using UnityEngine;

public class VehicleDeformationController : MonoBehaviour
{
	[Serializable]
	public class VehicleDeformation
	{
		[Serializable]
		public class VehicleDeformationPoint
		{
			public SerializableVector3 point;

			public SerializableVector3 normal;

			public float deformationRandomness;
		}

		public float decelerationMagnitude;

		public VehicleDeformationPoint[] points;
	}

	[SerializeField]
	private CarController carController;

	public MeshFilter[] meshFilters;

	public Mesh[] originalMeshes;

	[Range(0f, 2f)]
	public float deformationStrength = 0.25f;

	[Range(0f, 5f)]
	public float deformationRadius = 1f;

	[Range(0f, 1f)]
	public float deformationRandomness = 0.01f;

	private readonly Queue<(int, VehicleDeformation)> _deformationQueue = new Queue<(int, VehicleDeformation)>();

	public void Start()
	{
		originalMeshes = meshFilters.Select((MeshFilter mf) => mf.sharedMesh).ToArray();
		LoadDeformation();
	}

	[Button(null, EButtonEnableMode.Always)]
	public void Reset()
	{
		if ((bool)carController)
		{
			carController.vehicleInstance.deformations.Clear();
		}
		for (int i = 0; i < meshFilters.Length; i++)
		{
			meshFilters[i].mesh = originalMeshes[i];
		}
	}

	private void LoadDeformation()
	{
		foreach (VehicleDeformation deformation in carController.vehicleInstance.deformations)
		{
			for (int i = 0; i < meshFilters.Length; i++)
			{
				DeformMesh(i, deformation);
			}
		}
	}

	private void OnCollisionEnter(Collision collision)
	{
		if (collision.gameObject.CompareTag("Player"))
		{
			return;
		}
		VehicleDeformation vehicleDeformation = new VehicleDeformation
		{
			decelerationMagnitude = collision.relativeVelocity.magnitude * 100f,
			points = collision.contacts.Select((ContactPoint c) => new VehicleDeformation.VehicleDeformationPoint
			{
				point = base.transform.InverseTransformPoint(c.point),
				normal = base.transform.InverseTransformDirection(c.normal),
				deformationRandomness = UnityEngine.Random.Range(1f - deformationRandomness, 1f + deformationRandomness)
			}).ToArray()
		};
		for (int num = 0; num < meshFilters.Length; num++)
		{
			_deformationQueue.Enqueue((num, vehicleDeformation));
		}
		if (!(vehicleDeformation.decelerationMagnitude < 200f))
		{
			while (carController.vehicleInstance.deformations.Count >= 20)
			{
				carController.vehicleInstance.deformations.RemoveAt(0);
			}
			carController.vehicleInstance.deformations.Add(vehicleDeformation);
		}
	}

	private void LateUpdate()
	{
		if (_deformationQueue.Count != 0)
		{
			(int, VehicleDeformation) tuple = _deformationQueue.Dequeue();
			DeformMesh(tuple.Item1, tuple.Item2);
		}
	}

	private void DeformMesh(int meshIndex, VehicleDeformation deformation)
	{
		Vector3[] vertices = meshFilters[meshIndex].mesh.vertices;
		for (int i = 0; i < vertices.Length; i++)
		{
			Vector3 vector = vertices[i];
			Vector3 zero = Vector3.zero;
			for (int j = 0; j < deformation.points.Length; j++)
			{
				Vector3 vector2 = deformation.points[j].point;
				Vector3 vector3 = deformation.points[j].normal;
				float num = math.clamp(deformation.decelerationMagnitude * deformationStrength / 2000f, 0f, deformationRadius);
				float num2 = math.sqrt((vector2.x - vector.x) * (vector2.x - vector.x) + (vector2.z - vector.z) * (vector2.z - vector.z) + (vector2.y - vector.y) * (vector2.y - vector.y));
				num2 *= deformation.points[j].deformationRandomness;
				if (num2 < num)
				{
					zero += vector3 * (num - num2);
				}
			}
			vertices[i] += zero;
		}
		meshFilters[meshIndex].mesh.vertices = vertices;
		meshFilters[meshIndex].mesh.RecalculateTangents();
	}
}

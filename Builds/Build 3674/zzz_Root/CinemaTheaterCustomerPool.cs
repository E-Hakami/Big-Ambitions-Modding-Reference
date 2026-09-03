using UnityEngine;

[CreateAssetMenu(menuName = "BigAmbitions/AI/Pools/Customer/CinemaTheaterCustomerPool", fileName = "CinemaTheaterCustomerPool")]
public class CinemaTheaterCustomerPool : CustomerPool
{
	public override CustomerType GetCustomerType()
	{
		return CustomerType.CinemaTheater;
	}

	protected override string GetPrefabName()
	{
		return "Characters/CinemaTheaterCustomer";
	}

	protected override void ActionOnRelease(Customer customer)
	{
		customer.tpc.SetHandContent(null);
		base.ActionOnRelease(customer);
	}
}

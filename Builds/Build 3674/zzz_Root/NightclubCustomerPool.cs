using UnityEngine;

[CreateAssetMenu(menuName = "BigAmbitions/AI/Pools/Customer/NightclubCustomerPool", fileName = "NightclubCustomerPool")]
public class NightclubCustomerPool : CustomerPool
{
	public override CustomerType GetCustomerType()
	{
		return CustomerType.Nightclub;
	}

	protected override string GetPrefabName()
	{
		return "Characters/NightclubCustomer";
	}

	protected override void ActionOnRelease(Customer customer)
	{
		customer.RemoveDrink();
		customer.tpc.StopHoldingACoat();
		((NightclubCustomer)customer).lastAction = NightclubRandomAction.None;
		base.ActionOnRelease(customer);
		customer.tpc.Reset();
	}
}

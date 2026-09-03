using System.Collections.Generic;
using UnityEngine;

namespace Buildings.Indoors;

public class CustomerSpawner : MonoBehaviour
{
	[SerializeField]
	private CustomerPool[] customerPools;

	private readonly Dictionary<CustomerType, CustomerPool> _customerPoolsDictionary = new Dictionary<CustomerType, CustomerPool>();

	private void InitPoolsDictionary()
	{
		CustomerPool[] array = customerPools;
		foreach (CustomerPool customerPool in array)
		{
			_customerPoolsDictionary.Add(customerPool.GetCustomerType(), customerPool);
		}
	}

	public Customer GetCustomer(CustomerType customerType)
	{
		if (_customerPoolsDictionary.Count == 0)
		{
			InitPoolsDictionary();
		}
		CustomerPool customerPool = _customerPoolsDictionary[customerType];
		if (customerPool == null)
		{
			return null;
		}
		return customerPool.GetPoolHandler().Get();
	}

	public void ReleaseCustomer(Customer customer, CustomerType customerType)
	{
		if (_customerPoolsDictionary.Count == 0)
		{
			InitPoolsDictionary();
		}
		CustomerPool customerPool = _customerPoolsDictionary[customerType];
		if (!(customerPool == null))
		{
			customerPool.GetPoolHandler().Release(customer);
		}
	}
}

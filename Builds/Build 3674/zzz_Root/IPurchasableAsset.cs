using System.Collections;
using System.Collections.Generic;
using Entities;
using UnityEngine;

public interface IPurchasableAsset
{
	string GetLocalizeKey();

	float GetPurchasePrice();

	string GetInitialColor();

	List<(string key, string value)> GetSpecs();

	List<(string, Color32)> GetColors();

	void SetColor(string colorName, bool updateVisuals = true);

	void ResetColor();

	bool Purchase();

	void Order(Address deliveryAddress, Contact storeContact, bool showNotification);

	IEnumerator ShowcaseAnimation();

	IEnumerator CancelShowcaseAnimation();
}

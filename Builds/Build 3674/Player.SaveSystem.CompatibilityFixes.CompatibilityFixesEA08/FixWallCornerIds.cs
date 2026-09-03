using System.Linq;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA08;

public class FixWallCornerIds : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		foreach (BuildingRegistration buildingRegistration in gameInstance.BuildingRegistrations)
		{
			if (!buildingRegistration.BuildingCached || buildingRegistration.interiorDesigns == null)
			{
				continue;
			}
			if (buildingRegistration.BuildingCached.BuildingSize == "ba:buildingsize_a")
			{
				if (buildingRegistration.BuildingCached.BuildingVersion == 1)
				{
					ApplyFixesForA1(buildingRegistration);
				}
				else if (buildingRegistration.BuildingCached.BuildingVersion == 2)
				{
					ApplyFixesForA2(buildingRegistration);
				}
			}
			else if (buildingRegistration.BuildingCached.BuildingSize == "ba:buildingsize_b")
			{
				ApplyFixesForB1(buildingRegistration);
			}
			else if (buildingRegistration.BuildingCached.BuildingSize == "ba:buildingsize_c")
			{
				if (buildingRegistration.BuildingCached.BuildingVersion == 1)
				{
					ApplyFixesForC1(buildingRegistration);
				}
				else if (buildingRegistration.BuildingCached.BuildingVersion == 2)
				{
					ApplyFixesForC2(buildingRegistration);
				}
			}
			else if (buildingRegistration.BuildingCached.BuildingSize == "ba:buildingsize_d")
			{
				if (buildingRegistration.BuildingCached.BuildingVersion == 1)
				{
					ApplyFixesForD1(buildingRegistration);
				}
				else if (buildingRegistration.BuildingCached.BuildingVersion == 2)
				{
					ApplyFixesForD2(buildingRegistration);
				}
			}
			else if (buildingRegistration.BuildingCached.BuildingSize == "ba:buildingsize_f")
			{
				ApplyFixesForF1(buildingRegistration);
			}
			else if (buildingRegistration.BuildingCached.BuildingSize == "ba:buildingsize_j")
			{
				ApplyFixesForJ1(buildingRegistration);
			}
			else if (buildingRegistration.BuildingCached.BuildingSize == "ba:buildingsize_k")
			{
				ApplyFixesForK1(buildingRegistration);
			}
			else if (buildingRegistration.BuildingCached.BuildingSize == "ba:buildingsize_l")
			{
				ApplyFixesForL1(buildingRegistration);
			}
			else if (buildingRegistration.BuildingCached.BuildingSize == "ba:buildingsize_m")
			{
				ApplyFixesForM1(buildingRegistration);
			}
			else if (buildingRegistration.BuildingCached.BuildingSize == "ba:buildingsize_n")
			{
				ApplyFixesForN1(buildingRegistration);
			}
		}
	}

	private void ApplyFixesForA1(BuildingRegistration registration)
	{
		ReplaceDesign(registration, "CtLq6ZQToEyhMX76dF+cgw==", 3, "f00xHIMRPkq+gEVwQBGyvQ==", 1, removeOldDesign: false);
		ReplaceDesign(registration, "CtLq6ZQToEyhMX76dF+cgw==", 1, "CtLq6ZQToEyhMX76dF+cgw==", 1);
	}

	private void ApplyFixesForA2(BuildingRegistration registration)
	{
		ReplaceDesign(registration, "NYuVnTW60EmtXlLHeEhomw==", 1, "xtiu2lAVU2iNzfXGw0FNg==", 1, removeOldDesign: false);
		ReplaceDesign(registration, "NYuVnTW60EmtXlLHeEhomw==", 3, "nc9dehsUO0WMCAEziIQxw==", 1);
		ReplaceDesign(registration, "L+mKHaUdCEuDMZHeAFR/zA==", 1, "Xt2VWX+DmUSTbTFid1s6Rg==", 1, removeOldDesign: false);
		ReplaceDesign(registration, "L+mKHaUdCEuDMZHeAFR/zA==", 3, "CIvLH8ErzUKrb3rrCWUpEQ==", 1);
		ReplaceDesign(registration, "9kYsYH4MMUSzQzMiBWuYrQ==", 1, "yxrUMNf71UqgciNqIVFW7w==", 1, removeOldDesign: false);
		ReplaceDesign(registration, "9kYsYH4MMUSzQzMiBWuYrQ==", 3, "d6Hmvwm+8UKyZkh6UBPGKg==", 1);
	}

	private void ApplyFixesForB1(BuildingRegistration registration)
	{
		ReplaceDesign(registration, "QlfGKcimxEe1L3gNezDdaw==", 1, "lJ1o0V0NW0aMIP4Ut3gU4A==", 1, removeOldDesign: false);
		ReplaceDesign(registration, "QlfGKcimxEe1L3gNezDdaw==", 3, "nMUMrsX8E2AxfkfhlW00A==", 1);
		ReplaceDesign(registration, "HhKAC7TX60mnu2+uNwGIkw==", 1, "8Nx+BS5Gw0yuqnG0mbvvsA==", 1, removeOldDesign: false);
		ReplaceDesign(registration, "HhKAC7TX60mnu2+uNwGIkw==", 3, "YhEGy92QHk20c+RhYrI4Cw==", 1);
		ReplaceDesign(registration, "M9B6RO4ioUmqHMmsniAylQ==", 1, "LwNpieldk2DS0cUtmwgw==", 1, removeOldDesign: false);
		ReplaceDesign(registration, "M9B6RO4ioUmqHMmsniAylQ==", 3, "i9pPnGU78U6qXtVh04rTCw==", 1);
		ReplaceDesign(registration, "llzXN8xZBUS/z3qbFo7nXg==", 1, "p7It46bhVkG8wfpWm0A6Q==", 1, removeOldDesign: false);
		ReplaceDesign(registration, "llzXN8xZBUS/z3qbFo7nXg==", 3, "XLFylrIbWUy3Ghk00Ghsw==", 1);
	}

	private void ApplyFixesForC1(BuildingRegistration registration)
	{
		ReplaceDesign(registration, "7lp8ZobVm0a1GGPS24oEcw==", 4, "l0yMqAcVyEmCghHObugZww==", 1, removeOldDesign: false);
		ReplaceDesign(registration, "7lp8ZobVm0a1GGPS24oEcw==", 2, "ZIZHQn4Qbk2n8qEBa7S6rA==", 2);
		ReplaceDesign(registration, "mIciUcwBM0qC81V79QoWzA==", 2, "XKsyRetazEiFKFE+c0YdwA==", 2, removeOldDesign: false);
		ReplaceDesign(registration, "mIciUcwBM0qC81V79QoWzA==", 4, "SuGudFU6WEynUa3444uQA==", 1);
		ReplaceDesign(registration, "iENT8E10F0W+P+7j1JIWoQ==", 1, "ibLp8YP7Eq8RQ0mvv4Nhw==", 1, removeOldDesign: false);
		ReplaceDesign(registration, "iENT8E10F0W+P+7j1JIWoQ==", 3, "FG+F1q8970GIopmWypgyXw==", 1);
	}

	private void ApplyFixesForC2(BuildingRegistration registration)
	{
		ReplaceDesign(registration, "81Qo8PYe3U2lUPOEClbtMg==", 4, "iqbqeMNrGUWk8ha390KwSQ==", 1, removeOldDesign: false);
		ReplaceDesign(registration, "81Qo8PYe3U2lUPOEClbtMg==", 2, "m6Gwcf5+pUOueIGORY79Q==", 2);
		ReplaceDesign(registration, "kFM+09XZTEiiQHgYX7JJpQ==", 2, "P7ooKPSsB0aiqkl91g4FkA==", 2, removeOldDesign: false);
		ReplaceDesign(registration, "kFM+09XZTEiiQHgYX7JJpQ==", 4, "gcHmQ8MykEqS87KOmPuIFg==", 1);
		ReplaceDesign(registration, "NWmtazDJXESyXkES+aOulw==", 1, "s3uUUR05zEOizIXAijcRGA==", 1, removeOldDesign: false);
		ReplaceDesign(registration, "NWmtazDJXESyXkES+aOulw==", 3, "2NfJ+DybIkOG2drAG7maQA==", 1);
	}

	private void ApplyFixesForD1(BuildingRegistration registration)
	{
		ReplaceDesign(registration, "PY1u0zqrmUaalPB7ZUM3w==", 1, "2PTQtg+EmUi19lsGDP52SA==", 1, removeOldDesign: false);
		ReplaceDesign(registration, "PY1u0zqrmUaalPB7ZUM3w==", 3, "YCQ18NzoW0Wu8OXL+uWMgg==", 1);
		ReplaceDesign(registration, "czzUv5ZXL0aaaHERC7LHQ==", 1, "gKLaBiLn2UmaXuTSLLgYKw==", 1, removeOldDesign: false);
		ReplaceDesign(registration, "czzUv5ZXL0aaaHERC7LHQ==", 3, "kv5XaY1lqEKWpDQCfGOWXw==", 1);
		ReplaceDesign(registration, "A8KThI0H9UeKzYJV8WQLdw==", 1, "IFWk9EQYDkW+WB4wdD7GFQ==", 1, removeOldDesign: false);
		ReplaceDesign(registration, "A8KThI0H9UeKzYJV8WQLdw==", 3, "+auMC2aBXkabyIpxbk2wsQ==", 1);
		ReplaceDesign(registration, "VE1eljPEuUeOAbZkcVVelQ==", 1, "+CpXJRmozE28oxgmyFD2Xw==", 1, removeOldDesign: false);
		ReplaceDesign(registration, "VE1eljPEuUeOAbZkcVVelQ==", 3, "4XMcBrDnyUWTdghexW8ag==", 1);
	}

	private void ApplyFixesForD2(BuildingRegistration registration)
	{
		ReplaceDesign(registration, "KbBqgQCy1k2MyT36915IMw==", 1, "fot55z6T+0KQCz7rqer8Tw==", 1, removeOldDesign: false);
		ReplaceDesign(registration, "KbBqgQCy1k2MyT36915IMw==", 3, "cCH5aqpwDEKqAMhvKEP4Wg==", 1);
		ReplaceDesign(registration, "DkxTX2atiUu3zrq0GeKz2g==", 1, "1651FlNFw0KIqhFwgcmi7A==", 1, removeOldDesign: false);
		ReplaceDesign(registration, "DkxTX2atiUu3zrq0GeKz2g==", 3, "iCSCujNKp0q9Dusx+N1RlQ==", 1);
		ReplaceDesign(registration, "kG+mJEud0UariQ9kMzmP5g==", 1, "iMZ59mrny0O8TVARB1eIQ==", 1, removeOldDesign: false);
		ReplaceDesign(registration, "kG+mJEud0UariQ9kMzmP5g==", 3, "Mlxkeq51tUaMcfgD6W5joA==", 1);
		ReplaceDesign(registration, "isF7P01uTUS5El6Luxd7BQ==", 1, "85M7YOXE+EGUp7MRLd56pQ==", 1, removeOldDesign: false);
		ReplaceDesign(registration, "isF7P01uTUS5El6Luxd7BQ==", 3, "6gP4slyvB0um7GEmS0By5g==", 1);
	}

	private void ApplyFixesForF1(BuildingRegistration registration)
	{
		ReplaceDesign(registration, "q4dbIvtVBEKbw2tbvEvnXw==", 1, "tHgTFnYhhE2ftsZRShMOg==", 1, removeOldDesign: false);
		ReplaceDesign(registration, "q4dbIvtVBEKbw2tbvEvnXw==", 3, "irPEEMmvDkSxt0T8JAfMg==", 1);
		ReplaceDesign(registration, "eTeMhjUCIEG9E6cMv67QGg==", 1, "B3MG9GhLG0KKImNf6VDwMQ==", 1, removeOldDesign: false);
		ReplaceDesign(registration, "eTeMhjUCIEG9E6cMv67QGg==", 3, "MTkr4WDFXEOVBQfjh9XKgQ==", 1);
		ReplaceDesign(registration, "Cvh5//fqk0m6incNMJpDRg==", 1, "haUMAPIZukyeyo9OlxfuLA==", 1, removeOldDesign: false);
		ReplaceDesign(registration, "Cvh5//fqk0m6incNMJpDRg==", 3, "+ThfObSySk6hAet+YYUqYw==", 1);
		ReplaceDesign(registration, "CuddJXis3EadEKisKpb8HQ==", 1, "svb6ciRT2kCmOah4eZGl+A==", 1, removeOldDesign: false);
		ReplaceDesign(registration, "CuddJXis3EadEKisKpb8HQ==", 3, "bZdsmqoVt0+PYQmPw0vtQ==", 1);
	}

	private void ApplyFixesForJ1(BuildingRegistration registration)
	{
		ReplaceDesign(registration, "UoBJ0SEUTU+CScndYIGtpA==", 1, "mYNtGAQh4UWI0SXrQ6FyrQ==", 1, removeOldDesign: false);
		ReplaceDesign(registration, "UoBJ0SEUTU+CScndYIGtpA==", 3, "utXMrzWcU2U2aD9J4Nkug==", 1);
		ReplaceDesign(registration, "dbIaIi5tak+ZU6gDSpt37g==", 1, "VDO1Wt1uTE+7KCExLZZRSQ==", 1, removeOldDesign: false);
		ReplaceDesign(registration, "dbIaIi5tak+ZU6gDSpt37g==", 3, "Xm41MjgBGUulBl3ebjxog==", 1);
		ReplaceDesign(registration, "gUyhOXEzGE6rMV7riaSPdQ==", 1, "126LRGRwfkm+yEhT7N9VfA==", 1, removeOldDesign: false);
		ReplaceDesign(registration, "gUyhOXEzGE6rMV7riaSPdQ==", 3, "41jeQnoSPk2wztBWlzUSKA==", 1);
		ReplaceDesign(registration, "SgqpFtqV20ib0gsce9Ea6g==", 1, "2pwYqE3BkEuTh5VI7VRGA==", 1, removeOldDesign: false);
		ReplaceDesign(registration, "SgqpFtqV20ib0gsce9Ea6g==", 3, "faB2x2zqdEiX7wll23vOeg==", 1);
	}

	private void ApplyFixesForK1(BuildingRegistration registration)
	{
		ReplaceDesign(registration, "aMh8aJ38Lk2l6UUs9c4Dqg==", 1, "+y9S4uT6vUKgTkjHnBVM5Q==", 1, removeOldDesign: false);
		ReplaceDesign(registration, "aMh8aJ38Lk2l6UUs9c4Dqg==", 3, "KQZDStAkB0WvN96xO5yHZQ==", 1);
		ReplaceDesign(registration, "lSZ9y+X6p0WS1cZGUv1Mug==", 1, "w7TZU7rH0KaOKjmwlF7g==", 1, removeOldDesign: false);
		ReplaceDesign(registration, "lSZ9y+X6p0WS1cZGUv1Mug==", 3, "+JQQK8t9fU6YGtAi7oKjlQ==", 1);
		ReplaceDesign(registration, "ExIIvF5wkE6UYeyMbCEvoQ==", 1, "eox63dwb0EC33gul+2W8Rw==", 1, removeOldDesign: false);
		ReplaceDesign(registration, "ExIIvF5wkE6UYeyMbCEvoQ==", 3, "zcCCgWCi00GL+PVglaR+5w==", 1);
	}

	private void ApplyFixesForL1(BuildingRegistration registration)
	{
		ReplaceDesign(registration, "66K4o2ag8UqNWe8Dnv5m5Q==", 1, "67GOfu4wZk6UkFmpYINY8g==", 1, removeOldDesign: false);
		ReplaceDesign(registration, "66K4o2ag8UqNWe8Dnv5m5Q==", 3, "s0sUDnHwEqDkLcQLGj06g==", 1);
		ReplaceDesign(registration, "ALjZHzZDVk6bB+7FtuHBg==", 1, "VEOAdmOKMEmFoUFH6QBNA==", 1, removeOldDesign: false);
		ReplaceDesign(registration, "ALjZHzZDVk6bB+7FtuHBg==", 3, "2HkbtxQkJUekqyhDEey4Lg==", 1);
		ReplaceDesign(registration, "m0KchJCVdECZ1Sopylo85A==", 1, "3g3HP63aPEO85DfCQMjag==", 1, removeOldDesign: false);
		ReplaceDesign(registration, "m0KchJCVdECZ1Sopylo85A==", 3, "DngB+MlKFUqzgVvom1VFnA==", 1);
		ReplaceDesign(registration, "26MdXKYaJE+ZCzm1CiWAzg==", 1, "RPprhesxoE2rnlVFLiYWJw==", 1, removeOldDesign: false);
		ReplaceDesign(registration, "26MdXKYaJE+ZCzm1CiWAzg==", 3, "uTYr5AZ2MEGX02wK44pJqA==", 1);
	}

	private void ApplyFixesForM1(BuildingRegistration registration)
	{
		ReplaceDesign(registration, "Q4SqMb9UiMOIkpDB0PaQ==", 2, "26F6HdRe1UiIWKfybEKw==", 2, removeOldDesign: false);
		ReplaceDesign(registration, "Q4SqMb9UiMOIkpDB0PaQ==", 4, "WMAM0MQeUSbShgETIcXQ==", 1);
		ReplaceDesign(registration, "4PASJRIht02FtaCoT8CkoQ==", 1, "Ufu6ksg7G0+H5rRIywRUw==", 1, removeOldDesign: false);
		ReplaceDesign(registration, "4PASJRIht02FtaCoT8CkoQ==", 3, "jSnmLrDBNUqxjUt0o1W74w==", 1);
		ReplaceDesign(registration, "KAMI4Yc6HUeZaSCYgnmctQ==", 1, "DyP8t0slEWTqYkFf6CfmQ==", 1, removeOldDesign: false);
		ReplaceDesign(registration, "KAMI4Yc6HUeZaSCYgnmctQ==", 3, "8xMdRErfHkSX9Z08auEgA==", 1);
		ReplaceDesign(registration, "cKJkAm6T0OqAfYVUrigjA==", 4, "XmvRouT8B0SKuxTbmwAiYA==", 1, removeOldDesign: false);
		ReplaceDesign(registration, "cKJkAm6T0OqAfYVUrigjA==", 2, "WKTJbQZM9Umr7SQaFirCSA==", 2);
	}

	private void ApplyFixesForN1(BuildingRegistration registration)
	{
		ReplaceDesign(registration, "LN9t6D8akU2+6wghLrljcQ==", 1, "nUfVuDzjH06JYh2vgOxDzA==", 1, removeOldDesign: false);
		ReplaceDesign(registration, "LN9t6D8akU2+6wghLrljcQ==", 3, "TYfwuRoP0mvsS7OUiqV2g==", 1);
		ReplaceDesign(registration, "JQsWZo56k6EZa1Q1hloCg==", 1, "0sQnASCKkqVdMB18m0q7Q==", 1, removeOldDesign: false);
		ReplaceDesign(registration, "JQsWZo56k6EZa1Q1hloCg==", 3, "ufGSLFWJ2U+Qa+BcaCH8MQ==", 1);
		ReplaceDesign(registration, "phYng2NDHUiEhk+1B3aUIA==", 1, "qRG235AbOUSrfDDgTvgGg==", 1, removeOldDesign: false);
		ReplaceDesign(registration, "phYng2NDHUiEhk+1B3aUIA==", 3, "xpRSReNBg0Gr9iVbLS8uVw==", 1);
		ReplaceDesign(registration, "FyRJl4YOe02GI4XNNAXFxA==", 1, "gZUosab6fkOJsh4WaN6gyQ==", 1, removeOldDesign: false);
		ReplaceDesign(registration, "FyRJl4YOe02GI4XNNAXFxA==", 3, "AHRQlUqCVU+w3o+SibOEYw==", 1);
	}

	private static void ReplaceDesign(BuildingRegistration registration, string oldDesignId, int oldMaterialIndex, string newDesignId, int newMaterialIndex, bool removeOldDesign = true)
	{
		SerializedInteriorDesign serializedInteriorDesign = registration.interiorDesigns.FirstOrDefault((SerializedInteriorDesign x) => x.UUID == oldDesignId);
		if (serializedInteriorDesign == null)
		{
			return;
		}
		if (!serializedInteriorDesign.materials.Any((SerializedInteriorDesign.SerializableInteriorMaterial x) => x.MaterialIndex == oldMaterialIndex))
		{
			if (removeOldDesign)
			{
				registration.interiorDesigns.Remove(serializedInteriorDesign);
			}
			return;
		}
		SerializedInteriorDesign.SerializableInteriorMaterial serializableInteriorMaterial = serializedInteriorDesign.materials.First((SerializedInteriorDesign.SerializableInteriorMaterial x) => x.MaterialIndex == oldMaterialIndex);
		SerializedInteriorDesign serializedInteriorDesign2 = new SerializedInteriorDesign();
		serializedInteriorDesign2.UUID = newDesignId;
		serializedInteriorDesign2.materials = new SerializedInteriorDesign.SerializableInteriorMaterial[1]
		{
			new SerializedInteriorDesign.SerializableInteriorMaterial
			{
				MaterialID = serializableInteriorMaterial.MaterialID,
				MaterialIndex = newMaterialIndex,
				ColorIndex = serializableInteriorMaterial.ColorIndex
			}
		};
		SerializedInteriorDesign item = serializedInteriorDesign2;
		if (removeOldDesign)
		{
			registration.interiorDesigns.Remove(serializedInteriorDesign);
		}
		registration.interiorDesigns.Add(item);
	}
}

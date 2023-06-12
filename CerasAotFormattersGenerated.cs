
// ReSharper disable All

#nullable disable
#pragma warning disable 649

using System.Collections.Generic;
using Ceras;
using Ceras.Formatters;
using Ceras.Formatters.AotGenerator;

namespace AotSerialization
{
	internal class HRWateringCanFormatter : IFormatter<HRWateringCan>
	{
		CerasSerializer Serializer;
		CerasSerializer.SchemaDeserializer SchemaDeserializer;
		IFormatter<int> _intFormatter;

		public void Serialize(ref byte[] buffer, ref int offset, HRWateringCan value)
		{
			Serializer.SerializeSchema<HRWateringCan>(ref buffer, ref offset, "CurrentBulletCount_Saved");
			Serializer.SchemaSerialize(_intFormatter, ref buffer, ref offset, value.CurrentBulletCount_Saved);
		}

		public void Deserialize(byte[] buffer, ref int offset, ref HRWateringCan value)
		{
			Serializer.DeserializeSchema<HRWateringCan>(ref SchemaDeserializer, buffer, ref offset, "CurrentBulletCount_Saved");
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _intFormatter, buffer, ref offset, ref value.CurrentBulletCount_Saved);
		}
	}

	internal class BaseAchievementManagerFormatter : IFormatter<BaseAchievementManager>
	{
		CerasSerializer Serializer;
		CerasSerializer.SchemaDeserializer SchemaDeserializer;
		IFormatter<System.Collections.Generic.HashSet<string>> _hashSet_stringFormatter;

		public void Serialize(ref byte[] buffer, ref int offset, BaseAchievementManager value)
		{
			Serializer.SerializeSchema<BaseAchievementManager>(ref buffer, ref offset, "unlockedAchievements");
			Serializer.SchemaSerialize(_hashSet_stringFormatter, ref buffer, ref offset, value.unlockedAchievements);
		}

		public void Deserialize(byte[] buffer, ref int offset, ref BaseAchievementManager value)
		{
			Serializer.DeserializeSchema<BaseAchievementManager>(ref SchemaDeserializer, buffer, ref offset, "unlockedAchievements");
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _hashSet_stringFormatter, buffer, ref offset, ref value.unlockedAchievements);
		}
	}

	internal class BaseArmorComponentFormatter : IFormatter<BaseArmorComponent>
	{
		CerasSerializer Serializer;
		CerasSerializer.SchemaDeserializer SchemaDeserializer;
		IFormatter<float> _floatFormatter;

		public void Serialize(ref byte[] buffer, ref int offset, BaseArmorComponent value)
		{
			Serializer.SerializeSchema<BaseArmorComponent>(ref buffer, ref offset, "CurrentHP");
			Serializer.SchemaSerialize(_floatFormatter, ref buffer, ref offset, value.CurrentHP);
		}

		public void Deserialize(byte[] buffer, ref int offset, ref BaseArmorComponent value)
		{
			Serializer.DeserializeSchema<BaseArmorComponent>(ref SchemaDeserializer, buffer, ref offset, "CurrentHP");
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _floatFormatter, buffer, ref offset, ref value.CurrentHP);
		}
	}

	internal class BaseArmorManagerFormatter : IFormatter<BaseArmorManager>
	{
		CerasSerializer Serializer;
		CerasSerializer.SchemaDeserializer SchemaDeserializer;
		IFormatter<float> _floatFormatter;

		public void Serialize(ref byte[] buffer, ref int offset, BaseArmorManager value)
		{
			Serializer.SerializeSchema<BaseArmorManager>(ref buffer, ref offset, "CurrentHP");
			Serializer.SchemaSerialize(_floatFormatter, ref buffer, ref offset, value.CurrentHP);
		}

		public void Deserialize(byte[] buffer, ref int offset, ref BaseArmorManager value)
		{
			Serializer.DeserializeSchema<BaseArmorManager>(ref SchemaDeserializer, buffer, ref offset, "CurrentHP");
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _floatFormatter, buffer, ref offset, ref value.CurrentHP);
		}
	}

	internal class BaseHPFormatter : IFormatter<BaseHP>
	{
		CerasSerializer Serializer;
		CerasSerializer.SchemaDeserializer SchemaDeserializer;
		IFormatter<float> _floatFormatter;

		public void Serialize(ref byte[] buffer, ref int offset, BaseHP value)
		{
			Serializer.SerializeSchema<BaseHP>(ref buffer, ref offset, "SavedMaxHP", "CurrentHP");
			Serializer.SchemaSerialize(_floatFormatter, ref buffer, ref offset, value.SavedMaxHP);
			Serializer.SchemaSerialize(_floatFormatter, ref buffer, ref offset, value.CurrentHP);
		}

		public void Deserialize(byte[] buffer, ref int offset, ref BaseHP value)
		{
			Serializer.DeserializeSchema<BaseHP>(ref SchemaDeserializer, buffer, ref offset, "SavedMaxHP", "CurrentHP");
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _floatFormatter, buffer, ref offset, ref value.SavedMaxHP);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _floatFormatter, buffer, ref offset, ref value.CurrentHP);
		}
	}

	internal class BaseProjectilePhysicsFormatter : IFormatter<BaseProjectilePhysics>
	{
		CerasSerializer Serializer;
		CerasSerializer.SchemaDeserializer SchemaDeserializer;
		IFormatter<string> _stringFormatter;
		IFormatter<bool> _boolFormatter;

		public void Serialize(ref byte[] buffer, ref int offset, BaseProjectilePhysics value)
		{
			Serializer.SerializeSchema<BaseProjectilePhysics>(ref buffer, ref offset, "SavedRestingID", "bIsEnabled", "bIsAtRest", "bFirstDrop", "bRestingOnTerrain");
			Serializer.SchemaSerialize(_stringFormatter, ref buffer, ref offset, value.SavedRestingID);
			Serializer.SchemaSerialize(_boolFormatter, ref buffer, ref offset, value.bIsEnabled);
			Serializer.SchemaSerialize(_boolFormatter, ref buffer, ref offset, value.bIsAtRest);
			Serializer.SchemaSerialize(_boolFormatter, ref buffer, ref offset, value.bFirstDrop);
			Serializer.SchemaSerialize(_boolFormatter, ref buffer, ref offset, value.bRestingOnTerrain);
		}

		public void Deserialize(byte[] buffer, ref int offset, ref BaseProjectilePhysics value)
		{
			Serializer.DeserializeSchema<BaseProjectilePhysics>(ref SchemaDeserializer, buffer, ref offset, "SavedRestingID", "bIsEnabled", "bIsAtRest", "bFirstDrop", "bRestingOnTerrain");
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _stringFormatter, buffer, ref offset, ref value.SavedRestingID);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _boolFormatter, buffer, ref offset, ref value.bIsEnabled);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _boolFormatter, buffer, ref offset, ref value.bIsAtRest);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _boolFormatter, buffer, ref offset, ref value.bFirstDrop);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _boolFormatter, buffer, ref offset, ref value.bRestingOnTerrain);
		}
	}

	internal class BaseAreaReplacerFormatter : IFormatter<BaseAreaReplacer>
	{
		CerasSerializer Serializer;
		CerasSerializer.SchemaDeserializer SchemaDeserializer;
		IFormatter<bool> _boolFormatter;
		IFormatter<int> _intFormatter;

		public void Serialize(ref byte[] buffer, ref int offset, BaseAreaReplacer value)
		{
			Serializer.SerializeSchema<BaseAreaReplacer>(ref buffer, ref offset, "bShouldSpawnOnNextDay", "bIsDirty", "DayCounter");
			Serializer.SchemaSerialize(_boolFormatter, ref buffer, ref offset, value.bShouldSpawnOnNextDay);
			Serializer.SchemaSerialize(_boolFormatter, ref buffer, ref offset, value.bIsDirty);
			Serializer.SchemaSerialize(_intFormatter, ref buffer, ref offset, value.DayCounter);
		}

		public void Deserialize(byte[] buffer, ref int offset, ref BaseAreaReplacer value)
		{
			Serializer.DeserializeSchema<BaseAreaReplacer>(ref SchemaDeserializer, buffer, ref offset, "bShouldSpawnOnNextDay", "bIsDirty", "DayCounter");
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _boolFormatter, buffer, ref offset, ref value.bShouldSpawnOnNextDay);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _boolFormatter, buffer, ref offset, ref value.bIsDirty);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _intFormatter, buffer, ref offset, ref value.DayCounter);
		}
	}

	internal class BaseColorCustomizationComponentFormatter : IFormatter<BaseColorCustomizationComponent>
	{
		CerasSerializer Serializer;
		CerasSerializer.SchemaDeserializer SchemaDeserializer;
		IFormatter<string> _stringFormatter;

		public void Serialize(ref byte[] buffer, ref int offset, BaseColorCustomizationComponent value)
		{
			Serializer.SerializeSchema<BaseColorCustomizationComponent>(ref buffer, ref offset, "SavedCustomizationCode");
			Serializer.SchemaSerialize(_stringFormatter, ref buffer, ref offset, value.SavedCustomizationCode);
		}

		public void Deserialize(byte[] buffer, ref int offset, ref BaseColorCustomizationComponent value)
		{
			Serializer.DeserializeSchema<BaseColorCustomizationComponent>(ref SchemaDeserializer, buffer, ref offset, "SavedCustomizationCode");
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _stringFormatter, buffer, ref offset, ref value.SavedCustomizationCode);
		}
	}

	internal class BaseAutomaticDoorFormatter : IFormatter<BaseAutomaticDoor>
	{
		CerasSerializer Serializer;
		CerasSerializer.SchemaDeserializer SchemaDeserializer;
		IFormatter<bool> _boolFormatter;

		public void Serialize(ref byte[] buffer, ref int offset, BaseAutomaticDoor value)
		{
			Serializer.SerializeSchema<BaseAutomaticDoor>(ref buffer, ref offset, "bSavedHasOpened");
			Serializer.SchemaSerialize(_boolFormatter, ref buffer, ref offset, value.bSavedHasOpened);
		}

		public void Deserialize(byte[] buffer, ref int offset, ref BaseAutomaticDoor value)
		{
			Serializer.DeserializeSchema<BaseAutomaticDoor>(ref SchemaDeserializer, buffer, ref offset, "bSavedHasOpened");
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _boolFormatter, buffer, ref offset, ref value.bSavedHasOpened);
		}
	}

	internal class BaseManualDoorFormatter : IFormatter<BaseManualDoor>
	{
		CerasSerializer Serializer;
		CerasSerializer.SchemaDeserializer SchemaDeserializer;
		IFormatter<bool> _boolFormatter;

		public void Serialize(ref byte[] buffer, ref int offset, BaseManualDoor value)
		{
			Serializer.SerializeSchema<BaseManualDoor>(ref buffer, ref offset, "bSavedHasOpened");
			Serializer.SchemaSerialize(_boolFormatter, ref buffer, ref offset, value.bSavedHasOpened);
		}

		public void Deserialize(byte[] buffer, ref int offset, ref BaseManualDoor value)
		{
			Serializer.DeserializeSchema<BaseManualDoor>(ref SchemaDeserializer, buffer, ref offset, "bSavedHasOpened");
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _boolFormatter, buffer, ref offset, ref value.bSavedHasOpened);
		}
	}

	internal class BaseGunComponentFormatter : IFormatter<BaseGunComponent>
	{
		CerasSerializer Serializer;
		CerasSerializer.SchemaDeserializer SchemaDeserializer;
		IFormatter<int> _intFormatter;

		public void Serialize(ref byte[] buffer, ref int offset, BaseGunComponent value)
		{
			Serializer.SerializeSchema<BaseGunComponent>(ref buffer, ref offset, "CurrentBulletCount_Saved");
			Serializer.SchemaSerialize(_intFormatter, ref buffer, ref offset, value.CurrentBulletCount_Saved);
		}

		public void Deserialize(byte[] buffer, ref int offset, ref BaseGunComponent value)
		{
			Serializer.DeserializeSchema<BaseGunComponent>(ref SchemaDeserializer, buffer, ref offset, "CurrentBulletCount_Saved");
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _intFormatter, buffer, ref offset, ref value.CurrentBulletCount_Saved);
		}
	}

	internal class BaseLockedItemFormatter : IFormatter<BaseLockedItem>
	{
		CerasSerializer Serializer;
		CerasSerializer.SchemaDeserializer SchemaDeserializer;
		IFormatter<int> _intFormatter;
		IFormatter<bool> _boolFormatter;

		public void Serialize(ref byte[] buffer, ref int offset, BaseLockedItem value)
		{
			Serializer.SerializeSchema<BaseLockedItem>(ref buffer, ref offset, "SavedSecurityLevel", "bSavedLocked");
			Serializer.SchemaSerialize(_intFormatter, ref buffer, ref offset, value.SavedSecurityLevel);
			Serializer.SchemaSerialize(_boolFormatter, ref buffer, ref offset, value.bSavedLocked);
		}

		public void Deserialize(byte[] buffer, ref int offset, ref BaseLockedItem value)
		{
			Serializer.DeserializeSchema<BaseLockedItem>(ref SchemaDeserializer, buffer, ref offset, "SavedSecurityLevel", "bSavedLocked");
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _intFormatter, buffer, ref offset, ref value.SavedSecurityLevel);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _boolFormatter, buffer, ref offset, ref value.bSavedLocked);
		}
	}

	internal class BaseMortarComponentFormatter : IFormatter<BaseMortarComponent>
	{
		CerasSerializer Serializer;
		CerasSerializer.SchemaDeserializer SchemaDeserializer;
		IFormatter<int> _intFormatter;

		public void Serialize(ref byte[] buffer, ref int offset, BaseMortarComponent value)
		{
			Serializer.SerializeSchema<BaseMortarComponent>(ref buffer, ref offset, "CurrentBulletCount_Saved");
			Serializer.SchemaSerialize(_intFormatter, ref buffer, ref offset, value.CurrentBulletCount_Saved);
		}

		public void Deserialize(byte[] buffer, ref int offset, ref BaseMortarComponent value)
		{
			Serializer.DeserializeSchema<BaseMortarComponent>(ref SchemaDeserializer, buffer, ref offset, "CurrentBulletCount_Saved");
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _intFormatter, buffer, ref offset, ref value.CurrentBulletCount_Saved);
		}
	}

	internal class BaseProjectileGunFormatter : IFormatter<BaseProjectileGun>
	{
		CerasSerializer Serializer;
		CerasSerializer.SchemaDeserializer SchemaDeserializer;
		IFormatter<int> _intFormatter;

		public void Serialize(ref byte[] buffer, ref int offset, BaseProjectileGun value)
		{
			Serializer.SerializeSchema<BaseProjectileGun>(ref buffer, ref offset, "CurrentBulletCount_Saved");
			Serializer.SchemaSerialize(_intFormatter, ref buffer, ref offset, value.CurrentBulletCount_Saved);
		}

		public void Deserialize(byte[] buffer, ref int offset, ref BaseProjectileGun value)
		{
			Serializer.DeserializeSchema<BaseProjectileGun>(ref SchemaDeserializer, buffer, ref offset, "CurrentBulletCount_Saved");
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _intFormatter, buffer, ref offset, ref value.CurrentBulletCount_Saved);
		}
	}

	internal class BaseWeaponFormatter : IFormatter<BaseWeapon>
	{
		CerasSerializer Serializer;
		CerasSerializer.SchemaDeserializer SchemaDeserializer;
		IFormatter<int> _intFormatter;
		IFormatter<bool> _boolFormatter;
		IFormatter<byte> _byteFormatter;
		IFormatter<System.Collections.Generic.HashSet<System.ValueTuple<string, long>>> _hashSet_ValueTupleOfstringANDlongFormatter;

		public void Serialize(ref byte[] buffer, ref int offset, BaseWeapon value)
		{
			Serializer.SerializeSchema<BaseWeapon>(ref buffer, ref offset, "SavedStackCount", "bRandomizeRarityOnStart", "SavedItemRarity", "bHasSaveData", "bWasCrafted", "ClientsInstancedCopyTaken");
			Serializer.SchemaSerialize(_intFormatter, ref buffer, ref offset, value.SavedStackCount);
			Serializer.SchemaSerialize(_boolFormatter, ref buffer, ref offset, value.bRandomizeRarityOnStart);
			Serializer.SchemaSerialize(_byteFormatter, ref buffer, ref offset, value.SavedItemRarity);
			Serializer.SchemaSerialize(_boolFormatter, ref buffer, ref offset, value.bHasSaveData);
			Serializer.SchemaSerialize(_boolFormatter, ref buffer, ref offset, value.bWasCrafted);
			Serializer.SchemaSerialize(_hashSet_ValueTupleOfstringANDlongFormatter, ref buffer, ref offset, value.ClientsInstancedCopyTaken);
		}

		public void Deserialize(byte[] buffer, ref int offset, ref BaseWeapon value)
		{
			Serializer.DeserializeSchema<BaseWeapon>(ref SchemaDeserializer, buffer, ref offset, "SavedStackCount", "bRandomizeRarityOnStart", "SavedItemRarity", "bHasSaveData", "bWasCrafted", "ClientsInstancedCopyTaken");
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _intFormatter, buffer, ref offset, ref value.SavedStackCount);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _boolFormatter, buffer, ref offset, ref value.bRandomizeRarityOnStart);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _byteFormatter, buffer, ref offset, ref value.SavedItemRarity);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _boolFormatter, buffer, ref offset, ref value.bHasSaveData);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _boolFormatter, buffer, ref offset, ref value.bWasCrafted);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _hashSet_ValueTupleOfstringANDlongFormatter, buffer, ref offset, ref value.ClientsInstancedCopyTaken);
		}
	}

	internal class BaseWeaponMeleeFormatter : IFormatter<BaseWeaponMelee>
	{
		CerasSerializer Serializer;
		CerasSerializer.SchemaDeserializer SchemaDeserializer;
		IFormatter<float> _floatFormatter;
		IFormatter<bool> _boolFormatter;

		public void Serialize(ref byte[] buffer, ref int offset, BaseWeaponMelee value)
		{
			Serializer.SerializeSchema<BaseWeaponMelee>(ref buffer, ref offset, "SwingSpeed", "SavedBonusDamage", "bCalculatedBonusDamage");
			Serializer.SchemaSerialize(_floatFormatter, ref buffer, ref offset, value.SwingSpeed);
			Serializer.SchemaSerialize(_floatFormatter, ref buffer, ref offset, value.SavedBonusDamage);
			Serializer.SchemaSerialize(_boolFormatter, ref buffer, ref offset, value.bCalculatedBonusDamage);
		}

		public void Deserialize(byte[] buffer, ref int offset, ref BaseWeaponMelee value)
		{
			Serializer.DeserializeSchema<BaseWeaponMelee>(ref SchemaDeserializer, buffer, ref offset, "SwingSpeed", "SavedBonusDamage", "bCalculatedBonusDamage");
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _floatFormatter, buffer, ref offset, ref value.SwingSpeed);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _floatFormatter, buffer, ref offset, ref value.SavedBonusDamage);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _boolFormatter, buffer, ref offset, ref value.bCalculatedBonusDamage);
		}
	}

	internal class MultiPointGunComponentFormatter : IFormatter<MultiPointGunComponent>
	{
		CerasSerializer Serializer;
		CerasSerializer.SchemaDeserializer SchemaDeserializer;
		IFormatter<int> _intFormatter;

		public void Serialize(ref byte[] buffer, ref int offset, MultiPointGunComponent value)
		{
			Serializer.SerializeSchema<MultiPointGunComponent>(ref buffer, ref offset, "CurrentBulletCount_Saved");
			Serializer.SchemaSerialize(_intFormatter, ref buffer, ref offset, value.CurrentBulletCount_Saved);
		}

		public void Deserialize(byte[] buffer, ref int offset, ref MultiPointGunComponent value)
		{
			Serializer.DeserializeSchema<MultiPointGunComponent>(ref SchemaDeserializer, buffer, ref offset, "CurrentBulletCount_Saved");
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _intFormatter, buffer, ref offset, ref value.CurrentBulletCount_Saved);
		}
	}

	internal class ShepherdGunComponentFormatter : IFormatter<ShepherdGunComponent>
	{
		CerasSerializer Serializer;
		CerasSerializer.SchemaDeserializer SchemaDeserializer;
		IFormatter<int> _intFormatter;

		public void Serialize(ref byte[] buffer, ref int offset, ShepherdGunComponent value)
		{
			Serializer.SerializeSchema<ShepherdGunComponent>(ref buffer, ref offset, "CurrentBulletCount_Saved");
			Serializer.SchemaSerialize(_intFormatter, ref buffer, ref offset, value.CurrentBulletCount_Saved);
		}

		public void Deserialize(byte[] buffer, ref int offset, ref ShepherdGunComponent value)
		{
			Serializer.DeserializeSchema<ShepherdGunComponent>(ref SchemaDeserializer, buffer, ref offset, "CurrentBulletCount_Saved");
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _intFormatter, buffer, ref offset, ref value.CurrentBulletCount_Saved);
		}
	}

	internal class ZenaGunComponentFormatter : IFormatter<ZenaGunComponent>
	{
		CerasSerializer Serializer;
		CerasSerializer.SchemaDeserializer SchemaDeserializer;
		IFormatter<int> _intFormatter;

		public void Serialize(ref byte[] buffer, ref int offset, ZenaGunComponent value)
		{
			Serializer.SerializeSchema<ZenaGunComponent>(ref buffer, ref offset, "CurrentBulletCount_Saved");
			Serializer.SchemaSerialize(_intFormatter, ref buffer, ref offset, value.CurrentBulletCount_Saved);
		}

		public void Deserialize(byte[] buffer, ref int offset, ref ZenaGunComponent value)
		{
			Serializer.DeserializeSchema<ZenaGunComponent>(ref SchemaDeserializer, buffer, ref offset, "CurrentBulletCount_Saved");
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _intFormatter, buffer, ref offset, ref value.CurrentBulletCount_Saved);
		}
	}

	internal class BaseContainerFormatter : IFormatter<BaseContainer>
	{
		CerasSerializer Serializer;
		CerasSerializer.SchemaDeserializer SchemaDeserializer;
		IFormatter<bool> _boolFormatter;
		IFormatter<System.Collections.Generic.List<HRCategory>> _list_HRCategoryFormatter;
		IFormatter<System.Collections.Generic.List<int>> _list_intFormatter;
		IFormatter<int> _intFormatter;

		public void Serialize(ref byte[] buffer, ref int offset, BaseContainer value)
		{
			Serializer.SerializeSchema<BaseContainer>(ref buffer, ref offset, "bEmployeeUse", "bUseInput", "bUseOutput", "bUseItems", "AllowedContainerCategories", "AllowedItems", "AllowedFilterItemsIDs", "NumberOfValidItems");
			Serializer.SchemaSerialize(_boolFormatter, ref buffer, ref offset, value.bEmployeeUse);
			Serializer.SchemaSerialize(_boolFormatter, ref buffer, ref offset, value.bUseInput);
			Serializer.SchemaSerialize(_boolFormatter, ref buffer, ref offset, value.bUseOutput);
			Serializer.SchemaSerialize(_boolFormatter, ref buffer, ref offset, value.bUseItems);
			Serializer.SchemaSerialize(_list_HRCategoryFormatter, ref buffer, ref offset, value.AllowedContainerCategories);
			Serializer.SchemaSerialize(_list_intFormatter, ref buffer, ref offset, value.AllowedItems);
			Serializer.SchemaSerialize(_list_intFormatter, ref buffer, ref offset, value.AllowedFilterItemsIDs);
			Serializer.SchemaSerialize(_intFormatter, ref buffer, ref offset, value.NumberOfValidItems);
		}

		public void Deserialize(byte[] buffer, ref int offset, ref BaseContainer value)
		{
			Serializer.DeserializeSchema<BaseContainer>(ref SchemaDeserializer, buffer, ref offset, "bEmployeeUse", "bUseInput", "bUseOutput", "bUseItems", "AllowedContainerCategories", "AllowedItems", "AllowedFilterItemsIDs", "NumberOfValidItems");
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _boolFormatter, buffer, ref offset, ref value.bEmployeeUse);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _boolFormatter, buffer, ref offset, ref value.bUseInput);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _boolFormatter, buffer, ref offset, ref value.bUseOutput);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _boolFormatter, buffer, ref offset, ref value.bUseItems);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _list_HRCategoryFormatter, buffer, ref offset, ref value.AllowedContainerCategories);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _list_intFormatter, buffer, ref offset, ref value.AllowedItems);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _list_intFormatter, buffer, ref offset, ref value.AllowedFilterItemsIDs);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _intFormatter, buffer, ref offset, ref value.NumberOfValidItems);
		}
	}

	internal class BaseInventoryFormatter : IFormatter<BaseInventory>
	{
		CerasSerializer Serializer;
		CerasSerializer.SchemaDeserializer SchemaDeserializer;
		IFormatter<bool> _boolFormatter;
		IFormatter<System.String[]> _stringArrayFormatter;
		IFormatter<System.Int32[]> _int32ArrayFormatter;
		IFormatter<int> _intFormatter;

		public void Serialize(ref byte[] buffer, ref int offset, BaseInventory value)
		{
			Serializer.SerializeSchema<BaseInventory>(ref buffer, ref offset, "bContentsBeenRandomized", "SavedItemIDs", "SavedItemIDSlotTracker", "SavedMaxSlot");
			Serializer.SchemaSerialize(_boolFormatter, ref buffer, ref offset, value.bContentsBeenRandomized);
			Serializer.SchemaSerialize(_stringArrayFormatter, ref buffer, ref offset, value.SavedItemIDs);
			Serializer.SchemaSerialize(_int32ArrayFormatter, ref buffer, ref offset, value.SavedItemIDSlotTracker);
			Serializer.SchemaSerialize(_intFormatter, ref buffer, ref offset, value.SavedMaxSlot);
		}

		public void Deserialize(byte[] buffer, ref int offset, ref BaseInventory value)
		{
			Serializer.DeserializeSchema<BaseInventory>(ref SchemaDeserializer, buffer, ref offset, "bContentsBeenRandomized", "SavedItemIDs", "SavedItemIDSlotTracker", "SavedMaxSlot");
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _boolFormatter, buffer, ref offset, ref value.bContentsBeenRandomized);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _stringArrayFormatter, buffer, ref offset, ref value.SavedItemIDs);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _int32ArrayFormatter, buffer, ref offset, ref value.SavedItemIDSlotTracker);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _intFormatter, buffer, ref offset, ref value.SavedMaxSlot);
		}
	}

	internal class HRAchievementManagerFormatter : IFormatter<HRAchievementManager>
	{
		CerasSerializer Serializer;
		CerasSerializer.SchemaDeserializer SchemaDeserializer;
		IFormatter<System.Collections.Generic.HashSet<string>> _hashSet_stringFormatter;

		public void Serialize(ref byte[] buffer, ref int offset, HRAchievementManager value)
		{
			Serializer.SerializeSchema<HRAchievementManager>(ref buffer, ref offset, "unlockedAchievements");
			Serializer.SchemaSerialize(_hashSet_stringFormatter, ref buffer, ref offset, value.unlockedAchievements);
		}

		public void Deserialize(byte[] buffer, ref int offset, ref HRAchievementManager value)
		{
			Serializer.DeserializeSchema<HRAchievementManager>(ref SchemaDeserializer, buffer, ref offset, "unlockedAchievements");
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _hashSet_stringFormatter, buffer, ref offset, ref value.unlockedAchievements);
		}
	}

	internal class HRAttributeManagerFormatter : IFormatter<HRAttributeManager>
	{
		CerasSerializer Serializer;
		CerasSerializer.SchemaDeserializer SchemaDeserializer;
		IFormatter<System.Collections.Generic.List<HRAttributeData>> _list_HRAttributeDataFormatter;

		public void Serialize(ref byte[] buffer, ref int offset, HRAttributeManager value)
		{
			Serializer.SerializeSchema<HRAttributeManager>(ref buffer, ref offset, "SavedAttributes");
			Serializer.SchemaSerialize(_list_HRAttributeDataFormatter, ref buffer, ref offset, value.SavedAttributes);
		}

		public void Deserialize(byte[] buffer, ref int offset, ref HRAttributeManager value)
		{
			Serializer.DeserializeSchema<HRAttributeManager>(ref SchemaDeserializer, buffer, ref offset, "SavedAttributes");
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _list_HRAttributeDataFormatter, buffer, ref offset, ref value.SavedAttributes);
		}
	}

	internal class HRAttributeDataFormatter : IFormatter<HRAttributeData>
	{
		CerasSerializer Serializer;
		CerasSerializer.SchemaDeserializer SchemaDeserializer;
		IFormatter<bool> _boolFormatter;
		IFormatter<int> _intFormatter;
		IFormatter<System.String[]> _stringArrayFormatter;

		public void Serialize(ref byte[] buffer, ref int offset, HRAttributeData value)
		{
			Serializer.SerializeSchema<HRAttributeData>(ref buffer, ref offset, "bApplyToSelf", "bShouldSave", "AttributeID", "AttributeData");
			Serializer.SchemaSerialize(_boolFormatter, ref buffer, ref offset, value.bApplyToSelf);
			Serializer.SchemaSerialize(_boolFormatter, ref buffer, ref offset, value.bShouldSave);
			Serializer.SchemaSerialize(_intFormatter, ref buffer, ref offset, value.AttributeID);
			Serializer.SchemaSerialize(_stringArrayFormatter, ref buffer, ref offset, value.AttributeData);
		}

		public void Deserialize(byte[] buffer, ref int offset, ref HRAttributeData value)
		{
			Serializer.DeserializeSchema<HRAttributeData>(ref SchemaDeserializer, buffer, ref offset, "bApplyToSelf", "bShouldSave", "AttributeID", "AttributeData");
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _boolFormatter, buffer, ref offset, ref value.bApplyToSelf);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _boolFormatter, buffer, ref offset, ref value.bShouldSave);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _intFormatter, buffer, ref offset, ref value.AttributeID);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _stringArrayFormatter, buffer, ref offset, ref value.AttributeData);
		}
	}

	internal class HRAttributeManagerSaveFormatter : IFormatter<HRAttributeManagerSave>
	{
		CerasSerializer Serializer;
		CerasSerializer.SchemaDeserializer SchemaDeserializer;
		IFormatter<System.Collections.Generic.List<HRAttributeData>> _list_HRAttributeDataFormatter;

		public void Serialize(ref byte[] buffer, ref int offset, HRAttributeManagerSave value)
		{
			Serializer.SerializeSchema<HRAttributeManagerSave>(ref buffer, ref offset, "SavedAttributes");
			Serializer.SchemaSerialize(_list_HRAttributeDataFormatter, ref buffer, ref offset, value.SavedAttributes);
		}

		public void Deserialize(byte[] buffer, ref int offset, ref HRAttributeManagerSave value)
		{
			Serializer.DeserializeSchema<HRAttributeManagerSave>(ref SchemaDeserializer, buffer, ref offset, "SavedAttributes");
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _list_HRAttributeDataFormatter, buffer, ref offset, ref value.SavedAttributes);
		}
	}

	internal class HRAttributeRandomizerFormatter : IFormatter<HRAttributeRandomizer>
	{
		CerasSerializer Serializer;
		CerasSerializer.SchemaDeserializer SchemaDeserializer;
		IFormatter<bool> _boolFormatter;

		public void Serialize(ref byte[] buffer, ref int offset, HRAttributeRandomizer value)
		{
			Serializer.SerializeSchema<HRAttributeRandomizer>(ref buffer, ref offset, "bAlreadyRandomized");
			Serializer.SchemaSerialize(_boolFormatter, ref buffer, ref offset, value.bAlreadyRandomized);
		}

		public void Deserialize(byte[] buffer, ref int offset, ref HRAttributeRandomizer value)
		{
			Serializer.DeserializeSchema<HRAttributeRandomizer>(ref SchemaDeserializer, buffer, ref offset, "bAlreadyRandomized");
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _boolFormatter, buffer, ref offset, ref value.bAlreadyRandomized);
		}
	}

	internal class HRBankSystemFormatter : IFormatter<HRBankSystem>
	{
		CerasSerializer Serializer;
		CerasSerializer.SchemaDeserializer SchemaDeserializer;
		IFormatter<float> _floatFormatter;
		IFormatter<bool> _boolFormatter;
		IFormatter<BankAccountData[]> _bankAccountDataArrayFormatter;

		public void Serialize(ref byte[] buffer, ref int offset, HRBankSystem value)
		{
			Serializer.SerializeSchema<HRBankSystem>(ref buffer, ref offset, "dailyInterestRate", "compoundContinuously", "bankAccountSaveData");
			Serializer.SchemaSerialize(_floatFormatter, ref buffer, ref offset, value.dailyInterestRate);
			Serializer.SchemaSerialize(_boolFormatter, ref buffer, ref offset, value.compoundContinuously);
			Serializer.SchemaSerialize(_bankAccountDataArrayFormatter, ref buffer, ref offset, value.bankAccountSaveData);
		}

		public void Deserialize(byte[] buffer, ref int offset, ref HRBankSystem value)
		{
			Serializer.DeserializeSchema<HRBankSystem>(ref SchemaDeserializer, buffer, ref offset, "dailyInterestRate", "compoundContinuously", "bankAccountSaveData");
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _floatFormatter, buffer, ref offset, ref value.dailyInterestRate);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _boolFormatter, buffer, ref offset, ref value.compoundContinuously);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _bankAccountDataArrayFormatter, buffer, ref offset, ref value.bankAccountSaveData);
		}
	}

	internal class BankAccountDataFormatter : IFormatter<BankAccountData>
	{
		CerasSerializer Serializer;
		CerasSerializer.SchemaDeserializer SchemaDeserializer;
		IFormatter<BankAccountOwnerData> _bankAccountOwnerDataFormatter;
		IFormatter<HRDateAndTimeStruct> _hRDateAndTimeStructFormatter;
		IFormatter<float> _floatFormatter;

		public void Serialize(ref byte[] buffer, ref int offset, BankAccountData value)
		{
			Serializer.SerializeSchema<BankAccountData>(ref buffer, ref offset, "ownerData", "prevDateTime", "balance", "interestSinceLastTransaction");
			Serializer.SchemaSerialize(_bankAccountOwnerDataFormatter, ref buffer, ref offset, value.ownerData);
			Serializer.SchemaSerialize(_hRDateAndTimeStructFormatter, ref buffer, ref offset, value.prevDateTime);
			Serializer.SchemaSerialize(_floatFormatter, ref buffer, ref offset, value.balance);
			Serializer.SchemaSerialize(_floatFormatter, ref buffer, ref offset, value.interestSinceLastTransaction);
		}

		public void Deserialize(byte[] buffer, ref int offset, ref BankAccountData value)
		{
			Serializer.DeserializeSchema<BankAccountData>(ref SchemaDeserializer, buffer, ref offset, "ownerData", "prevDateTime", "balance", "interestSinceLastTransaction");
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _bankAccountOwnerDataFormatter, buffer, ref offset, ref value.ownerData);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _hRDateAndTimeStructFormatter, buffer, ref offset, ref value.prevDateTime);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _floatFormatter, buffer, ref offset, ref value.balance);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _floatFormatter, buffer, ref offset, ref value.interestSinceLastTransaction);
		}
	}

	internal class BankAccountOwnerDataFormatter : IFormatter<BankAccountOwnerData>
	{
		CerasSerializer Serializer;
		CerasSerializer.SchemaDeserializer SchemaDeserializer;
		IFormatter<long> _longFormatter;
		IFormatter<string> _stringFormatter;

		public void Serialize(ref byte[] buffer, ref int offset, BankAccountOwnerData value)
		{
			Serializer.SerializeSchema<BankAccountOwnerData>(ref buffer, ref offset, "characterId", "steamId");
			Serializer.SchemaSerialize(_longFormatter, ref buffer, ref offset, value.characterId);
			Serializer.SchemaSerialize(_stringFormatter, ref buffer, ref offset, value.steamId);
		}

		public void Deserialize(byte[] buffer, ref int offset, ref BankAccountOwnerData value)
		{
			Serializer.DeserializeSchema<BankAccountOwnerData>(ref SchemaDeserializer, buffer, ref offset, "characterId", "steamId");
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _longFormatter, buffer, ref offset, ref value.characterId);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _stringFormatter, buffer, ref offset, ref value.steamId);
		}
	}

	internal class HRConstructionComponentFormatter : IFormatter<HRConstructionComponent>
	{
		CerasSerializer Serializer;
		CerasSerializer.SchemaDeserializer SchemaDeserializer;
		IFormatter<HRDateStruct> _hRDateStructFormatter;
		IFormatter<int> _intFormatter;

		public void Serialize(ref byte[] buffer, ref int offset, HRConstructionComponent value)
		{
			Serializer.SerializeSchema<HRConstructionComponent>(ref buffer, ref offset, "DateStarted", "CurrentStatus");
			Serializer.SchemaSerialize(_hRDateStructFormatter, ref buffer, ref offset, value.DateStarted);
			Serializer.SchemaSerialize(_intFormatter, ref buffer, ref offset, value.CurrentStatus);
		}

		public void Deserialize(byte[] buffer, ref int offset, ref HRConstructionComponent value)
		{
			Serializer.DeserializeSchema<HRConstructionComponent>(ref SchemaDeserializer, buffer, ref offset, "DateStarted", "CurrentStatus");
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _hRDateStructFormatter, buffer, ref offset, ref value.DateStarted);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _intFormatter, buffer, ref offset, ref value.CurrentStatus);
		}
	}

	internal class HRDateStructFormatter : IFormatter<HRDateStruct>
	{
		CerasSerializer Serializer;
		CerasSerializer.SchemaDeserializer SchemaDeserializer;
		IFormatter<int> _intFormatter;

		public void Serialize(ref byte[] buffer, ref int offset, HRDateStruct value)
		{
			Serializer.SerializeSchema<HRDateStruct>(ref buffer, ref offset, "CurrentDay", "CurrentMonth", "CurrentYear");
			Serializer.SchemaSerialize(_intFormatter, ref buffer, ref offset, value.CurrentDay);
			Serializer.SchemaSerialize(_intFormatter, ref buffer, ref offset, value.CurrentMonth);
			Serializer.SchemaSerialize(_intFormatter, ref buffer, ref offset, value.CurrentYear);
		}

		public void Deserialize(byte[] buffer, ref int offset, ref HRDateStruct value)
		{
			Serializer.DeserializeSchema<HRDateStruct>(ref SchemaDeserializer, buffer, ref offset, "CurrentDay", "CurrentMonth", "CurrentYear");
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _intFormatter, buffer, ref offset, ref value.CurrentDay);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _intFormatter, buffer, ref offset, ref value.CurrentMonth);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _intFormatter, buffer, ref offset, ref value.CurrentYear);
		}
	}

	internal class HRConstructionManagerFormatter : IFormatter<HRConstructionManager>
	{
		CerasSerializer Serializer;
		CerasSerializer.SchemaDeserializer SchemaDeserializer;

		public void Serialize(ref byte[] buffer, ref int offset, HRConstructionManager value)
		{
		}

		public void Deserialize(byte[] buffer, ref int offset, ref HRConstructionManager value)
		{
		}
	}

	internal class HRCraftingComponentFormatter : IFormatter<HRCraftingComponent>
	{
		CerasSerializer Serializer;
		CerasSerializer.SchemaDeserializer SchemaDeserializer;
		IFormatter<float> _floatFormatter;
		IFormatter<bool> _boolFormatter;
		IFormatter<HRCraftingComponent.CraftedItemData> _craftedItemDataFormatter;
		IFormatter<HRDateStruct> _hRDateStructFormatter;
		IFormatter<int> _intFormatter;
		IFormatter<System.Collections.Generic.List<HRCraftingComponent.SavedCraftedItemData>> _list_SavedCraftedItemDataFormatter;

		public void Serialize(ref byte[] buffer, ref int offset, HRCraftingComponent value)
		{
			Serializer.SerializeSchema<HRCraftingComponent>(ref buffer, ref offset, "CraftTimer", "bIsCrafting", "bIsAutoCraftEnabled", "CurrentCraftedItemData", "LastCraftedItemData", "SavedDate", "SavedHour", "SavedMinute", "SavedRecipeQueue");
			Serializer.SchemaSerialize(_floatFormatter, ref buffer, ref offset, value.CraftTimer);
			Serializer.SchemaSerialize(_boolFormatter, ref buffer, ref offset, value.bIsCrafting);
			Serializer.SchemaSerialize(_boolFormatter, ref buffer, ref offset, value.bIsAutoCraftEnabled);
			Serializer.SchemaSerialize(_craftedItemDataFormatter, ref buffer, ref offset, value.CurrentCraftedItemData);
			Serializer.SchemaSerialize(_craftedItemDataFormatter, ref buffer, ref offset, value.LastCraftedItemData);
			Serializer.SchemaSerialize(_hRDateStructFormatter, ref buffer, ref offset, value.SavedDate);
			Serializer.SchemaSerialize(_intFormatter, ref buffer, ref offset, value.SavedHour);
			Serializer.SchemaSerialize(_intFormatter, ref buffer, ref offset, value.SavedMinute);
			Serializer.SchemaSerialize(_list_SavedCraftedItemDataFormatter, ref buffer, ref offset, value.SavedRecipeQueue);
		}

		public void Deserialize(byte[] buffer, ref int offset, ref HRCraftingComponent value)
		{
			Serializer.DeserializeSchema<HRCraftingComponent>(ref SchemaDeserializer, buffer, ref offset, "CraftTimer", "bIsCrafting", "bIsAutoCraftEnabled", "CurrentCraftedItemData", "LastCraftedItemData", "SavedDate", "SavedHour", "SavedMinute", "SavedRecipeQueue");
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _floatFormatter, buffer, ref offset, ref value.CraftTimer);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _boolFormatter, buffer, ref offset, ref value.bIsCrafting);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _boolFormatter, buffer, ref offset, ref value.bIsAutoCraftEnabled);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _craftedItemDataFormatter, buffer, ref offset, ref value.CurrentCraftedItemData);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _craftedItemDataFormatter, buffer, ref offset, ref value.LastCraftedItemData);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _hRDateStructFormatter, buffer, ref offset, ref value.SavedDate);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _intFormatter, buffer, ref offset, ref value.SavedHour);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _intFormatter, buffer, ref offset, ref value.SavedMinute);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _list_SavedCraftedItemDataFormatter, buffer, ref offset, ref value.SavedRecipeQueue);
		}
	}

	internal class CraftedItemDataFormatter : IFormatter<HRCraftingComponent.CraftedItemData>
	{
		CerasSerializer Serializer;
		CerasSerializer.SchemaDeserializer SchemaDeserializer;
		IFormatter<HRCraftingInfo.HRCraftingIngredient[]> _hRCraftingIngredientArrayFormatter;
		IFormatter<HRNetworkManager.HRLocalPlayerData> _hRLocalPlayerDataFormatter;
		IFormatter<int> _intFormatter;
		IFormatter<float> _floatFormatter;

		public void Serialize(ref byte[] buffer, ref int offset, HRCraftingComponent.CraftedItemData value)
		{
			Serializer.SerializeSchema<HRCraftingComponent.CraftedItemData>(ref buffer, ref offset, "Ingredients", "LocalPlayerData", "ItemID", "TimeToCraft");
			Serializer.SchemaSerialize(_hRCraftingIngredientArrayFormatter, ref buffer, ref offset, value.Ingredients);
			Serializer.SchemaSerialize(_hRLocalPlayerDataFormatter, ref buffer, ref offset, value.LocalPlayerData);
			Serializer.SchemaSerialize(_intFormatter, ref buffer, ref offset, value.ItemID);
			Serializer.SchemaSerialize(_floatFormatter, ref buffer, ref offset, value.TimeToCraft);
		}

		public void Deserialize(byte[] buffer, ref int offset, ref HRCraftingComponent.CraftedItemData value)
		{
			Serializer.DeserializeSchema<HRCraftingComponent.CraftedItemData>(ref SchemaDeserializer, buffer, ref offset, "Ingredients", "LocalPlayerData", "ItemID", "TimeToCraft");
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _hRCraftingIngredientArrayFormatter, buffer, ref offset, ref value.Ingredients);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _hRLocalPlayerDataFormatter, buffer, ref offset, ref value.LocalPlayerData);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _intFormatter, buffer, ref offset, ref value.ItemID);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _floatFormatter, buffer, ref offset, ref value.TimeToCraft);
		}
	}

	internal class HRLocalPlayerDataFormatter : IFormatter<HRNetworkManager.HRLocalPlayerData>
	{
		CerasSerializer Serializer;
		CerasSerializer.SchemaDeserializer SchemaDeserializer;
		IFormatter<long> _longFormatter;
		IFormatter<string> _stringFormatter;

		public void Serialize(ref byte[] buffer, ref int offset, HRNetworkManager.HRLocalPlayerData value)
		{
			Serializer.SerializeSchema<HRNetworkManager.HRLocalPlayerData>(ref buffer, ref offset, "LocalCharacterID", "LocalSteamID");
			Serializer.SchemaSerialize(_longFormatter, ref buffer, ref offset, value.LocalCharacterID);
			Serializer.SchemaSerialize(_stringFormatter, ref buffer, ref offset, value.LocalSteamID);
		}

		public void Deserialize(byte[] buffer, ref int offset, ref HRNetworkManager.HRLocalPlayerData value)
		{
			Serializer.DeserializeSchema<HRNetworkManager.HRLocalPlayerData>(ref SchemaDeserializer, buffer, ref offset, "LocalCharacterID", "LocalSteamID");
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _longFormatter, buffer, ref offset, ref value.LocalCharacterID);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _stringFormatter, buffer, ref offset, ref value.LocalSteamID);
		}
	}

	internal class HRCraftingIngredientFormatter : IFormatter<HRCraftingInfo.HRCraftingIngredient>
	{
		CerasSerializer Serializer;
		CerasSerializer.SchemaDeserializer SchemaDeserializer;
		IFormatter<BaseWeapon> _baseWeaponFormatter;
		IFormatter<int> _intFormatter;

		public void Serialize(ref byte[] buffer, ref int offset, HRCraftingInfo.HRCraftingIngredient value)
		{
			Serializer.SerializeSchema<HRCraftingInfo.HRCraftingIngredient>(ref buffer, ref offset, "IngredientWeapon", "AmountRequired", "ItemID");
			Serializer.SchemaSerialize(_baseWeaponFormatter, ref buffer, ref offset, value.IngredientWeapon);
			Serializer.SchemaSerialize(_intFormatter, ref buffer, ref offset, value.AmountRequired);
			Serializer.SchemaSerialize(_intFormatter, ref buffer, ref offset, value.ItemID);
		}

		public void Deserialize(byte[] buffer, ref int offset, ref HRCraftingInfo.HRCraftingIngredient value)
		{
			Serializer.DeserializeSchema<HRCraftingInfo.HRCraftingIngredient>(ref SchemaDeserializer, buffer, ref offset, "IngredientWeapon", "AmountRequired", "ItemID");
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _baseWeaponFormatter, buffer, ref offset, ref value.IngredientWeapon);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _intFormatter, buffer, ref offset, ref value.AmountRequired);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _intFormatter, buffer, ref offset, ref value.ItemID);
		}
	}

	internal class SavedCraftedItemDataFormatter : IFormatter<HRCraftingComponent.SavedCraftedItemData>
	{
		CerasSerializer Serializer;
		CerasSerializer.SchemaDeserializer SchemaDeserializer;
		IFormatter<HRCraftingInfo.HRCraftingIngredient[]> _hRCraftingIngredientArrayFormatter;
		IFormatter<HRNetworkManager.HRLocalPlayerData> _hRLocalPlayerDataFormatter;
		IFormatter<int> _intFormatter;
		IFormatter<float> _floatFormatter;
		IFormatter<string> _stringFormatter;

		public void Serialize(ref byte[] buffer, ref int offset, HRCraftingComponent.SavedCraftedItemData value)
		{
			Serializer.SerializeSchema<HRCraftingComponent.SavedCraftedItemData>(ref buffer, ref offset, "Ingredients", "LocalPlayerData", "ItemID", "TimeToCraft", "OwningPlayerID");
			Serializer.SchemaSerialize(_hRCraftingIngredientArrayFormatter, ref buffer, ref offset, value.Ingredients);
			Serializer.SchemaSerialize(_hRLocalPlayerDataFormatter, ref buffer, ref offset, value.LocalPlayerData);
			Serializer.SchemaSerialize(_intFormatter, ref buffer, ref offset, value.ItemID);
			Serializer.SchemaSerialize(_floatFormatter, ref buffer, ref offset, value.TimeToCraft);
			Serializer.SchemaSerialize(_stringFormatter, ref buffer, ref offset, value.OwningPlayerID);
		}

		public void Deserialize(byte[] buffer, ref int offset, ref HRCraftingComponent.SavedCraftedItemData value)
		{
			Serializer.DeserializeSchema<HRCraftingComponent.SavedCraftedItemData>(ref SchemaDeserializer, buffer, ref offset, "Ingredients", "LocalPlayerData", "ItemID", "TimeToCraft", "OwningPlayerID");
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _hRCraftingIngredientArrayFormatter, buffer, ref offset, ref value.Ingredients);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _hRLocalPlayerDataFormatter, buffer, ref offset, ref value.LocalPlayerData);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _intFormatter, buffer, ref offset, ref value.ItemID);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _floatFormatter, buffer, ref offset, ref value.TimeToCraft);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _stringFormatter, buffer, ref offset, ref value.OwningPlayerID);
		}
	}

	internal class HRPaintingSystemFormatter : IFormatter<HRPaintingSystem>
	{
		CerasSerializer Serializer;
		CerasSerializer.SchemaDeserializer SchemaDeserializer;
		IFormatter<System.Collections.Generic.Dictionary<string, System.String[]>> _dictionary_string_StringArrayFormatter;
		IFormatter<string> _stringFormatter;

		public void Serialize(ref byte[] buffer, ref int offset, HRPaintingSystem value)
		{
			Serializer.SerializeSchema<HRPaintingSystem>(ref buffer, ref offset, "SaveData", "TextureSavePath");
			Serializer.SchemaSerialize(_dictionary_string_StringArrayFormatter, ref buffer, ref offset, value.SaveData);
			Serializer.SchemaSerialize(_stringFormatter, ref buffer, ref offset, value.TextureSavePath);
		}

		public void Deserialize(byte[] buffer, ref int offset, ref HRPaintingSystem value)
		{
			Serializer.DeserializeSchema<HRPaintingSystem>(ref SchemaDeserializer, buffer, ref offset, "SaveData", "TextureSavePath");
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _dictionary_string_StringArrayFormatter, buffer, ref offset, ref value.SaveData);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _stringFormatter, buffer, ref offset, ref value.TextureSavePath);
		}
	}

	internal class HRFearComponentFormatter : IFormatter<HRFearComponent>
	{
		CerasSerializer Serializer;
		CerasSerializer.SchemaDeserializer SchemaDeserializer;
		IFormatter<float> _floatFormatter;

		public void Serialize(ref byte[] buffer, ref int offset, HRFearComponent value)
		{
			Serializer.SerializeSchema<HRFearComponent>(ref buffer, ref offset, "CurrentHP");
			Serializer.SchemaSerialize(_floatFormatter, ref buffer, ref offset, value.CurrentHP);
		}

		public void Deserialize(byte[] buffer, ref int offset, ref HRFearComponent value)
		{
			Serializer.DeserializeSchema<HRFearComponent>(ref SchemaDeserializer, buffer, ref offset, "CurrentHP");
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _floatFormatter, buffer, ref offset, ref value.CurrentHP);
		}
	}

	internal class HRFloorTileFormatter : IFormatter<HRFloorTile>
	{
		CerasSerializer Serializer;
		CerasSerializer.SchemaDeserializer SchemaDeserializer;
		IFormatter<int> _intFormatter;

		public void Serialize(ref byte[] buffer, ref int offset, HRFloorTile value)
		{
			Serializer.SerializeSchema<HRFloorTile>(ref buffer, ref offset, "SavedFloorType", "SavedAltIndex");
			Serializer.SchemaSerialize(_intFormatter, ref buffer, ref offset, value.SavedFloorType);
			Serializer.SchemaSerialize(_intFormatter, ref buffer, ref offset, value.SavedAltIndex);
		}

		public void Deserialize(byte[] buffer, ref int offset, ref HRFloorTile value)
		{
			Serializer.DeserializeSchema<HRFloorTile>(ref SchemaDeserializer, buffer, ref offset, "SavedFloorType", "SavedAltIndex");
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _intFormatter, buffer, ref offset, ref value.SavedFloorType);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _intFormatter, buffer, ref offset, ref value.SavedAltIndex);
		}
	}

	internal class HRItemValueFormatter : IFormatter<HRItemValue>
	{
		CerasSerializer Serializer;
		CerasSerializer.SchemaDeserializer SchemaDeserializer;
		IFormatter<float> _floatFormatter;

		public void Serialize(ref byte[] buffer, ref int offset, HRItemValue value)
		{
			Serializer.SerializeSchema<HRItemValue>(ref buffer, ref offset, "SavedUniqueItemValue");
			Serializer.SchemaSerialize(_floatFormatter, ref buffer, ref offset, value.SavedUniqueItemValue);
		}

		public void Deserialize(byte[] buffer, ref int offset, ref HRItemValue value)
		{
			Serializer.DeserializeSchema<HRItemValue>(ref SchemaDeserializer, buffer, ref offset, "SavedUniqueItemValue");
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _floatFormatter, buffer, ref offset, ref value.SavedUniqueItemValue);
		}
	}

	internal class HRNPCShopSystemFormatter : IFormatter<HRNPCShopSystem>
	{
		CerasSerializer Serializer;
		CerasSerializer.SchemaDeserializer SchemaDeserializer;
		IFormatter<HRDateAndTimeStruct> _hRDateAndTimeStructFormatter;
		IFormatter<System.Boolean[]> _booleanArrayFormatter;
		IFormatter<bool> _boolFormatter;

		public void Serialize(ref byte[] buffer, ref int offset, HRNPCShopSystem value)
		{
			Serializer.SerializeSchema<HRNPCShopSystem>(ref buffer, ref offset, "SavedRestockDate", "SavedOwnerRespawnDate", "savedRestockFlags", "bInitialized");
			Serializer.SchemaSerialize(_hRDateAndTimeStructFormatter, ref buffer, ref offset, value.SavedRestockDate);
			Serializer.SchemaSerialize(_hRDateAndTimeStructFormatter, ref buffer, ref offset, value.SavedOwnerRespawnDate);
			Serializer.SchemaSerialize(_booleanArrayFormatter, ref buffer, ref offset, value.savedRestockFlags);
			Serializer.SchemaSerialize(_boolFormatter, ref buffer, ref offset, value.bInitialized);
		}

		public void Deserialize(byte[] buffer, ref int offset, ref HRNPCShopSystem value)
		{
			Serializer.DeserializeSchema<HRNPCShopSystem>(ref SchemaDeserializer, buffer, ref offset, "SavedRestockDate", "SavedOwnerRespawnDate", "savedRestockFlags", "bInitialized");
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _hRDateAndTimeStructFormatter, buffer, ref offset, ref value.SavedRestockDate);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _hRDateAndTimeStructFormatter, buffer, ref offset, ref value.SavedOwnerRespawnDate);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _booleanArrayFormatter, buffer, ref offset, ref value.savedRestockFlags);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _boolFormatter, buffer, ref offset, ref value.bInitialized);
		}
	}

	internal class HRDateAndTimeStructFormatter : IFormatter<HRDateAndTimeStruct>
	{
		CerasSerializer Serializer;
		CerasSerializer.SchemaDeserializer SchemaDeserializer;
		IFormatter<HRDateStruct> _hRDateStructFormatter;
		IFormatter<int> _intFormatter;

		public void Serialize(ref byte[] buffer, ref int offset, HRDateAndTimeStruct value)
		{
			Serializer.SerializeSchema<HRDateAndTimeStruct>(ref buffer, ref offset, "Date", "Hour", "Minute");
			Serializer.SchemaSerialize(_hRDateStructFormatter, ref buffer, ref offset, value.Date);
			Serializer.SchemaSerialize(_intFormatter, ref buffer, ref offset, value.Hour);
			Serializer.SchemaSerialize(_intFormatter, ref buffer, ref offset, value.Minute);
		}

		public void Deserialize(byte[] buffer, ref int offset, ref HRDateAndTimeStruct value)
		{
			Serializer.DeserializeSchema<HRDateAndTimeStruct>(ref SchemaDeserializer, buffer, ref offset, "Date", "Hour", "Minute");
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _hRDateStructFormatter, buffer, ref offset, ref value.Date);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _intFormatter, buffer, ref offset, ref value.Hour);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _intFormatter, buffer, ref offset, ref value.Minute);
		}
	}

	internal class HRQuestCustomersFormatter : IFormatter<HRQuestCustomers>
	{
		CerasSerializer Serializer;
		CerasSerializer.SchemaDeserializer SchemaDeserializer;
		IFormatter<HRQuestCustomer_Saved[]> _hRQuestCustomer_SavedArrayFormatter;

		public void Serialize(ref byte[] buffer, ref int offset, HRQuestCustomers value)
		{
			Serializer.SerializeSchema<HRQuestCustomers>(ref buffer, ref offset, "questCustomerSavedList");
			Serializer.SchemaSerialize(_hRQuestCustomer_SavedArrayFormatter, ref buffer, ref offset, value.questCustomerSavedList);
		}

		public void Deserialize(byte[] buffer, ref int offset, ref HRQuestCustomers value)
		{
			Serializer.DeserializeSchema<HRQuestCustomers>(ref SchemaDeserializer, buffer, ref offset, "questCustomerSavedList");
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _hRQuestCustomer_SavedArrayFormatter, buffer, ref offset, ref value.questCustomerSavedList);
		}
	}

	internal class HRQuestCustomer_SavedFormatter : IFormatter<HRQuestCustomer_Saved>
	{
		CerasSerializer Serializer;
		CerasSerializer.SchemaDeserializer SchemaDeserializer;
		IFormatter<bool> _boolFormatter;

		public void Serialize(ref byte[] buffer, ref int offset, HRQuestCustomer_Saved value)
		{
			Serializer.SerializeSchema<HRQuestCustomer_Saved>(ref buffer, ref offset, "bUnlocked");
			Serializer.SchemaSerialize(_boolFormatter, ref buffer, ref offset, value.bUnlocked);
		}

		public void Deserialize(byte[] buffer, ref int offset, ref HRQuestCustomer_Saved value)
		{
			Serializer.DeserializeSchema<HRQuestCustomer_Saved>(ref SchemaDeserializer, buffer, ref offset, "bUnlocked");
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _boolFormatter, buffer, ref offset, ref value.bUnlocked);
		}
	}

	internal class HRReputationFormatter : IFormatter<HRReputation>
	{
		CerasSerializer Serializer;
		CerasSerializer.SchemaDeserializer SchemaDeserializer;
		IFormatter<int> _intFormatter;

		public void Serialize(ref byte[] buffer, ref int offset, HRReputation value)
		{
			Serializer.SerializeSchema<HRReputation>(ref buffer, ref offset, "CurrentReputation");
			Serializer.SchemaSerialize(_intFormatter, ref buffer, ref offset, value.CurrentReputation);
		}

		public void Deserialize(byte[] buffer, ref int offset, ref HRReputation value)
		{
			Serializer.DeserializeSchema<HRReputation>(ref SchemaDeserializer, buffer, ref offset, "CurrentReputation");
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _intFormatter, buffer, ref offset, ref value.CurrentReputation);
		}
	}

	internal class HRShopManagerFormatter : IFormatter<HRShopManager>
	{
		CerasSerializer Serializer;
		CerasSerializer.SchemaDeserializer SchemaDeserializer;
		IFormatter<string> _stringFormatter;
		IFormatter<float> _floatFormatter;
		IFormatter<bool> _boolFormatter;
		IFormatter<System.Collections.Generic.List<HRRatingData>> _list_HRRatingDataFormatter;

		public void Serialize(ref byte[] buffer, ref int offset, HRShopManager value)
		{
			Serializer.SerializeSchema<HRShopManager>(ref buffer, ref offset, "ShopName", "TotalMoneyMade", "bSavedShowUnassignedPoints", "LastRatingsSave", "SavedCurrentRating");
			Serializer.SchemaSerialize(_stringFormatter, ref buffer, ref offset, value.ShopName);
			Serializer.SchemaSerialize(_floatFormatter, ref buffer, ref offset, value.TotalMoneyMade);
			Serializer.SchemaSerialize(_boolFormatter, ref buffer, ref offset, value.bSavedShowUnassignedPoints);
			Serializer.SchemaSerialize(_list_HRRatingDataFormatter, ref buffer, ref offset, value.LastRatingsSave);
			Serializer.SchemaSerialize(_floatFormatter, ref buffer, ref offset, value.SavedCurrentRating);
		}

		public void Deserialize(byte[] buffer, ref int offset, ref HRShopManager value)
		{
			Serializer.DeserializeSchema<HRShopManager>(ref SchemaDeserializer, buffer, ref offset, "ShopName", "TotalMoneyMade", "bSavedShowUnassignedPoints", "LastRatingsSave", "SavedCurrentRating");
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _stringFormatter, buffer, ref offset, ref value.ShopName);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _floatFormatter, buffer, ref offset, ref value.TotalMoneyMade);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _boolFormatter, buffer, ref offset, ref value.bSavedShowUnassignedPoints);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _list_HRRatingDataFormatter, buffer, ref offset, ref value.LastRatingsSave);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _floatFormatter, buffer, ref offset, ref value.SavedCurrentRating);
		}
	}

	internal class HRRatingDataFormatter : IFormatter<HRRatingData>
	{
		CerasSerializer Serializer;
		CerasSerializer.SchemaDeserializer SchemaDeserializer;
		IFormatter<float> _floatFormatter;
		IFormatter<string> _stringFormatter;
		IFormatter<System.String[]> _stringArrayFormatter;

		public void Serialize(ref byte[] buffer, ref int offset, HRRatingData value)
		{
			Serializer.SerializeSchema<HRRatingData>(ref buffer, ref offset, "Rating", "Customer", "Messages");
			Serializer.SchemaSerialize(_floatFormatter, ref buffer, ref offset, value.Rating);
			Serializer.SchemaSerialize(_stringFormatter, ref buffer, ref offset, value.Customer);
			Serializer.SchemaSerialize(_stringArrayFormatter, ref buffer, ref offset, value.Messages);
		}

		public void Deserialize(byte[] buffer, ref int offset, ref HRRatingData value)
		{
			Serializer.DeserializeSchema<HRRatingData>(ref SchemaDeserializer, buffer, ref offset, "Rating", "Customer", "Messages");
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _floatFormatter, buffer, ref offset, ref value.Rating);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _stringFormatter, buffer, ref offset, ref value.Customer);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _stringArrayFormatter, buffer, ref offset, ref value.Messages);
		}
	}

	internal class HRShopPlotFormatter : IFormatter<HRShopPlot>
	{
		CerasSerializer Serializer;
		CerasSerializer.SchemaDeserializer SchemaDeserializer;
		IFormatter<int> _intFormatter;
		IFormatter<System.Byte[]> _byteArrayFormatter;
		IFormatter<bool> _boolFormatter;

		public void Serialize(ref byte[] buffer, ref int offset, HRShopPlot value)
		{
			Serializer.SerializeSchema<HRShopPlot>(ref buffer, ref offset, "NumShopsToAdd", "savedNavMesh", "Unlocked");
			Serializer.SchemaSerialize(_intFormatter, ref buffer, ref offset, value.NumShopsToAdd);
			Serializer.SchemaSerialize(_byteArrayFormatter, ref buffer, ref offset, value.savedNavMesh);
			Serializer.SchemaSerialize(_boolFormatter, ref buffer, ref offset, value.Unlocked);
		}

		public void Deserialize(byte[] buffer, ref int offset, ref HRShopPlot value)
		{
			Serializer.DeserializeSchema<HRShopPlot>(ref SchemaDeserializer, buffer, ref offset, "NumShopsToAdd", "savedNavMesh", "Unlocked");
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _intFormatter, buffer, ref offset, ref value.NumShopsToAdd);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _byteArrayFormatter, buffer, ref offset, ref value.savedNavMesh);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _boolFormatter, buffer, ref offset, ref value.Unlocked);
		}
	}

	internal class HREmployeeSystemFormatter : IFormatter<HREmployeeSystem>
	{
		CerasSerializer Serializer;
		CerasSerializer.SchemaDeserializer SchemaDeserializer;
		IFormatter<System.Collections.Generic.List<HREmployeeSystem.EmployeeSaveData>> _list_EmployeeSaveDataFormatter;
		IFormatter<int> _intFormatter;
		IFormatter<HRDateAndTimeStruct> _hRDateAndTimeStructFormatter;

		public void Serialize(ref byte[] buffer, ref int offset, HREmployeeSystem value)
		{
			Serializer.SerializeSchema<HREmployeeSystem>(ref buffer, ref offset, "savedEmployees", "FirstHour", "SecondHour", "TotalWage", "Money", "EmployeeLimit", "RerollCost", "SavedDate");
			Serializer.SchemaSerialize(_list_EmployeeSaveDataFormatter, ref buffer, ref offset, value.savedEmployees);
			Serializer.SchemaSerialize(_intFormatter, ref buffer, ref offset, value.FirstHour);
			Serializer.SchemaSerialize(_intFormatter, ref buffer, ref offset, value.SecondHour);
			Serializer.SchemaSerialize(_intFormatter, ref buffer, ref offset, value.TotalWage);
			Serializer.SchemaSerialize(_intFormatter, ref buffer, ref offset, value.Money);
			Serializer.SchemaSerialize(_intFormatter, ref buffer, ref offset, value.EmployeeLimit);
			Serializer.SchemaSerialize(_intFormatter, ref buffer, ref offset, value.RerollCost);
			Serializer.SchemaSerialize(_hRDateAndTimeStructFormatter, ref buffer, ref offset, value.SavedDate);
		}

		public void Deserialize(byte[] buffer, ref int offset, ref HREmployeeSystem value)
		{
			Serializer.DeserializeSchema<HREmployeeSystem>(ref SchemaDeserializer, buffer, ref offset, "savedEmployees", "FirstHour", "SecondHour", "TotalWage", "Money", "EmployeeLimit", "RerollCost", "SavedDate");
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _list_EmployeeSaveDataFormatter, buffer, ref offset, ref value.savedEmployees);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _intFormatter, buffer, ref offset, ref value.FirstHour);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _intFormatter, buffer, ref offset, ref value.SecondHour);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _intFormatter, buffer, ref offset, ref value.TotalWage);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _intFormatter, buffer, ref offset, ref value.Money);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _intFormatter, buffer, ref offset, ref value.EmployeeLimit);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _intFormatter, buffer, ref offset, ref value.RerollCost);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _hRDateAndTimeStructFormatter, buffer, ref offset, ref value.SavedDate);
		}
	}

	internal class EmployeeSaveDataFormatter : IFormatter<HREmployeeSystem.EmployeeSaveData>
	{
		CerasSerializer Serializer;
		CerasSerializer.SchemaDeserializer SchemaDeserializer;
		IFormatter<bool> _boolFormatter;
		IFormatter<int> _intFormatter;
		IFormatter<System.Int32[,]> _int322DArrayFormatter;
		IFormatter<System.Int32[]> _int32ArrayFormatter;
		IFormatter<string> _stringFormatter;
		IFormatter<System.String[]> _stringArrayFormatter;
		IFormatter<UnityEngine.Vector3> _vector3Formatter;

		public void Serialize(ref byte[] buffer, ref int offset, HREmployeeSystem.EmployeeSaveData value)
		{
			Serializer.SerializeSchema<HREmployeeSystem.EmployeeSaveData>(ref buffer, ref offset, "IsUnpaid", "IsWorking", "AuxID", "ComponentID", "Health", "HireCost", "PriorityLimit", "Wage", "WageModifcation", "skillTaskCodes", "InventoryItemCounts", "taskDataCodes", "ActiveContainerID", "ActiveItemID", "CustomizationCode", "EmployeeName", "PlotName", "Priorities", "ShopName", "InventoryUniqueIDS", "genericTaskDatas", "specificTaskDatas", "EulerAngles", "Position");
			Serializer.SchemaSerialize(_boolFormatter, ref buffer, ref offset, value.IsUnpaid);
			Serializer.SchemaSerialize(_boolFormatter, ref buffer, ref offset, value.IsWorking);
			Serializer.SchemaSerialize(_intFormatter, ref buffer, ref offset, value.AuxID);
			Serializer.SchemaSerialize(_intFormatter, ref buffer, ref offset, value.ComponentID);
			Serializer.SchemaSerialize(_intFormatter, ref buffer, ref offset, value.Health);
			Serializer.SchemaSerialize(_intFormatter, ref buffer, ref offset, value.HireCost);
			Serializer.SchemaSerialize(_intFormatter, ref buffer, ref offset, value.PriorityLimit);
			Serializer.SchemaSerialize(_intFormatter, ref buffer, ref offset, value.Wage);
			Serializer.SchemaSerialize(_intFormatter, ref buffer, ref offset, value.WageModifcation);
			Serializer.SchemaSerialize(_int322DArrayFormatter, ref buffer, ref offset, value.skillTaskCodes);
			Serializer.SchemaSerialize(_int32ArrayFormatter, ref buffer, ref offset, value.InventoryItemCounts);
			Serializer.SchemaSerialize(_int32ArrayFormatter, ref buffer, ref offset, value.taskDataCodes);
			Serializer.SchemaSerialize(_stringFormatter, ref buffer, ref offset, value.ActiveContainerID);
			Serializer.SchemaSerialize(_stringFormatter, ref buffer, ref offset, value.ActiveItemID);
			Serializer.SchemaSerialize(_stringFormatter, ref buffer, ref offset, value.CustomizationCode);
			Serializer.SchemaSerialize(_stringFormatter, ref buffer, ref offset, value.EmployeeName);
			Serializer.SchemaSerialize(_stringFormatter, ref buffer, ref offset, value.PlotName);
			Serializer.SchemaSerialize(_stringFormatter, ref buffer, ref offset, value.Priorities);
			Serializer.SchemaSerialize(_intFormatter, ref buffer, ref offset, value.ShopIndex);
			Serializer.SchemaSerialize(_stringArrayFormatter, ref buffer, ref offset, value.InventoryUniqueIDS);
			Serializer.SchemaSerialize(_stringArrayFormatter, ref buffer, ref offset, value.genericTaskDatas);
			Serializer.SchemaSerialize(_stringArrayFormatter, ref buffer, ref offset, value.specificTaskDatas);
			Serializer.SchemaSerialize(_vector3Formatter, ref buffer, ref offset, value.EulerAngles);
			Serializer.SchemaSerialize(_vector3Formatter, ref buffer, ref offset, value.Position);
		}

		public void Deserialize(byte[] buffer, ref int offset, ref HREmployeeSystem.EmployeeSaveData value)
		{
			Serializer.DeserializeSchema<HREmployeeSystem.EmployeeSaveData>(ref SchemaDeserializer, buffer, ref offset, "IsUnpaid", "IsWorking", "AuxID", "ComponentID", "Health", "HireCost", "PriorityLimit", "Wage", "WageModifcation", "skillTaskCodes", "InventoryItemCounts", "taskDataCodes", "ActiveContainerID", "ActiveItemID", "CustomizationCode", "EmployeeName", "PlotName", "Priorities", "ShopName", "InventoryUniqueIDS", "genericTaskDatas", "specificTaskDatas", "EulerAngles", "Position");
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _boolFormatter, buffer, ref offset, ref value.IsUnpaid);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _boolFormatter, buffer, ref offset, ref value.IsWorking);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _intFormatter, buffer, ref offset, ref value.AuxID);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _intFormatter, buffer, ref offset, ref value.ComponentID);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _intFormatter, buffer, ref offset, ref value.Health);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _intFormatter, buffer, ref offset, ref value.HireCost);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _intFormatter, buffer, ref offset, ref value.PriorityLimit);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _intFormatter, buffer, ref offset, ref value.Wage);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _intFormatter, buffer, ref offset, ref value.WageModifcation);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _int322DArrayFormatter, buffer, ref offset, ref value.skillTaskCodes);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _int32ArrayFormatter, buffer, ref offset, ref value.InventoryItemCounts);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _int32ArrayFormatter, buffer, ref offset, ref value.taskDataCodes);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _stringFormatter, buffer, ref offset, ref value.ActiveContainerID);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _stringFormatter, buffer, ref offset, ref value.ActiveItemID);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _stringFormatter, buffer, ref offset, ref value.CustomizationCode);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _stringFormatter, buffer, ref offset, ref value.EmployeeName);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _stringFormatter, buffer, ref offset, ref value.PlotName);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _stringFormatter, buffer, ref offset, ref value.Priorities);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _intFormatter, buffer, ref offset, ref value.ShopIndex);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _stringArrayFormatter, buffer, ref offset, ref value.InventoryUniqueIDS);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _stringArrayFormatter, buffer, ref offset, ref value.genericTaskDatas);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _stringArrayFormatter, buffer, ref offset, ref value.specificTaskDatas);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _vector3Formatter, buffer, ref offset, ref value.EulerAngles);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _vector3Formatter, buffer, ref offset, ref value.Position);
		}
	}

	internal class HRPlotManagerFormatter : IFormatter<HRPlotManager>
	{
		CerasSerializer Serializer;
		CerasSerializer.SchemaDeserializer SchemaDeserializer;
		IFormatter<bool> _boolFormatter;
		IFormatter<PlotManagerSaveData> _plotManagerSaveDataFormatter;

		public void Serialize(ref byte[] buffer, ref int offset, HRPlotManager value)
		{
			Serializer.SerializeSchema<HRPlotManager>(ref buffer, ref offset, "bHasSave", "saveData");
			Serializer.SchemaSerialize(_boolFormatter, ref buffer, ref offset, value.bHasSave);
			Serializer.SchemaSerialize(_plotManagerSaveDataFormatter, ref buffer, ref offset, value.saveData);
		}

		public void Deserialize(byte[] buffer, ref int offset, ref HRPlotManager value)
		{
			Serializer.DeserializeSchema<HRPlotManager>(ref SchemaDeserializer, buffer, ref offset, "bHasSave", "saveData");
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _boolFormatter, buffer, ref offset, ref value.bHasSave);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _plotManagerSaveDataFormatter, buffer, ref offset, ref value.saveData);
		}
	}

	internal class PlotManagerSaveDataFormatter : IFormatter<PlotManagerSaveData>
	{
		CerasSerializer Serializer;
		CerasSerializer.SchemaDeserializer SchemaDeserializer;
		IFormatter<EmployeeSystem.Gathering.GatheringArea[]> _gatheringAreaArrayFormatter;
		IFormatter<System.Byte[]> _byteArrayFormatter;
		IFormatter<System.Int16[]> _int16ArrayFormatter;

		public void Serialize(ref byte[] buffer, ref int offset, PlotManagerSaveData value)
		{
			Serializer.SerializeSchema<PlotManagerSaveData>(ref buffer, ref offset, "unlockedGatheringAreas", "unlockedPlots", "unlockedResources");
			Serializer.SchemaSerialize(_gatheringAreaArrayFormatter, ref buffer, ref offset, value.unlockedGatheringAreas);
			Serializer.SchemaSerialize(_byteArrayFormatter, ref buffer, ref offset, value.unlockedPlots);
			Serializer.SchemaSerialize(_int16ArrayFormatter, ref buffer, ref offset, value.unlockedResources);
		}

		public void Deserialize(byte[] buffer, ref int offset, ref PlotManagerSaveData value)
		{
			Serializer.DeserializeSchema<PlotManagerSaveData>(ref SchemaDeserializer, buffer, ref offset, "unlockedGatheringAreas", "unlockedPlots", "unlockedResources");
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _gatheringAreaArrayFormatter, buffer, ref offset, ref value.unlockedGatheringAreas);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _byteArrayFormatter, buffer, ref offset, ref value.unlockedPlots);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _int16ArrayFormatter, buffer, ref offset, ref value.unlockedResources);
		}
	}

	internal class HREncounterSpawnerFormatter : IFormatter<HREncounterSpawner>
	{
		CerasSerializer Serializer;
		CerasSerializer.SchemaDeserializer SchemaDeserializer;
		IFormatter<System.Collections.Generic.List<HREncounterSaveData>> _list_HREncounterSaveDataFormatter;

		public void Serialize(ref byte[] buffer, ref int offset, HREncounterSpawner value)
		{
			Serializer.SerializeSchema<HREncounterSpawner>(ref buffer, ref offset, "EncountersCache");
			Serializer.SchemaSerialize(_list_HREncounterSaveDataFormatter, ref buffer, ref offset, value.EncountersCache);
		}

		public void Deserialize(byte[] buffer, ref int offset, ref HREncounterSpawner value)
		{
			Serializer.DeserializeSchema<HREncounterSpawner>(ref SchemaDeserializer, buffer, ref offset, "EncountersCache");
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _list_HREncounterSaveDataFormatter, buffer, ref offset, ref value.EncountersCache);
		}
	}

	internal class HREncounterSaveDataFormatter : IFormatter<HREncounterSaveData>
	{
		CerasSerializer Serializer;
		CerasSerializer.SchemaDeserializer SchemaDeserializer;
		IFormatter<EncounterObjectiveSaveData[]> _encounterObjectiveSaveDataArrayFormatter;
		IFormatter<System.Collections.Generic.List<HREncounterDynamicEnemyData>> _list_HREncounterDynamicEnemyDataFormatter;
		IFormatter<int> _intFormatter;
		IFormatter<UnityEngine.Vector3> _vector3Formatter;

		public void Serialize(ref byte[] buffer, ref int offset, HREncounterSaveData value)
		{
			Serializer.SerializeSchema<HREncounterSaveData>(ref buffer, ref offset, "SavedObjectives", "DynamicEnemiesList", "EncounterID", "EncounterPosition");
			Serializer.SchemaSerialize(_encounterObjectiveSaveDataArrayFormatter, ref buffer, ref offset, value.SavedObjectives);
			Serializer.SchemaSerialize(_list_HREncounterDynamicEnemyDataFormatter, ref buffer, ref offset, value.DynamicEnemiesList);
			Serializer.SchemaSerialize(_intFormatter, ref buffer, ref offset, value.EncounterID);
			Serializer.SchemaSerialize(_vector3Formatter, ref buffer, ref offset, value.EncounterPosition);
		}

		public void Deserialize(byte[] buffer, ref int offset, ref HREncounterSaveData value)
		{
			Serializer.DeserializeSchema<HREncounterSaveData>(ref SchemaDeserializer, buffer, ref offset, "SavedObjectives", "DynamicEnemiesList", "EncounterID", "EncounterPosition");
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _encounterObjectiveSaveDataArrayFormatter, buffer, ref offset, ref value.SavedObjectives);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _list_HREncounterDynamicEnemyDataFormatter, buffer, ref offset, ref value.DynamicEnemiesList);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _intFormatter, buffer, ref offset, ref value.EncounterID);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _vector3Formatter, buffer, ref offset, ref value.EncounterPosition);
		}
	}

	internal class HREncounterDynamicEnemyDataFormatter : IFormatter<HREncounterDynamicEnemyData>
	{
		CerasSerializer Serializer;
		CerasSerializer.SchemaDeserializer SchemaDeserializer;
		IFormatter<int> _intFormatter;
		IFormatter<float> _floatFormatter;
		IFormatter<string> _stringFormatter;

		public void Serialize(ref byte[] buffer, ref int offset, HREncounterDynamicEnemyData value)
		{
			Serializer.SerializeSchema<HREncounterDynamicEnemyData>(ref buffer, ref offset, "Amount", "EnemyID", "SpawningTime", "EnemyName");
			Serializer.SchemaSerialize(_intFormatter, ref buffer, ref offset, value.Amount);
			Serializer.SchemaSerialize(_intFormatter, ref buffer, ref offset, value.EnemyID);
			Serializer.SchemaSerialize(_floatFormatter, ref buffer, ref offset, value.SpawningTime);
			Serializer.SchemaSerialize(_stringFormatter, ref buffer, ref offset, value.EnemyName);
		}

		public void Deserialize(byte[] buffer, ref int offset, ref HREncounterDynamicEnemyData value)
		{
			Serializer.DeserializeSchema<HREncounterDynamicEnemyData>(ref SchemaDeserializer, buffer, ref offset, "Amount", "EnemyID", "SpawningTime", "EnemyName");
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _intFormatter, buffer, ref offset, ref value.Amount);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _intFormatter, buffer, ref offset, ref value.EnemyID);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _floatFormatter, buffer, ref offset, ref value.SpawningTime);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _stringFormatter, buffer, ref offset, ref value.EnemyName);
		}
	}

	internal class EncounterObjectiveSaveDataFormatter : IFormatter<EncounterObjectiveSaveData>
	{
		CerasSerializer Serializer;
		CerasSerializer.SchemaDeserializer SchemaDeserializer;
		IFormatter<bool> _boolFormatter;

		public void Serialize(ref byte[] buffer, ref int offset, EncounterObjectiveSaveData value)
		{
			Serializer.SerializeSchema<EncounterObjectiveSaveData>(ref buffer, ref offset, "bCompleted", "bSucceeded");
			Serializer.SchemaSerialize(_boolFormatter, ref buffer, ref offset, value.bCompleted);
			Serializer.SchemaSerialize(_boolFormatter, ref buffer, ref offset, value.bSucceeded);
		}

		public void Deserialize(byte[] buffer, ref int offset, ref EncounterObjectiveSaveData value)
		{
			Serializer.DeserializeSchema<EncounterObjectiveSaveData>(ref SchemaDeserializer, buffer, ref offset, "bCompleted", "bSucceeded");
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _boolFormatter, buffer, ref offset, ref value.bCompleted);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _boolFormatter, buffer, ref offset, ref value.bSucceeded);
		}
	}

	internal class HRPlantComponentFormatter : IFormatter<HRPlantComponent>
	{
		CerasSerializer Serializer;
		CerasSerializer.SchemaDeserializer SchemaDeserializer;
		IFormatter<bool> _boolFormatter;
		IFormatter<HRDateAndTimeStruct> _hRDateAndTimeStructFormatter;

		public void Serialize(ref byte[] buffer, ref int offset, HRPlantComponent value)
		{
			Serializer.SerializeSchema<HRPlantComponent>(ref buffer, ref offset, "bIsGrowing", "SavedDateAndTime");
			Serializer.SchemaSerialize(_boolFormatter, ref buffer, ref offset, value.bIsGrowing);
			Serializer.SchemaSerialize(_hRDateAndTimeStructFormatter, ref buffer, ref offset, value.SavedDateAndTime);
		}

		public void Deserialize(byte[] buffer, ref int offset, ref HRPlantComponent value)
		{
			Serializer.DeserializeSchema<HRPlantComponent>(ref SchemaDeserializer, buffer, ref offset, "bIsGrowing", "SavedDateAndTime");
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _boolFormatter, buffer, ref offset, ref value.bIsGrowing);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _hRDateAndTimeStructFormatter, buffer, ref offset, ref value.SavedDateAndTime);
		}
	}

	internal class InnSystemFormatter : IFormatter<InnSystem>
	{
		CerasSerializer Serializer;
		CerasSerializer.SchemaDeserializer SchemaDeserializer;
		IFormatter<System.Collections.Generic.List<InnSystem.SavedRoomStatus>> _list_SavedRoomStatusFormatter;

		public void Serialize(ref byte[] buffer, ref int offset, InnSystem value)
		{
			Serializer.SerializeSchema<InnSystem>(ref buffer, ref offset, "savedRoomStatus");
			Serializer.SchemaSerialize(_list_SavedRoomStatusFormatter, ref buffer, ref offset, value.savedRoomStatus);
		}

		public void Deserialize(byte[] buffer, ref int offset, ref InnSystem value)
		{
			Serializer.DeserializeSchema<InnSystem>(ref SchemaDeserializer, buffer, ref offset, "savedRoomStatus");
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _list_SavedRoomStatusFormatter, buffer, ref offset, ref value.savedRoomStatus);
		}
	}

	internal class SavedRoomStatusFormatter : IFormatter<InnSystem.SavedRoomStatus>
	{
		CerasSerializer Serializer;
		CerasSerializer.SchemaDeserializer SchemaDeserializer;
		IFormatter<HRDateStruct> _hRDateStructFormatter;
		IFormatter<bool> _boolFormatter;
		IFormatter<int> _intFormatter;

		public void Serialize(ref byte[] buffer, ref int offset, InnSystem.SavedRoomStatus value)
		{
			Serializer.SerializeSchema<InnSystem.SavedRoomStatus>(ref buffer, ref offset, "date", "bIsLocked", "hour", "minute");
			Serializer.SchemaSerialize(_hRDateStructFormatter, ref buffer, ref offset, value.date);
			Serializer.SchemaSerialize(_boolFormatter, ref buffer, ref offset, value.bIsLocked);
			Serializer.SchemaSerialize(_intFormatter, ref buffer, ref offset, value.hour);
			Serializer.SchemaSerialize(_intFormatter, ref buffer, ref offset, value.minute);
		}

		public void Deserialize(byte[] buffer, ref int offset, ref InnSystem.SavedRoomStatus value)
		{
			Serializer.DeserializeSchema<InnSystem.SavedRoomStatus>(ref SchemaDeserializer, buffer, ref offset, "date", "bIsLocked", "hour", "minute");
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _hRDateStructFormatter, buffer, ref offset, ref value.date);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _boolFormatter, buffer, ref offset, ref value.bIsLocked);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _intFormatter, buffer, ref offset, ref value.hour);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _intFormatter, buffer, ref offset, ref value.minute);
		}
	}

	internal class ReplaceOnHitFormatter : IFormatter<ReplaceOnHit>
	{
		CerasSerializer Serializer;
		CerasSerializer.SchemaDeserializer SchemaDeserializer;

		public void Serialize(ref byte[] buffer, ref int offset, ReplaceOnHit value)
		{
		}

		public void Deserialize(byte[] buffer, ref int offset, ref ReplaceOnHit value)
		{
		}
	}

	internal class ReplaceOnHitInteractFormatter : IFormatter<ReplaceOnHitInteract>
	{
		CerasSerializer Serializer;
		CerasSerializer.SchemaDeserializer SchemaDeserializer;

		public void Serialize(ref byte[] buffer, ref int offset, ReplaceOnHitInteract value)
		{
		}

		public void Deserialize(byte[] buffer, ref int offset, ref ReplaceOnHitInteract value)
		{
		}
	}

	internal class HRShopEntityComponentFormatter : IFormatter<HRShopEntityComponent>
	{
		CerasSerializer Serializer;
		CerasSerializer.SchemaDeserializer SchemaDeserializer;
		IFormatter<bool> _boolFormatter;
		IFormatter<string> _stringFormatter;
		IFormatter<float> _floatFormatter;

		public void Serialize(ref byte[] buffer, ref int offset, HRShopEntityComponent value)
		{
			Serializer.SerializeSchema<HRShopEntityComponent>(ref buffer, ref offset, "bHasShopName", "OverrideShopName", "ShopPerformance", "FinalShopPerformance");
			Serializer.SchemaSerialize(_boolFormatter, ref buffer, ref offset, value.bHasShopName);
			Serializer.SchemaSerialize(_stringFormatter, ref buffer, ref offset, value.OverrideShopName);
			Serializer.SchemaSerialize(_floatFormatter, ref buffer, ref offset, value.ShopPerformance);
			Serializer.SchemaSerialize(_floatFormatter, ref buffer, ref offset, value.FinalShopPerformance);
		}

		public void Deserialize(byte[] buffer, ref int offset, ref HRShopEntityComponent value)
		{
			Serializer.DeserializeSchema<HRShopEntityComponent>(ref SchemaDeserializer, buffer, ref offset, "bHasShopName", "OverrideShopName", "ShopPerformance", "FinalShopPerformance");
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _boolFormatter, buffer, ref offset, ref value.bHasShopName);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _stringFormatter, buffer, ref offset, ref value.OverrideShopName);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _floatFormatter, buffer, ref offset, ref value.ShopPerformance);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _floatFormatter, buffer, ref offset, ref value.FinalShopPerformance);
		}
	}

	internal class HRZenaFightHelperFormatter : IFormatter<HRZenaFightHelper>
	{
		CerasSerializer Serializer;
		CerasSerializer.SchemaDeserializer SchemaDeserializer;
		IFormatter<System.Collections.Generic.List<bool>> _list_boolFormatter;

		public void Serialize(ref byte[] buffer, ref int offset, HRZenaFightHelper value)
		{
			Serializer.SerializeSchema<HRZenaFightHelper>(ref buffer, ref offset, "SavedUnlockedBonuses");
			Serializer.SchemaSerialize(_list_boolFormatter, ref buffer, ref offset, value.SavedUnlockedBonuses);
		}

		public void Deserialize(byte[] buffer, ref int offset, ref HRZenaFightHelper value)
		{
			Serializer.DeserializeSchema<HRZenaFightHelper>(ref SchemaDeserializer, buffer, ref offset, "SavedUnlockedBonuses");
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _list_boolFormatter, buffer, ref offset, ref value.SavedUnlockedBonuses);
		}
	}

	internal class HRCalendarManagerFormatter : IFormatter<HRCalendarManager>
	{
		CerasSerializer Serializer;
		CerasSerializer.SchemaDeserializer SchemaDeserializer;
		IFormatter<HRDateStruct> _hRDateStructFormatter;

		public void Serialize(ref byte[] buffer, ref int offset, HRCalendarManager value)
		{
			Serializer.SerializeSchema<HRCalendarManager>(ref buffer, ref offset, "SavedCurrentDate", "DefaultDate");
			Serializer.SchemaSerialize(_hRDateStructFormatter, ref buffer, ref offset, value.SavedCurrentDate);
			Serializer.SchemaSerialize(_hRDateStructFormatter, ref buffer, ref offset, value.DefaultDate);
		}

		public void Deserialize(byte[] buffer, ref int offset, ref HRCalendarManager value)
		{
			Serializer.DeserializeSchema<HRCalendarManager>(ref SchemaDeserializer, buffer, ref offset, "SavedCurrentDate", "DefaultDate");
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _hRDateStructFormatter, buffer, ref offset, ref value.SavedCurrentDate);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _hRDateStructFormatter, buffer, ref offset, ref value.DefaultDate);
		}
	}

	internal class HRConversationPoolManagerFormatter : IFormatter<HRConversationPoolManager>
	{
		CerasSerializer Serializer;
		CerasSerializer.SchemaDeserializer SchemaDeserializer;
		IFormatter<System.Collections.Generic.List<HRConversationPoolManager.ConversationPool>> _list_ConversationPoolFormatter;
		IFormatter<System.Collections.Generic.List<HRConversationPoolManager.ConversationPoolCollection>> _list_ConversationPoolCollectionFormatter;

		public void Serialize(ref byte[] buffer, ref int offset, HRConversationPoolManager value)
		{
			Serializer.SerializeSchema<HRConversationPoolManager>(ref buffer, ref offset, "ConversationPools", "PoolCollections");
			Serializer.SchemaSerialize(_list_ConversationPoolFormatter, ref buffer, ref offset, value.ConversationPools);
			Serializer.SchemaSerialize(_list_ConversationPoolCollectionFormatter, ref buffer, ref offset, value.PoolCollections);
		}

		public void Deserialize(byte[] buffer, ref int offset, ref HRConversationPoolManager value)
		{
			Serializer.DeserializeSchema<HRConversationPoolManager>(ref SchemaDeserializer, buffer, ref offset, "ConversationPools", "PoolCollections");
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _list_ConversationPoolFormatter, buffer, ref offset, ref value.ConversationPools);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _list_ConversationPoolCollectionFormatter, buffer, ref offset, ref value.PoolCollections);
		}
	}

	internal class ConversationPoolFormatter : IFormatter<HRConversationPoolManager.ConversationPool>
	{
		CerasSerializer Serializer;
		CerasSerializer.SchemaDeserializer SchemaDeserializer;
		IFormatter<bool> _boolFormatter;
		IFormatter<System.Collections.Generic.List<string>> _list_stringFormatter;
		IFormatter<string> _stringFormatter;

		public void Serialize(ref byte[] buffer, ref int offset, HRConversationPoolManager.ConversationPool value)
		{
			Serializer.SerializeSchema<HRConversationPoolManager.ConversationPool>(ref buffer, ref offset, "bUniquePool", "AvailableConversations", "CompletedUniqueConversations", "PoolFolder");
			Serializer.SchemaSerialize(_boolFormatter, ref buffer, ref offset, value.bUniquePool);
			Serializer.SchemaSerialize(_list_stringFormatter, ref buffer, ref offset, value.AvailableConversations);
			Serializer.SchemaSerialize(_list_stringFormatter, ref buffer, ref offset, value.CompletedUniqueConversations);
			Serializer.SchemaSerialize(_stringFormatter, ref buffer, ref offset, value.PoolFolder);
		}

		public void Deserialize(byte[] buffer, ref int offset, ref HRConversationPoolManager.ConversationPool value)
		{
			Serializer.DeserializeSchema<HRConversationPoolManager.ConversationPool>(ref SchemaDeserializer, buffer, ref offset, "bUniquePool", "AvailableConversations", "CompletedUniqueConversations", "PoolFolder");
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _boolFormatter, buffer, ref offset, ref value.bUniquePool);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _list_stringFormatter, buffer, ref offset, ref value.AvailableConversations);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _list_stringFormatter, buffer, ref offset, ref value.CompletedUniqueConversations);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _stringFormatter, buffer, ref offset, ref value.PoolFolder);
		}
	}

	internal class ConversationPoolCollectionFormatter : IFormatter<HRConversationPoolManager.ConversationPoolCollection>
	{
		CerasSerializer Serializer;
		CerasSerializer.SchemaDeserializer SchemaDeserializer;
		IFormatter<System.Collections.Generic.List<string>> _list_stringFormatter;
		IFormatter<string> _stringFormatter;

		public void Serialize(ref byte[] buffer, ref int offset, HRConversationPoolManager.ConversationPoolCollection value)
		{
			Serializer.SerializeSchema<HRConversationPoolManager.ConversationPoolCollection>(ref buffer, ref offset, "FolderNames", "CollectionName");
			Serializer.SchemaSerialize(_list_stringFormatter, ref buffer, ref offset, value.FolderNames);
			Serializer.SchemaSerialize(_stringFormatter, ref buffer, ref offset, value.CollectionName);
		}

		public void Deserialize(byte[] buffer, ref int offset, ref HRConversationPoolManager.ConversationPoolCollection value)
		{
			Serializer.DeserializeSchema<HRConversationPoolManager.ConversationPoolCollection>(ref SchemaDeserializer, buffer, ref offset, "FolderNames", "CollectionName");
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _list_stringFormatter, buffer, ref offset, ref value.FolderNames);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _stringFormatter, buffer, ref offset, ref value.CollectionName);
		}
	}

	internal class HRDayManagerFormatter : IFormatter<HRDayManager>
	{
		CerasSerializer Serializer;
		CerasSerializer.SchemaDeserializer SchemaDeserializer;
		IFormatter<bool> _boolFormatter;
		IFormatter<int> _intFormatter;
		IFormatter<float> _floatFormatter;

		public void Serialize(ref byte[] buffer, ref int offset, HRDayManager value)
		{
			Serializer.SerializeSchema<HRDayManager>(ref buffer, ref offset, "bTimePassEnabled", "DaysPassed", "SavedTimeHour", "SavedTimeMinute");
			Serializer.SchemaSerialize(_boolFormatter, ref buffer, ref offset, value.bTimePassEnabled);
			Serializer.SchemaSerialize(_intFormatter, ref buffer, ref offset, value.DaysPassed);
			Serializer.SchemaSerialize(_intFormatter, ref buffer, ref offset, value.SavedTimeHour);
			Serializer.SchemaSerialize(_floatFormatter, ref buffer, ref offset, value.SavedTimeMinute);
		}

		public void Deserialize(byte[] buffer, ref int offset, ref HRDayManager value)
		{
			Serializer.DeserializeSchema<HRDayManager>(ref SchemaDeserializer, buffer, ref offset, "bTimePassEnabled", "DaysPassed", "SavedTimeHour", "SavedTimeMinute");
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _boolFormatter, buffer, ref offset, ref value.bTimePassEnabled);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _intFormatter, buffer, ref offset, ref value.DaysPassed);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _intFormatter, buffer, ref offset, ref value.SavedTimeHour);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _floatFormatter, buffer, ref offset, ref value.SavedTimeMinute);
		}
	}

	internal class HRTimeTriggerFormatter : IFormatter<HRTimeTrigger>
	{
		CerasSerializer Serializer;
		CerasSerializer.SchemaDeserializer SchemaDeserializer;
		IFormatter<bool> _boolFormatter;

		public void Serialize(ref byte[] buffer, ref int offset, HRTimeTrigger value)
		{
			Serializer.SerializeSchema<HRTimeTrigger>(ref buffer, ref offset, "bAlreadyDone");
			Serializer.SchemaSerialize(_boolFormatter, ref buffer, ref offset, value.bAlreadyDone);
		}

		public void Deserialize(byte[] buffer, ref int offset, ref HRTimeTrigger value)
		{
			Serializer.DeserializeSchema<HRTimeTrigger>(ref SchemaDeserializer, buffer, ref offset, "bAlreadyDone");
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _boolFormatter, buffer, ref offset, ref value.bAlreadyDone);
		}
	}

	internal class HRConsumableRecipeUnlockFormatter : IFormatter<HRConsumableRecipeUnlock>
	{
		CerasSerializer Serializer;
		CerasSerializer.SchemaDeserializer SchemaDeserializer;

		public void Serialize(ref byte[] buffer, ref int offset, HRConsumableRecipeUnlock value)
		{
		}

		public void Deserialize(byte[] buffer, ref int offset, ref HRConsumableRecipeUnlock value)
		{
		}
	}

	internal class HRDeathBoxFormatter : IFormatter<HRDeathBox>
	{
		CerasSerializer Serializer;
		CerasSerializer.SchemaDeserializer SchemaDeserializer;
		IFormatter<string> _stringFormatter;

		public void Serialize(ref byte[] buffer, ref int offset, HRDeathBox value)
		{
			Serializer.SerializeSchema<HRDeathBox>(ref buffer, ref offset, "SavedDropCharacterName");
			Serializer.SchemaSerialize(_stringFormatter, ref buffer, ref offset, value.SavedDropCharacterName);
		}

		public void Deserialize(byte[] buffer, ref int offset, ref HRDeathBox value)
		{
			Serializer.DeserializeSchema<HRDeathBox>(ref SchemaDeserializer, buffer, ref offset, "SavedDropCharacterName");
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _stringFormatter, buffer, ref offset, ref value.SavedDropCharacterName);
		}
	}

	internal class HRDisplayContainerFormatter : IFormatter<HRDisplayContainer>
	{
		CerasSerializer Serializer;
		CerasSerializer.SchemaDeserializer SchemaDeserializer;

		public void Serialize(ref byte[] buffer, ref int offset, HRDisplayContainer value)
		{
		}

		public void Deserialize(byte[] buffer, ref int offset, ref HRDisplayContainer value)
		{
		}
	}

	internal class HRDynamicZiplineFormatter : IFormatter<HRDynamicZipline>
	{
		CerasSerializer Serializer;
		CerasSerializer.SchemaDeserializer SchemaDeserializer;
		IFormatter<UnityEngine.Vector3> _vector3Formatter;

		public void Serialize(ref byte[] buffer, ref int offset, HRDynamicZipline value)
		{
			Serializer.SerializeSchema<HRDynamicZipline>(ref buffer, ref offset, "ZiplineEndPosition");
			Serializer.SchemaSerialize(_vector3Formatter, ref buffer, ref offset, value.ZiplineEndPosition);
		}

		public void Deserialize(byte[] buffer, ref int offset, ref HRDynamicZipline value)
		{
			Serializer.DeserializeSchema<HRDynamicZipline>(ref SchemaDeserializer, buffer, ref offset, "ZiplineEndPosition");
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _vector3Formatter, buffer, ref offset, ref value.ZiplineEndPosition);
		}
	}

	internal class HRItemCustomizationStationFormatter : IFormatter<HRItemCustomizationStation>
	{
		CerasSerializer Serializer;
		CerasSerializer.SchemaDeserializer SchemaDeserializer;
		IFormatter<bool> _boolFormatter;
		IFormatter<System.Collections.Generic.List<HRCategory>> _list_HRCategoryFormatter;
		IFormatter<System.Collections.Generic.List<int>> _list_intFormatter;
		IFormatter<int> _intFormatter;

		public void Serialize(ref byte[] buffer, ref int offset, HRItemCustomizationStation value)
		{
			Serializer.SerializeSchema<HRItemCustomizationStation>(ref buffer, ref offset, "bEmployeeUse", "bUseInput", "bUseOutput", "bUseItems", "AllowedContainerCategories", "AllowedItems", "AllowedFilterItemsIDs", "NumberOfValidItems");
			Serializer.SchemaSerialize(_boolFormatter, ref buffer, ref offset, value.bEmployeeUse);
			Serializer.SchemaSerialize(_boolFormatter, ref buffer, ref offset, value.bUseInput);
			Serializer.SchemaSerialize(_boolFormatter, ref buffer, ref offset, value.bUseOutput);
			Serializer.SchemaSerialize(_boolFormatter, ref buffer, ref offset, value.bUseItems);
			Serializer.SchemaSerialize(_list_HRCategoryFormatter, ref buffer, ref offset, value.AllowedContainerCategories);
			Serializer.SchemaSerialize(_list_intFormatter, ref buffer, ref offset, value.AllowedItems);
			Serializer.SchemaSerialize(_list_intFormatter, ref buffer, ref offset, value.AllowedFilterItemsIDs);
			Serializer.SchemaSerialize(_intFormatter, ref buffer, ref offset, value.NumberOfValidItems);
		}

		public void Deserialize(byte[] buffer, ref int offset, ref HRItemCustomizationStation value)
		{
			Serializer.DeserializeSchema<HRItemCustomizationStation>(ref SchemaDeserializer, buffer, ref offset, "bEmployeeUse", "bUseInput", "bUseOutput", "bUseItems", "AllowedContainerCategories", "AllowedItems", "AllowedFilterItemsIDs", "NumberOfValidItems");
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _boolFormatter, buffer, ref offset, ref value.bEmployeeUse);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _boolFormatter, buffer, ref offset, ref value.bUseInput);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _boolFormatter, buffer, ref offset, ref value.bUseOutput);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _boolFormatter, buffer, ref offset, ref value.bUseItems);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _list_HRCategoryFormatter, buffer, ref offset, ref value.AllowedContainerCategories);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _list_intFormatter, buffer, ref offset, ref value.AllowedItems);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _list_intFormatter, buffer, ref offset, ref value.AllowedFilterItemsIDs);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _intFormatter, buffer, ref offset, ref value.NumberOfValidItems);
		}
	}

	internal class HRMannequinContainerFormatter : IFormatter<HRMannequinContainer>
	{
		CerasSerializer Serializer;
		CerasSerializer.SchemaDeserializer SchemaDeserializer;
		IFormatter<bool> _boolFormatter;
		IFormatter<System.Collections.Generic.List<HRCategory>> _list_HRCategoryFormatter;
		IFormatter<System.Collections.Generic.List<int>> _list_intFormatter;
		IFormatter<int> _intFormatter;

		public void Serialize(ref byte[] buffer, ref int offset, HRMannequinContainer value)
		{
			Serializer.SerializeSchema<HRMannequinContainer>(ref buffer, ref offset, "bEmployeeUse", "bUseInput", "bUseOutput", "bUseItems", "AllowedContainerCategories", "AllowedItems", "AllowedFilterItemsIDs", "NumberOfValidItems");
			Serializer.SchemaSerialize(_boolFormatter, ref buffer, ref offset, value.bEmployeeUse);
			Serializer.SchemaSerialize(_boolFormatter, ref buffer, ref offset, value.bUseInput);
			Serializer.SchemaSerialize(_boolFormatter, ref buffer, ref offset, value.bUseOutput);
			Serializer.SchemaSerialize(_boolFormatter, ref buffer, ref offset, value.bUseItems);
			Serializer.SchemaSerialize(_list_HRCategoryFormatter, ref buffer, ref offset, value.AllowedContainerCategories);
			Serializer.SchemaSerialize(_list_intFormatter, ref buffer, ref offset, value.AllowedItems);
			Serializer.SchemaSerialize(_list_intFormatter, ref buffer, ref offset, value.AllowedFilterItemsIDs);
			Serializer.SchemaSerialize(_intFormatter, ref buffer, ref offset, value.NumberOfValidItems);
		}

		public void Deserialize(byte[] buffer, ref int offset, ref HRMannequinContainer value)
		{
			Serializer.DeserializeSchema<HRMannequinContainer>(ref SchemaDeserializer, buffer, ref offset, "bEmployeeUse", "bUseInput", "bUseOutput", "bUseItems", "AllowedContainerCategories", "AllowedItems", "AllowedFilterItemsIDs", "NumberOfValidItems");
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _boolFormatter, buffer, ref offset, ref value.bEmployeeUse);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _boolFormatter, buffer, ref offset, ref value.bUseInput);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _boolFormatter, buffer, ref offset, ref value.bUseOutput);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _boolFormatter, buffer, ref offset, ref value.bUseItems);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _list_HRCategoryFormatter, buffer, ref offset, ref value.AllowedContainerCategories);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _list_intFormatter, buffer, ref offset, ref value.AllowedItems);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _list_intFormatter, buffer, ref offset, ref value.AllowedFilterItemsIDs);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _intFormatter, buffer, ref offset, ref value.NumberOfValidItems);
		}
	}

	internal class HROpenSignFormatter : IFormatter<HROpenSign>
	{
		CerasSerializer Serializer;
		CerasSerializer.SchemaDeserializer SchemaDeserializer;
		IFormatter<string> _stringFormatter;

		public void Serialize(ref byte[] buffer, ref int offset, HROpenSign value)
		{
			Serializer.SerializeSchema<HROpenSign>(ref buffer, ref offset, "OwningShopManagerID");
			Serializer.SchemaSerialize(_stringFormatter, ref buffer, ref offset, value.OwningShopManagerID);
		}

		public void Deserialize(byte[] buffer, ref int offset, ref HROpenSign value)
		{
			Serializer.DeserializeSchema<HROpenSign>(ref SchemaDeserializer, buffer, ref offset, "OwningShopManagerID");
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _stringFormatter, buffer, ref offset, ref value.OwningShopManagerID);
		}
	}

	internal class HRUpgradeTableFormatter : IFormatter<HRUpgradeTable>
	{
		CerasSerializer Serializer;
		CerasSerializer.SchemaDeserializer SchemaDeserializer;

		public void Serialize(ref byte[] buffer, ref int offset, HRUpgradeTable value)
		{
		}

		public void Deserialize(byte[] buffer, ref int offset, ref HRUpgradeTable value)
		{
		}
	}

	internal class HRZiplineAnchorFormatter : IFormatter<HRZiplineAnchor>
	{
		CerasSerializer Serializer;
		CerasSerializer.SchemaDeserializer SchemaDeserializer;
		IFormatter<bool> _boolFormatter;

		public void Serialize(ref byte[] buffer, ref int offset, HRZiplineAnchor value)
		{
			Serializer.SerializeSchema<HRZiplineAnchor>(ref buffer, ref offset, "bActivated");
			Serializer.SchemaSerialize(_boolFormatter, ref buffer, ref offset, value.bActivated);
		}

		public void Deserialize(byte[] buffer, ref int offset, ref HRZiplineAnchor value)
		{
			Serializer.DeserializeSchema<HRZiplineAnchor>(ref SchemaDeserializer, buffer, ref offset, "bActivated");
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _boolFormatter, buffer, ref offset, ref value.bActivated);
		}
	}

	internal class HRClothingComponentFormatter : IFormatter<HRClothingComponent>
	{
		CerasSerializer Serializer;
		CerasSerializer.SchemaDeserializer SchemaDeserializer;
		IFormatter<string> _stringFormatter;

		public void Serialize(ref byte[] buffer, ref int offset, HRClothingComponent value)
		{
			Serializer.SerializeSchema<HRClothingComponent>(ref buffer, ref offset, "SavedCustomizationID", "SavedCustomMaterialID");
			Serializer.SchemaSerialize(_stringFormatter, ref buffer, ref offset, value.SavedCustomizationID);
			Serializer.SchemaSerialize(_stringFormatter, ref buffer, ref offset, value.SavedCustomMaterialID);
		}

		public void Deserialize(byte[] buffer, ref int offset, ref HRClothingComponent value)
		{
			Serializer.DeserializeSchema<HRClothingComponent>(ref SchemaDeserializer, buffer, ref offset, "SavedCustomizationID", "SavedCustomMaterialID");
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _stringFormatter, buffer, ref offset, ref value.SavedCustomizationID);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _stringFormatter, buffer, ref offset, ref value.SavedCustomMaterialID);
		}
	}

	internal class HRContainerFormatter : IFormatter<HRContainer>
	{
		CerasSerializer Serializer;
		CerasSerializer.SchemaDeserializer SchemaDeserializer;
		IFormatter<bool> _boolFormatter;
		IFormatter<System.Collections.Generic.List<HRCategory>> _list_HRCategoryFormatter;
		IFormatter<System.Collections.Generic.List<int>> _list_intFormatter;
		IFormatter<int> _intFormatter;

		public void Serialize(ref byte[] buffer, ref int offset, HRContainer value)
		{
			Serializer.SerializeSchema<HRContainer>(ref buffer, ref offset, "bEmployeeUse", "bUseInput", "bUseOutput", "bUseItems", "AllowedContainerCategories", "AllowedItems", "AllowedFilterItemsIDs", "NumberOfValidItems");
			Serializer.SchemaSerialize(_boolFormatter, ref buffer, ref offset, value.bEmployeeUse);
			Serializer.SchemaSerialize(_boolFormatter, ref buffer, ref offset, value.bUseInput);
			Serializer.SchemaSerialize(_boolFormatter, ref buffer, ref offset, value.bUseOutput);
			Serializer.SchemaSerialize(_boolFormatter, ref buffer, ref offset, value.bUseItems);
			Serializer.SchemaSerialize(_list_HRCategoryFormatter, ref buffer, ref offset, value.AllowedContainerCategories);
			Serializer.SchemaSerialize(_list_intFormatter, ref buffer, ref offset, value.AllowedItems);
			Serializer.SchemaSerialize(_list_intFormatter, ref buffer, ref offset, value.AllowedFilterItemsIDs);
			Serializer.SchemaSerialize(_intFormatter, ref buffer, ref offset, value.NumberOfValidItems);
		}

		public void Deserialize(byte[] buffer, ref int offset, ref HRContainer value)
		{
			Serializer.DeserializeSchema<HRContainer>(ref SchemaDeserializer, buffer, ref offset, "bEmployeeUse", "bUseInput", "bUseOutput", "bUseItems", "AllowedContainerCategories", "AllowedItems", "AllowedFilterItemsIDs", "NumberOfValidItems");
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _boolFormatter, buffer, ref offset, ref value.bEmployeeUse);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _boolFormatter, buffer, ref offset, ref value.bUseInput);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _boolFormatter, buffer, ref offset, ref value.bUseOutput);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _boolFormatter, buffer, ref offset, ref value.bUseItems);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _list_HRCategoryFormatter, buffer, ref offset, ref value.AllowedContainerCategories);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _list_intFormatter, buffer, ref offset, ref value.AllowedItems);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _list_intFormatter, buffer, ref offset, ref value.AllowedFilterItemsIDs);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _intFormatter, buffer, ref offset, ref value.NumberOfValidItems);
		}
	}

	internal class HRFuelInventoryFormatter : IFormatter<HRFuelInventory>
	{
		CerasSerializer Serializer;
		CerasSerializer.SchemaDeserializer SchemaDeserializer;
		IFormatter<bool> _boolFormatter;
		IFormatter<float> _floatFormatter;

		public void Serialize(ref byte[] buffer, ref int offset, HRFuelInventory value)
		{
			Serializer.SerializeSchema<HRFuelInventory>(ref buffer, ref offset, "bSavedIsBurningFuel", "SavedFuelTimer", "SavedCurrentBurnDuration");
			Serializer.SchemaSerialize(_boolFormatter, ref buffer, ref offset, value.bSavedIsBurningFuel);
			Serializer.SchemaSerialize(_floatFormatter, ref buffer, ref offset, value.SavedFuelTimer);
			Serializer.SchemaSerialize(_floatFormatter, ref buffer, ref offset, value.SavedCurrentBurnDuration);
		}

		public void Deserialize(byte[] buffer, ref int offset, ref HRFuelInventory value)
		{
			Serializer.DeserializeSchema<HRFuelInventory>(ref SchemaDeserializer, buffer, ref offset, "bSavedIsBurningFuel", "SavedFuelTimer", "SavedCurrentBurnDuration");
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _boolFormatter, buffer, ref offset, ref value.bSavedIsBurningFuel);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _floatFormatter, buffer, ref offset, ref value.SavedFuelTimer);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _floatFormatter, buffer, ref offset, ref value.SavedCurrentBurnDuration);
		}
	}

	internal class HRInventoryFormatter : IFormatter<HRInventory>
	{
		CerasSerializer Serializer;
		CerasSerializer.SchemaDeserializer SchemaDeserializer;
		IFormatter<bool> _boolFormatter;
		IFormatter<System.String[]> _stringArrayFormatter;
		IFormatter<System.Int32[]> _int32ArrayFormatter;
		IFormatter<int> _intFormatter;

		public void Serialize(ref byte[] buffer, ref int offset, HRInventory value)
		{
			Serializer.SerializeSchema<HRInventory>(ref buffer, ref offset, "bContentsBeenRandomized", "SavedItemIDs", "SavedItemIDSlotTracker", "SavedMaxSlot");
			Serializer.SchemaSerialize(_boolFormatter, ref buffer, ref offset, value.bContentsBeenRandomized);
			Serializer.SchemaSerialize(_stringArrayFormatter, ref buffer, ref offset, value.SavedItemIDs);
			Serializer.SchemaSerialize(_int32ArrayFormatter, ref buffer, ref offset, value.SavedItemIDSlotTracker);
			Serializer.SchemaSerialize(_intFormatter, ref buffer, ref offset, value.SavedMaxSlot);
		}

		public void Deserialize(byte[] buffer, ref int offset, ref HRInventory value)
		{
			Serializer.DeserializeSchema<HRInventory>(ref SchemaDeserializer, buffer, ref offset, "bContentsBeenRandomized", "SavedItemIDs", "SavedItemIDSlotTracker", "SavedMaxSlot");
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _boolFormatter, buffer, ref offset, ref value.bContentsBeenRandomized);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _stringArrayFormatter, buffer, ref offset, ref value.SavedItemIDs);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _int32ArrayFormatter, buffer, ref offset, ref value.SavedItemIDSlotTracker);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _intFormatter, buffer, ref offset, ref value.SavedMaxSlot);
		}
	}

	internal class HRGolfManagerFormatter : IFormatter<HRGolfManager>
	{
		CerasSerializer Serializer;
		CerasSerializer.SchemaDeserializer SchemaDeserializer;
		IFormatter<bool> _boolFormatter;

		public void Serialize(ref byte[] buffer, ref int offset, HRGolfManager value)
		{
			Serializer.SerializeSchema<HRGolfManager>(ref buffer, ref offset, "bGameOver");
			Serializer.SchemaSerialize(_boolFormatter, ref buffer, ref offset, value.bGameOver);
		}

		public void Deserialize(byte[] buffer, ref int offset, ref HRGolfManager value)
		{
			Serializer.DeserializeSchema<HRGolfManager>(ref SchemaDeserializer, buffer, ref offset, "bGameOver");
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _boolFormatter, buffer, ref offset, ref value.bGameOver);
		}
	}

	internal class HRTypingInputMinigameComponentFormatter : IFormatter<HRTypingInputMinigameComponent>
	{
		CerasSerializer Serializer;
		CerasSerializer.SchemaDeserializer SchemaDeserializer;
		IFormatter<string> _stringFormatter;

		public void Serialize(ref byte[] buffer, ref int offset, HRTypingInputMinigameComponent value)
		{
			Serializer.SerializeSchema<HRTypingInputMinigameComponent>(ref buffer, ref offset, "SavedText");
			Serializer.SchemaSerialize(_stringFormatter, ref buffer, ref offset, value.SavedText);
		}

		public void Deserialize(byte[] buffer, ref int offset, ref HRTypingInputMinigameComponent value)
		{
			Serializer.DeserializeSchema<HRTypingInputMinigameComponent>(ref SchemaDeserializer, buffer, ref offset, "SavedText");
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _stringFormatter, buffer, ref offset, ref value.SavedText);
		}
	}

	internal class HRNeedManagerFormatter : IFormatter<HRNeedManager>
	{
		CerasSerializer Serializer;
		CerasSerializer.SchemaDeserializer SchemaDeserializer;
		IFormatter<System.Collections.Generic.List<NeedDataToSave>> _list_NeedDataToSaveFormatter;

		public void Serialize(ref byte[] buffer, ref int offset, HRNeedManager value)
		{
			Serializer.SerializeSchema<HRNeedManager>(ref buffer, ref offset, "SavedData");
			Serializer.SchemaSerialize(_list_NeedDataToSaveFormatter, ref buffer, ref offset, value.SavedData);
		}

		public void Deserialize(byte[] buffer, ref int offset, ref HRNeedManager value)
		{
			Serializer.DeserializeSchema<HRNeedManager>(ref SchemaDeserializer, buffer, ref offset, "SavedData");
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _list_NeedDataToSaveFormatter, buffer, ref offset, ref value.SavedData);
		}
	}

	internal class NeedDataToSaveFormatter : IFormatter<NeedDataToSave>
	{
		CerasSerializer Serializer;
		CerasSerializer.SchemaDeserializer SchemaDeserializer;
		IFormatter<bool> _boolFormatter;
		IFormatter<float> _floatFormatter;

		public void Serialize(ref byte[] buffer, ref int offset, NeedDataToSave value)
		{
			Serializer.SerializeSchema<NeedDataToSave>(ref buffer, ref offset, "IsActive", "BurnRate", "CurrentHP");
			Serializer.SchemaSerialize(_boolFormatter, ref buffer, ref offset, value.IsActive);
			Serializer.SchemaSerialize(_floatFormatter, ref buffer, ref offset, value.BurnRate);
			Serializer.SchemaSerialize(_floatFormatter, ref buffer, ref offset, value.CurrentHP);
		}

		public void Deserialize(byte[] buffer, ref int offset, ref NeedDataToSave value)
		{
			Serializer.DeserializeSchema<NeedDataToSave>(ref SchemaDeserializer, buffer, ref offset, "IsActive", "BurnRate", "CurrentHP");
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _boolFormatter, buffer, ref offset, ref value.IsActive);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _floatFormatter, buffer, ref offset, ref value.BurnRate);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _floatFormatter, buffer, ref offset, ref value.CurrentHP);
		}
	}

	internal class HRNeedRuntimeDataFormatter : IFormatter<HRNeedRuntimeData>
	{
		CerasSerializer Serializer;
		CerasSerializer.SchemaDeserializer SchemaDeserializer;
		IFormatter<bool> _boolFormatter;
		IFormatter<float> _floatFormatter;

		public void Serialize(ref byte[] buffer, ref int offset, HRNeedRuntimeData value)
		{
			Serializer.SerializeSchema<HRNeedRuntimeData>(ref buffer, ref offset, "bActive", "BurnRate");
			Serializer.SchemaSerialize(_boolFormatter, ref buffer, ref offset, value.bActive);
			Serializer.SchemaSerialize(_floatFormatter, ref buffer, ref offset, value.BurnRate);
		}

		public void Deserialize(byte[] buffer, ref int offset, ref HRNeedRuntimeData value)
		{
			Serializer.DeserializeSchema<HRNeedRuntimeData>(ref SchemaDeserializer, buffer, ref offset, "bActive", "BurnRate");
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _boolFormatter, buffer, ref offset, ref value.bActive);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _floatFormatter, buffer, ref offset, ref value.BurnRate);
		}
	}

	internal class HRStaminaComponentFormatter : IFormatter<HRStaminaComponent>
	{
		CerasSerializer Serializer;
		CerasSerializer.SchemaDeserializer SchemaDeserializer;
		IFormatter<float> _floatFormatter;

		public void Serialize(ref byte[] buffer, ref int offset, HRStaminaComponent value)
		{
			Serializer.SerializeSchema<HRStaminaComponent>(ref buffer, ref offset, "CurrentHP");
			Serializer.SchemaSerialize(_floatFormatter, ref buffer, ref offset, value.CurrentHP);
		}

		public void Deserialize(byte[] buffer, ref int offset, ref HRStaminaComponent value)
		{
			Serializer.DeserializeSchema<HRStaminaComponent>(ref SchemaDeserializer, buffer, ref offset, "CurrentHP");
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _floatFormatter, buffer, ref offset, ref value.CurrentHP);
		}
	}

	internal class HRXPComponentFormatter : IFormatter<HRXPComponent>
	{
		CerasSerializer Serializer;
		CerasSerializer.SchemaDeserializer SchemaDeserializer;
		IFormatter<int> _intFormatter;
		IFormatter<float> _floatFormatter;

		public void Serialize(ref byte[] buffer, ref int offset, HRXPComponent value)
		{
			Serializer.SerializeSchema<HRXPComponent>(ref buffer, ref offset, "SavedCurrentXPLevel", "CurrentHP");
			Serializer.SchemaSerialize(_intFormatter, ref buffer, ref offset, value.SavedCurrentXPLevel);
			Serializer.SchemaSerialize(_floatFormatter, ref buffer, ref offset, value.CurrentHP);
		}

		public void Deserialize(byte[] buffer, ref int offset, ref HRXPComponent value)
		{
			Serializer.DeserializeSchema<HRXPComponent>(ref SchemaDeserializer, buffer, ref offset, "SavedCurrentXPLevel", "CurrentHP");
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _intFormatter, buffer, ref offset, ref value.SavedCurrentXPLevel);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _floatFormatter, buffer, ref offset, ref value.CurrentHP);
		}
	}

	internal class HRQuestTriggerFormatter : IFormatter<HRQuestTrigger>
	{
		CerasSerializer Serializer;
		CerasSerializer.SchemaDeserializer SchemaDeserializer;
		IFormatter<bool> _boolFormatter;

		public void Serialize(ref byte[] buffer, ref int offset, HRQuestTrigger value)
		{
			Serializer.SerializeSchema<HRQuestTrigger>(ref buffer, ref offset, "bTriggered");
			Serializer.SchemaSerialize(_boolFormatter, ref buffer, ref offset, value.bTriggered);
		}

		public void Deserialize(byte[] buffer, ref int offset, ref HRQuestTrigger value)
		{
			Serializer.DeserializeSchema<HRQuestTrigger>(ref SchemaDeserializer, buffer, ref offset, "bTriggered");
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _boolFormatter, buffer, ref offset, ref value.bTriggered);
		}
	}

	internal class HRSaveComponentFormatter : IFormatter<HRSaveComponent>
	{
		CerasSerializer Serializer;
		CerasSerializer.SchemaDeserializer SchemaDeserializer;
		IFormatter<UnityEngine.Vector3> _vector3Formatter;
		IFormatter<UnityEngine.Quaternion> _quaternionFormatter;
		IFormatter<bool> _boolFormatter;
		IFormatter<string> _stringFormatter;

		public void Serialize(ref byte[] buffer, ref int offset, HRSaveComponent value)
		{
			Serializer.SerializeSchema<HRSaveComponent>(ref buffer, ref offset, "SavedPosition", "SavedRotation", "SavedActive", "WasDestroyed", "WasTaken", "bLoadTransform", "SavedMapName", "bHasSaveData", "bSaved");
			Serializer.SchemaSerialize(_vector3Formatter, ref buffer, ref offset, value.SavedPosition);
			Serializer.SchemaSerialize(_quaternionFormatter, ref buffer, ref offset, value.SavedRotation);
			Serializer.SchemaSerialize(_boolFormatter, ref buffer, ref offset, value.SavedActive);
			Serializer.SchemaSerialize(_boolFormatter, ref buffer, ref offset, value.WasDestroyed);
			Serializer.SchemaSerialize(_boolFormatter, ref buffer, ref offset, value.WasTaken);
			Serializer.SchemaSerialize(_boolFormatter, ref buffer, ref offset, value.bLoadTransform);
			Serializer.SchemaSerialize(_stringFormatter, ref buffer, ref offset, value.SavedMapName);
			Serializer.SchemaSerialize(_boolFormatter, ref buffer, ref offset, value.bHasSaveData);
			Serializer.SchemaSerialize(_boolFormatter, ref buffer, ref offset, value.bSaved);
		}

		public void Deserialize(byte[] buffer, ref int offset, ref HRSaveComponent value)
		{
			Serializer.DeserializeSchema<HRSaveComponent>(ref SchemaDeserializer, buffer, ref offset, "SavedPosition", "SavedRotation", "SavedActive", "WasDestroyed", "WasTaken", "bLoadTransform", "SavedMapName", "bHasSaveData", "bSaved");
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _vector3Formatter, buffer, ref offset, ref value.SavedPosition);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _quaternionFormatter, buffer, ref offset, ref value.SavedRotation);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _boolFormatter, buffer, ref offset, ref value.SavedActive);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _boolFormatter, buffer, ref offset, ref value.WasDestroyed);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _boolFormatter, buffer, ref offset, ref value.WasTaken);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _boolFormatter, buffer, ref offset, ref value.bLoadTransform);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _stringFormatter, buffer, ref offset, ref value.SavedMapName);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _boolFormatter, buffer, ref offset, ref value.bHasSaveData);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _boolFormatter, buffer, ref offset, ref value.bSaved);
		}
	}

	internal class HRSaveSystemFormatter : IFormatter<HRSaveSystem>
	{
		CerasSerializer Serializer;
		CerasSerializer.SchemaDeserializer SchemaDeserializer;
		IFormatter<bool> _boolFormatter;

		public void Serialize(ref byte[] buffer, ref int offset, HRSaveSystem value)
		{
			Serializer.SerializeSchema<HRSaveSystem>(ref buffer, ref offset, "bSavePersistentPlayerData", "bSaveLevelData", "bSaveDialogueData", "bSaveSLinkData");
			Serializer.SchemaSerialize(_boolFormatter, ref buffer, ref offset, value.bSavePersistentPlayerData);
			Serializer.SchemaSerialize(_boolFormatter, ref buffer, ref offset, value.bSaveLevelData);
			Serializer.SchemaSerialize(_boolFormatter, ref buffer, ref offset, value.bSaveDialogueData);
			Serializer.SchemaSerialize(_boolFormatter, ref buffer, ref offset, value.bSaveSLinkData);
		}

		public void Deserialize(byte[] buffer, ref int offset, ref HRSaveSystem value)
		{
			Serializer.DeserializeSchema<HRSaveSystem>(ref SchemaDeserializer, buffer, ref offset, "bSavePersistentPlayerData", "bSaveLevelData", "bSaveDialogueData", "bSaveSLinkData");
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _boolFormatter, buffer, ref offset, ref value.bSavePersistentPlayerData);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _boolFormatter, buffer, ref offset, ref value.bSaveLevelData);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _boolFormatter, buffer, ref offset, ref value.bSaveDialogueData);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _boolFormatter, buffer, ref offset, ref value.bSaveSLinkData);
		}
	}

	internal class HRShopSpecialtiesManagerFormatter : IFormatter<HRShopSpecialtiesManager>
	{
		CerasSerializer Serializer;
		CerasSerializer.SchemaDeserializer SchemaDeserializer;
		IFormatter<System.Collections.Generic.List<int>> _list_intFormatter;
		IFormatter<System.Collections.Generic.List<string>> _list_stringFormatter;

		public void Serialize(ref byte[] buffer, ref int offset, HRShopSpecialtiesManager value)
		{
			Serializer.SerializeSchema<HRShopSpecialtiesManager>(ref buffer, ref offset, "SavedSpecialtyItemIds", "SavedSpecialtyCategories");
			Serializer.SchemaSerialize(_list_intFormatter, ref buffer, ref offset, value.SavedSpecialtyItemIds);
			Serializer.SchemaSerialize(_list_stringFormatter, ref buffer, ref offset, value.SavedSpecialtyCategories);
		}

		public void Deserialize(byte[] buffer, ref int offset, ref HRShopSpecialtiesManager value)
		{
			Serializer.DeserializeSchema<HRShopSpecialtiesManager>(ref SchemaDeserializer, buffer, ref offset, "SavedSpecialtyItemIds", "SavedSpecialtyCategories");
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _list_intFormatter, buffer, ref offset, ref value.SavedSpecialtyItemIds);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _list_stringFormatter, buffer, ref offset, ref value.SavedSpecialtyCategories);
		}
	}

	internal class HRGlobalPlayerSkillTreeInstanceFormatter : IFormatter<HRGlobalPlayerSkillTreeInstance>
	{
		CerasSerializer Serializer;
		CerasSerializer.SchemaDeserializer SchemaDeserializer;
		IFormatter<int> _intFormatter;
		IFormatter<float> _floatFormatter;
		IFormatter<System.Collections.Generic.List<HRSkillTreeInstance.SkillUnlockData>> _list_SkillUnlockDataFormatter;

		public void Serialize(ref byte[] buffer, ref int offset, HRGlobalPlayerSkillTreeInstance value)
		{
			Serializer.SerializeSchema<HRGlobalPlayerSkillTreeInstance>(ref buffer, ref offset, "MaxSkillPoints", "CurrentCooldownTime", "UnlockedSkills");
			Serializer.SchemaSerialize(_intFormatter, ref buffer, ref offset, value.MaxSkillPoints);
			Serializer.SchemaSerialize(_floatFormatter, ref buffer, ref offset, value.CurrentCooldownTime);
			Serializer.SchemaSerialize(_list_SkillUnlockDataFormatter, ref buffer, ref offset, value.UnlockedSkills);
		}

		public void Deserialize(byte[] buffer, ref int offset, ref HRGlobalPlayerSkillTreeInstance value)
		{
			Serializer.DeserializeSchema<HRGlobalPlayerSkillTreeInstance>(ref SchemaDeserializer, buffer, ref offset, "MaxSkillPoints", "CurrentCooldownTime", "UnlockedSkills");
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _intFormatter, buffer, ref offset, ref value.MaxSkillPoints);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _floatFormatter, buffer, ref offset, ref value.CurrentCooldownTime);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _list_SkillUnlockDataFormatter, buffer, ref offset, ref value.UnlockedSkills);
		}
	}

	internal class SkillUnlockDataFormatter : IFormatter<HRSkillTreeInstance.SkillUnlockData>
	{
		CerasSerializer Serializer;
		CerasSerializer.SchemaDeserializer SchemaDeserializer;
		IFormatter<bool> _boolFormatter;
		IFormatter<int> _intFormatter;
		IFormatter<float> _floatFormatter;
		IFormatter<string> _stringFormatter;

		public void Serialize(ref byte[] buffer, ref int offset, HRSkillTreeInstance.SkillUnlockData value)
		{
			Serializer.SerializeSchema<HRSkillTreeInstance.SkillUnlockData>(ref buffer, ref offset, "bActive", "bIgnored", "bUnlockedForPurchase", "bUnlocked", "Level", "NodeIndex", "RootID", "RowIndex", "ShelfIndex", "SkillID", "Progress", "CurrentOwner");
			Serializer.SchemaSerialize(_boolFormatter, ref buffer, ref offset, value.bActive);
			Serializer.SchemaSerialize(_boolFormatter, ref buffer, ref offset, value.bIgnored);
			Serializer.SchemaSerialize(_boolFormatter, ref buffer, ref offset, value.bUnlockedForPurchase);
			Serializer.SchemaSerialize(_boolFormatter, ref buffer, ref offset, value.bUnlocked);
			Serializer.SchemaSerialize(_intFormatter, ref buffer, ref offset, value.Level);
			Serializer.SchemaSerialize(_intFormatter, ref buffer, ref offset, value.NodeIndex);
			Serializer.SchemaSerialize(_intFormatter, ref buffer, ref offset, value.RootID);
			Serializer.SchemaSerialize(_intFormatter, ref buffer, ref offset, value.RowIndex);
			Serializer.SchemaSerialize(_intFormatter, ref buffer, ref offset, value.ShelfIndex);
			Serializer.SchemaSerialize(_intFormatter, ref buffer, ref offset, value.SkillID);
			Serializer.SchemaSerialize(_floatFormatter, ref buffer, ref offset, value.Progress);
			Serializer.SchemaSerialize(_stringFormatter, ref buffer, ref offset, value.CurrentOwner);
		}

		public void Deserialize(byte[] buffer, ref int offset, ref HRSkillTreeInstance.SkillUnlockData value)
		{
			Serializer.DeserializeSchema<HRSkillTreeInstance.SkillUnlockData>(ref SchemaDeserializer, buffer, ref offset, "bActive", "bIgnored", "bUnlockedForPurchase", "bUnlocked", "Level", "NodeIndex", "RootID", "RowIndex", "ShelfIndex", "SkillID", "Progress", "CurrentOwner");
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _boolFormatter, buffer, ref offset, ref value.bActive);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _boolFormatter, buffer, ref offset, ref value.bIgnored);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _boolFormatter, buffer, ref offset, ref value.bUnlockedForPurchase);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _boolFormatter, buffer, ref offset, ref value.bUnlocked);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _intFormatter, buffer, ref offset, ref value.Level);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _intFormatter, buffer, ref offset, ref value.NodeIndex);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _intFormatter, buffer, ref offset, ref value.RootID);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _intFormatter, buffer, ref offset, ref value.RowIndex);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _intFormatter, buffer, ref offset, ref value.ShelfIndex);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _intFormatter, buffer, ref offset, ref value.SkillID);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _floatFormatter, buffer, ref offset, ref value.Progress);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _stringFormatter, buffer, ref offset, ref value.CurrentOwner);
		}
	}

	internal class HRSkillTreeInstanceFormatter : IFormatter<HRSkillTreeInstance>
	{
		CerasSerializer Serializer;
		CerasSerializer.SchemaDeserializer SchemaDeserializer;
		IFormatter<int> _intFormatter;
		IFormatter<float> _floatFormatter;
		IFormatter<System.Collections.Generic.List<HRSkillTreeInstance.SkillUnlockData>> _list_SkillUnlockDataFormatter;

		public void Serialize(ref byte[] buffer, ref int offset, HRSkillTreeInstance value)
		{
			Serializer.SerializeSchema<HRSkillTreeInstance>(ref buffer, ref offset, "MaxSkillPoints", "CurrentCooldownTime", "UnlockedSkills");
			Serializer.SchemaSerialize(_intFormatter, ref buffer, ref offset, value.MaxSkillPoints);
			Serializer.SchemaSerialize(_floatFormatter, ref buffer, ref offset, value.CurrentCooldownTime);
			Serializer.SchemaSerialize(_list_SkillUnlockDataFormatter, ref buffer, ref offset, value.UnlockedSkills);
		}

		public void Deserialize(byte[] buffer, ref int offset, ref HRSkillTreeInstance value)
		{
			Serializer.DeserializeSchema<HRSkillTreeInstance>(ref SchemaDeserializer, buffer, ref offset, "MaxSkillPoints", "CurrentCooldownTime", "UnlockedSkills");
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _intFormatter, buffer, ref offset, ref value.MaxSkillPoints);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _floatFormatter, buffer, ref offset, ref value.CurrentCooldownTime);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _list_SkillUnlockDataFormatter, buffer, ref offset, ref value.UnlockedSkills);
		}
	}

	internal class HRPlayerCustomizationUIFormatter : IFormatter<HRPlayerCustomizationUI>
	{
		CerasSerializer Serializer;
		CerasSerializer.SchemaDeserializer SchemaDeserializer;
		IFormatter<int> _intFormatter;
		IFormatter<System.Int32[]> _int32ArrayFormatter;

		public void Serialize(ref byte[] buffer, ref int offset, HRPlayerCustomizationUI value)
		{
			Serializer.SerializeSchema<HRPlayerCustomizationUI>(ref buffer, ref offset, "SavedPlayerBodyType", "SavedPlayerSkinColorIndex", "SavedPlayerEquippedItemIDs", "SavedPlayerEquippedMaterialIDs");
			Serializer.SchemaSerialize(_intFormatter, ref buffer, ref offset, value.SavedPlayerBodyType);
			Serializer.SchemaSerialize(_intFormatter, ref buffer, ref offset, value.SavedPlayerSkinColorIndex);
			Serializer.SchemaSerialize(_int32ArrayFormatter, ref buffer, ref offset, value.SavedPlayerEquippedItemIDs);
			Serializer.SchemaSerialize(_int32ArrayFormatter, ref buffer, ref offset, value.SavedPlayerEquippedMaterialIDs);
		}

		public void Deserialize(byte[] buffer, ref int offset, ref HRPlayerCustomizationUI value)
		{
			Serializer.DeserializeSchema<HRPlayerCustomizationUI>(ref SchemaDeserializer, buffer, ref offset, "SavedPlayerBodyType", "SavedPlayerSkinColorIndex", "SavedPlayerEquippedItemIDs", "SavedPlayerEquippedMaterialIDs");
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _intFormatter, buffer, ref offset, ref value.SavedPlayerBodyType);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _intFormatter, buffer, ref offset, ref value.SavedPlayerSkinColorIndex);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _int32ArrayFormatter, buffer, ref offset, ref value.SavedPlayerEquippedItemIDs);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _int32ArrayFormatter, buffer, ref offset, ref value.SavedPlayerEquippedMaterialIDs);
		}
	}

	internal class HRPlayerTotemFormatter : IFormatter<HRPlayerTotem>
	{
		CerasSerializer Serializer;
		CerasSerializer.SchemaDeserializer SchemaDeserializer;
		IFormatter<string> _stringFormatter;

		public void Serialize(ref byte[] buffer, ref int offset, HRPlayerTotem value)
		{
			Serializer.SerializeSchema<HRPlayerTotem>(ref buffer, ref offset, "savedTotemId");
			Serializer.SchemaSerialize(_stringFormatter, ref buffer, ref offset, value.savedTotemId);
		}

		public void Deserialize(byte[] buffer, ref int offset, ref HRPlayerTotem value)
		{
			Serializer.DeserializeSchema<HRPlayerTotem>(ref SchemaDeserializer, buffer, ref offset, "savedTotemId");
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _stringFormatter, buffer, ref offset, ref value.savedTotemId);
		}
	}

	internal class HRWorldMapTotemManagerFormatter : IFormatter<HRWorldMapTotemManager>
	{
		CerasSerializer Serializer;
		CerasSerializer.SchemaDeserializer SchemaDeserializer;
		IFormatter<MapTotemSaveData[]> _mapTotemSaveDataArrayFormatter;
		IFormatter<PlayerTotemSaveData[]> _playerTotemSaveDataArrayFormatter;
		IFormatter<PlayerLastUsedTotemSaveData[]> _playerLastUsedTotemSaveDataArrayFormatter;

		public void Serialize(ref byte[] buffer, ref int offset, HRWorldMapTotemManager value)
		{
			Serializer.SerializeSchema<HRWorldMapTotemManager>(ref buffer, ref offset, "mapTotemSaveData", "playerTotemSaveData", "playerLastUsedTotemSaveData");
			Serializer.SchemaSerialize(_mapTotemSaveDataArrayFormatter, ref buffer, ref offset, value.mapTotemSaveData);
			Serializer.SchemaSerialize(_playerTotemSaveDataArrayFormatter, ref buffer, ref offset, value.playerTotemSaveData);
			Serializer.SchemaSerialize(_playerLastUsedTotemSaveDataArrayFormatter, ref buffer, ref offset, value.playerLastUsedTotemSaveData);
		}

		public void Deserialize(byte[] buffer, ref int offset, ref HRWorldMapTotemManager value)
		{
			Serializer.DeserializeSchema<HRWorldMapTotemManager>(ref SchemaDeserializer, buffer, ref offset, "mapTotemSaveData", "playerTotemSaveData", "playerLastUsedTotemSaveData");
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _mapTotemSaveDataArrayFormatter, buffer, ref offset, ref value.mapTotemSaveData);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _playerTotemSaveDataArrayFormatter, buffer, ref offset, ref value.playerTotemSaveData);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _playerLastUsedTotemSaveDataArrayFormatter, buffer, ref offset, ref value.playerLastUsedTotemSaveData);
		}
	}

	internal class MapTotemSaveDataFormatter : IFormatter<MapTotemSaveData>
	{
		CerasSerializer Serializer;
		CerasSerializer.SchemaDeserializer SchemaDeserializer;
		IFormatter<bool> _boolFormatter;
		IFormatter<string> _stringFormatter;

		public void Serialize(ref byte[] buffer, ref int offset, MapTotemSaveData value)
		{
			Serializer.SerializeSchema<MapTotemSaveData>(ref buffer, ref offset, "active", "id");
			Serializer.SchemaSerialize(_boolFormatter, ref buffer, ref offset, value.active);
			Serializer.SchemaSerialize(_stringFormatter, ref buffer, ref offset, value.id);
		}

		public void Deserialize(byte[] buffer, ref int offset, ref MapTotemSaveData value)
		{
			Serializer.DeserializeSchema<MapTotemSaveData>(ref SchemaDeserializer, buffer, ref offset, "active", "id");
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _boolFormatter, buffer, ref offset, ref value.active);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _stringFormatter, buffer, ref offset, ref value.id);
		}
	}

	internal class PlayerTotemSaveDataFormatter : IFormatter<PlayerTotemSaveData>
	{
		CerasSerializer Serializer;
		CerasSerializer.SchemaDeserializer SchemaDeserializer;
		IFormatter<bool> _boolFormatter;
		IFormatter<string> _stringFormatter;
		IFormatter<UnityEngine.Vector3> _vector3Formatter;

		public void Serialize(ref byte[] buffer, ref int offset, PlayerTotemSaveData value)
		{
			Serializer.SerializeSchema<PlayerTotemSaveData>(ref buffer, ref offset, "active", "id", "name", "position");
			Serializer.SchemaSerialize(_boolFormatter, ref buffer, ref offset, value.active);
			Serializer.SchemaSerialize(_stringFormatter, ref buffer, ref offset, value.id);
			Serializer.SchemaSerialize(_stringFormatter, ref buffer, ref offset, value.name);
			Serializer.SchemaSerialize(_vector3Formatter, ref buffer, ref offset, value.position);
		}

		public void Deserialize(byte[] buffer, ref int offset, ref PlayerTotemSaveData value)
		{
			Serializer.DeserializeSchema<PlayerTotemSaveData>(ref SchemaDeserializer, buffer, ref offset, "active", "id", "name", "position");
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _boolFormatter, buffer, ref offset, ref value.active);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _stringFormatter, buffer, ref offset, ref value.id);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _stringFormatter, buffer, ref offset, ref value.name);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _vector3Formatter, buffer, ref offset, ref value.position);
		}
	}

	internal class PlayerLastUsedTotemSaveDataFormatter : IFormatter<PlayerLastUsedTotemSaveData>
	{
		CerasSerializer Serializer;
		CerasSerializer.SchemaDeserializer SchemaDeserializer;
		IFormatter<string> _stringFormatter;

		public void Serialize(ref byte[] buffer, ref int offset, PlayerLastUsedTotemSaveData value)
		{
			Serializer.SerializeSchema<PlayerLastUsedTotemSaveData>(ref buffer, ref offset, "steamId", "totemId");
			Serializer.SchemaSerialize(_stringFormatter, ref buffer, ref offset, value.steamId);
			Serializer.SchemaSerialize(_stringFormatter, ref buffer, ref offset, value.totemId);
		}

		public void Deserialize(byte[] buffer, ref int offset, ref PlayerLastUsedTotemSaveData value)
		{
			Serializer.DeserializeSchema<PlayerLastUsedTotemSaveData>(ref SchemaDeserializer, buffer, ref offset, "steamId", "totemId");
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _stringFormatter, buffer, ref offset, ref value.steamId);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _stringFormatter, buffer, ref offset, ref value.totemId);
		}
	}

	internal class HRWorldEnemySpawnerFormatter : IFormatter<HRWorldEnemySpawner>
	{
		CerasSerializer Serializer;
		CerasSerializer.SchemaDeserializer SchemaDeserializer;

		public void Serialize(ref byte[] buffer, ref int offset, HRWorldEnemySpawner value)
		{
		}

		public void Deserialize(byte[] buffer, ref int offset, ref HRWorldEnemySpawner value)
		{
		}
	}

	internal class BaseNObjectDataFormatter : IFormatter<BaseNObjectData>
	{
		CerasSerializer Serializer;
		CerasSerializer.SchemaDeserializer SchemaDeserializer;
		IFormatter<BaseNObjectMemberData[]> _baseNObjectMemberDataArrayFormatter;
		IFormatter<int> _intFormatter;

		public void Serialize(ref byte[] buffer, ref int offset, BaseNObjectData value)
		{
			Serializer.SerializeSchema<BaseNObjectData>(ref buffer, ref offset, "data", "versionNumber");
			Serializer.SchemaSerialize(_baseNObjectMemberDataArrayFormatter, ref buffer, ref offset, value.data);
			Serializer.SchemaSerialize(_intFormatter, ref buffer, ref offset, value.versionNumber);
		}

		public void Deserialize(byte[] buffer, ref int offset, ref BaseNObjectData value)
		{
			Serializer.DeserializeSchema<BaseNObjectData>(ref SchemaDeserializer, buffer, ref offset, "data", "versionNumber");
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _baseNObjectMemberDataArrayFormatter, buffer, ref offset, ref value.data);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _intFormatter, buffer, ref offset, ref value.versionNumber);
		}
	}

	internal class BaseNObjectMemberDataFormatter : IFormatter<BaseNObjectMemberData>
	{
		CerasSerializer Serializer;
		CerasSerializer.SchemaDeserializer SchemaDeserializer;
		IFormatter<int> _intFormatter;
		IFormatter<float> _floatFormatter;
		IFormatter<string> _stringFormatter;

		public void Serialize(ref byte[] buffer, ref int offset, BaseNObjectMemberData value)
		{
			Serializer.SerializeSchema<BaseNObjectMemberData>(ref buffer, ref offset, "memberID", "HP", "matrixData", "objectName");
			Serializer.SchemaSerialize(_intFormatter, ref buffer, ref offset, value.memberID);
			Serializer.SchemaSerialize(_floatFormatter, ref buffer, ref offset, value.HP);
			Serializer.SchemaSerialize(_stringFormatter, ref buffer, ref offset, value.matrixData);
			Serializer.SchemaSerialize(_stringFormatter, ref buffer, ref offset, value.objectName);
		}

		public void Deserialize(byte[] buffer, ref int offset, ref BaseNObjectMemberData value)
		{
			Serializer.DeserializeSchema<BaseNObjectMemberData>(ref SchemaDeserializer, buffer, ref offset, "memberID", "HP", "matrixData", "objectName");
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _intFormatter, buffer, ref offset, ref value.memberID);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _floatFormatter, buffer, ref offset, ref value.HP);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _stringFormatter, buffer, ref offset, ref value.matrixData);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _stringFormatter, buffer, ref offset, ref value.objectName);
		}
	}

	internal class CollectableDataFormatter : IFormatter<HRCollectableManager.CollectableData>
	{
		CerasSerializer Serializer;
		CerasSerializer.SchemaDeserializer SchemaDeserializer;
		IFormatter<System.Collections.Generic.List<bool>> _list_boolFormatter;
		IFormatter<int> _intFormatter;
		IFormatter<string> _stringFormatter;

		public void Serialize(ref byte[] buffer, ref int offset, HRCollectableManager.CollectableData value)
		{
			Serializer.SerializeSchema<HRCollectableManager.CollectableData>(ref buffer, ref offset, "CollectedList", "NumCollected", "CollectableID");
			Serializer.SchemaSerialize(_list_boolFormatter, ref buffer, ref offset, value.CollectedList);
			Serializer.SchemaSerialize(_intFormatter, ref buffer, ref offset, value.NumCollected);
			Serializer.SchemaSerialize(_stringFormatter, ref buffer, ref offset, value.CollectableID);
		}

		public void Deserialize(byte[] buffer, ref int offset, ref HRCollectableManager.CollectableData value)
		{
			Serializer.DeserializeSchema<HRCollectableManager.CollectableData>(ref SchemaDeserializer, buffer, ref offset, "CollectedList", "NumCollected", "CollectableID");
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _list_boolFormatter, buffer, ref offset, ref value.CollectedList);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _intFormatter, buffer, ref offset, ref value.NumCollected);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _stringFormatter, buffer, ref offset, ref value.CollectableID);
		}
	}

	internal class TransformDataFormatter : IFormatter<TransformData>
	{
		CerasSerializer Serializer;
		CerasSerializer.SchemaDeserializer SchemaDeserializer;
		IFormatter<string> _stringFormatter;
		IFormatter<UnityEngine.Quaternion> _quaternionFormatter;
		IFormatter<UnityEngine.Vector3> _vector3Formatter;

		public void Serialize(ref byte[] buffer, ref int offset, TransformData value)
		{
			Serializer.SerializeSchema<TransformData>(ref buffer, ref offset, "MasterMap", "rotation", "position");
			Serializer.SchemaSerialize(_stringFormatter, ref buffer, ref offset, value.MasterMap);
			Serializer.SchemaSerialize(_quaternionFormatter, ref buffer, ref offset, value.rotation);
			Serializer.SchemaSerialize(_vector3Formatter, ref buffer, ref offset, value.position);
		}

		public void Deserialize(byte[] buffer, ref int offset, ref TransformData value)
		{
			Serializer.DeserializeSchema<TransformData>(ref SchemaDeserializer, buffer, ref offset, "MasterMap", "rotation", "position");
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _stringFormatter, buffer, ref offset, ref value.MasterMap);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _quaternionFormatter, buffer, ref offset, ref value.rotation);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _vector3Formatter, buffer, ref offset, ref value.position);
		}
	}

	internal class SavedSaleInfoFormatter : IFormatter<HRBarteringSystem.SavedSaleInfo>
	{
		CerasSerializer Serializer;
		CerasSerializer.SchemaDeserializer SchemaDeserializer;
		IFormatter<System.Int32[]> _int32ArrayFormatter;

		public void Serialize(ref byte[] buffer, ref int offset, HRBarteringSystem.SavedSaleInfo value)
		{
			Serializer.SerializeSchema<HRBarteringSystem.SavedSaleInfo>(ref buffer, ref offset, "LastSaleByRarity");
			Serializer.SchemaSerialize(_int32ArrayFormatter, ref buffer, ref offset, value.LastSaleByRarity);
		}

		public void Deserialize(byte[] buffer, ref int offset, ref HRBarteringSystem.SavedSaleInfo value)
		{
			Serializer.DeserializeSchema<HRBarteringSystem.SavedSaleInfo>(ref SchemaDeserializer, buffer, ref offset, "LastSaleByRarity");
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _int32ArrayFormatter, buffer, ref offset, ref value.LastSaleByRarity);
		}
	}

	internal class EncyclopediaSaveDataFormatter : IFormatter<HREncyclopediaSystem.EncyclopediaSaveData>
	{
		CerasSerializer Serializer;
		CerasSerializer.SchemaDeserializer SchemaDeserializer;
		IFormatter<bool> _boolFormatter;
		IFormatter<int> _intFormatter;

		public void Serialize(ref byte[] buffer, ref int offset, HREncyclopediaSystem.EncyclopediaSaveData value)
		{
			Serializer.SerializeSchema<HREncyclopediaSystem.EncyclopediaSaveData>(ref buffer, ref offset, "bDisplayed", "bNew", "bShownFirst", "bUnlocked", "ItemID");
			Serializer.SchemaSerialize(_boolFormatter, ref buffer, ref offset, value.bDisplayed);
			Serializer.SchemaSerialize(_boolFormatter, ref buffer, ref offset, value.bNew);
			Serializer.SchemaSerialize(_boolFormatter, ref buffer, ref offset, value.bShownFirst);
			Serializer.SchemaSerialize(_boolFormatter, ref buffer, ref offset, value.bUnlocked);
			Serializer.SchemaSerialize(_intFormatter, ref buffer, ref offset, value.ItemID);
		}

		public void Deserialize(byte[] buffer, ref int offset, ref HREncyclopediaSystem.EncyclopediaSaveData value)
		{
			Serializer.DeserializeSchema<HREncyclopediaSystem.EncyclopediaSaveData>(ref SchemaDeserializer, buffer, ref offset, "bDisplayed", "bNew", "bShownFirst", "bUnlocked", "ItemID");
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _boolFormatter, buffer, ref offset, ref value.bDisplayed);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _boolFormatter, buffer, ref offset, ref value.bNew);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _boolFormatter, buffer, ref offset, ref value.bShownFirst);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _boolFormatter, buffer, ref offset, ref value.bUnlocked);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _intFormatter, buffer, ref offset, ref value.ItemID);
		}
	}

	internal class SavedSpawnedObjectInfoFormatter : IFormatter<SavedSpawnedObjectInfo>
	{
		CerasSerializer Serializer;
		CerasSerializer.SchemaDeserializer SchemaDeserializer;
		IFormatter<bool> _boolFormatter;
		IFormatter<int> _intFormatter;
		IFormatter<UnityEngine.Quaternion> _quaternionFormatter;
		IFormatter<UnityEngine.Vector3> _vector3Formatter;

		public void Serialize(ref byte[] buffer, ref int offset, SavedSpawnedObjectInfo value)
		{
			Serializer.SerializeSchema<SavedSpawnedObjectInfo>(ref buffer, ref offset, "bIsGroupLeader", "bIsInGroup", "EnemyIndex", "Rotation", "GroupFollowOffset", "Position");
			Serializer.SchemaSerialize(_boolFormatter, ref buffer, ref offset, value.bIsGroupLeader);
			Serializer.SchemaSerialize(_boolFormatter, ref buffer, ref offset, value.bIsInGroup);
			Serializer.SchemaSerialize(_intFormatter, ref buffer, ref offset, value.EnemyIndex);
			Serializer.SchemaSerialize(_quaternionFormatter, ref buffer, ref offset, value.Rotation);
			Serializer.SchemaSerialize(_vector3Formatter, ref buffer, ref offset, value.GroupFollowOffset);
			Serializer.SchemaSerialize(_vector3Formatter, ref buffer, ref offset, value.Position);
		}

		public void Deserialize(byte[] buffer, ref int offset, ref SavedSpawnedObjectInfo value)
		{
			Serializer.DeserializeSchema<SavedSpawnedObjectInfo>(ref SchemaDeserializer, buffer, ref offset, "bIsGroupLeader", "bIsInGroup", "EnemyIndex", "Rotation", "GroupFollowOffset", "Position");
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _boolFormatter, buffer, ref offset, ref value.bIsGroupLeader);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _boolFormatter, buffer, ref offset, ref value.bIsInGroup);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _intFormatter, buffer, ref offset, ref value.EnemyIndex);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _quaternionFormatter, buffer, ref offset, ref value.Rotation);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _vector3Formatter, buffer, ref offset, ref value.GroupFollowOffset);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _vector3Formatter, buffer, ref offset, ref value.Position);
		}
	}

	internal class MarkerFormatter : IFormatter<WorldMap.Marker>
	{
		CerasSerializer Serializer;
		CerasSerializer.SchemaDeserializer SchemaDeserializer;
		IFormatter<long> _longFormatter;
		IFormatter<string> _stringFormatter;
		IFormatter<UnityEngine.Vector3> _vector3Formatter;
		IFormatter<WorldMap.MarkerType> _markerTypeFormatter;

		public void Serialize(ref byte[] buffer, ref int offset, WorldMap.Marker value)
		{
			Serializer.SerializeSchema<WorldMap.Marker>(ref buffer, ref offset, "Timestamp", "Id", "Name", "WorldPosition", "Type");
			Serializer.SchemaSerialize(_longFormatter, ref buffer, ref offset, value.Timestamp);
			Serializer.SchemaSerialize(_stringFormatter, ref buffer, ref offset, value.Id);
			Serializer.SchemaSerialize(_stringFormatter, ref buffer, ref offset, value.Name);
			Serializer.SchemaSerialize(_vector3Formatter, ref buffer, ref offset, value.WorldPosition);
			Serializer.SchemaSerialize(_markerTypeFormatter, ref buffer, ref offset, value.Type);
		}

		public void Deserialize(byte[] buffer, ref int offset, ref WorldMap.Marker value)
		{
			Serializer.DeserializeSchema<WorldMap.Marker>(ref SchemaDeserializer, buffer, ref offset, "Timestamp", "Id", "Name", "WorldPosition", "Type");
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _longFormatter, buffer, ref offset, ref value.Timestamp);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _stringFormatter, buffer, ref offset, ref value.Id);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _stringFormatter, buffer, ref offset, ref value.Name);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _vector3Formatter, buffer, ref offset, ref value.WorldPosition);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _markerTypeFormatter, buffer, ref offset, ref value.Type);
		}
	}

	internal class TerrainRecastFormatter : IFormatter<TerrainRecast>
	{
		CerasSerializer Serializer;
		CerasSerializer.SchemaDeserializer SchemaDeserializer;
		IFormatter<int> _intFormatter;
		IFormatter<float> _floatFormatter;
		IFormatter<TileData[]> _tileDataArrayFormatter;

		public void Serialize(ref byte[] buffer, ref int offset, TerrainRecast value)
		{
			Serializer.SerializeSchema<TerrainRecast>(ref buffer, ref offset, "tileXCount", "tileZCount", "TileWorldSizeX", "TileWorldSizeZ", "centerX", "centerZ", "sizeX", "sizeZ", "tiles");
			Serializer.SchemaSerialize(_intFormatter, ref buffer, ref offset, value.tileXCount);
			Serializer.SchemaSerialize(_intFormatter, ref buffer, ref offset, value.tileZCount);
			Serializer.SchemaSerialize(_floatFormatter, ref buffer, ref offset, value.TileWorldSizeX);
			Serializer.SchemaSerialize(_floatFormatter, ref buffer, ref offset, value.TileWorldSizeZ);
			Serializer.SchemaSerialize(_floatFormatter, ref buffer, ref offset, value.centerX);
			Serializer.SchemaSerialize(_floatFormatter, ref buffer, ref offset, value.centerZ);
			Serializer.SchemaSerialize(_floatFormatter, ref buffer, ref offset, value.sizeX);
			Serializer.SchemaSerialize(_floatFormatter, ref buffer, ref offset, value.sizeZ);
			Serializer.SchemaSerialize(_tileDataArrayFormatter, ref buffer, ref offset, value.tiles);
		}

		public void Deserialize(byte[] buffer, ref int offset, ref TerrainRecast value)
		{
			Serializer.DeserializeSchema<TerrainRecast>(ref SchemaDeserializer, buffer, ref offset, "tileXCount", "tileZCount", "TileWorldSizeX", "TileWorldSizeZ", "centerX", "centerZ", "sizeX", "sizeZ", "tiles");
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _intFormatter, buffer, ref offset, ref value.tileXCount);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _intFormatter, buffer, ref offset, ref value.tileZCount);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _floatFormatter, buffer, ref offset, ref value.TileWorldSizeX);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _floatFormatter, buffer, ref offset, ref value.TileWorldSizeZ);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _floatFormatter, buffer, ref offset, ref value.centerX);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _floatFormatter, buffer, ref offset, ref value.centerZ);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _floatFormatter, buffer, ref offset, ref value.sizeX);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _floatFormatter, buffer, ref offset, ref value.sizeZ);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _tileDataArrayFormatter, buffer, ref offset, ref value.tiles);
		}
	}

	internal class TileDataFormatter : IFormatter<TileData>
	{
		CerasSerializer Serializer;
		CerasSerializer.SchemaDeserializer SchemaDeserializer;
		IFormatter<System.Int32[]> _int32ArrayFormatter;
		IFormatter<float> _floatFormatter;
		IFormatter<Vert3[]> _vert3ArrayFormatter;

		public void Serialize(ref byte[] buffer, ref int offset, TileData value)
		{
			Serializer.SerializeSchema<TileData>(ref buffer, ref offset, "tris", "CenterY", "SizeY", "verts");
			Serializer.SchemaSerialize(_int32ArrayFormatter, ref buffer, ref offset, value.tris);
			Serializer.SchemaSerialize(_floatFormatter, ref buffer, ref offset, value.CenterY);
			Serializer.SchemaSerialize(_floatFormatter, ref buffer, ref offset, value.SizeY);
			Serializer.SchemaSerialize(_vert3ArrayFormatter, ref buffer, ref offset, value.verts);
		}

		public void Deserialize(byte[] buffer, ref int offset, ref TileData value)
		{
			Serializer.DeserializeSchema<TileData>(ref SchemaDeserializer, buffer, ref offset, "tris", "CenterY", "SizeY", "verts");
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _int32ArrayFormatter, buffer, ref offset, ref value.tris);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _floatFormatter, buffer, ref offset, ref value.CenterY);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _floatFormatter, buffer, ref offset, ref value.SizeY);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _vert3ArrayFormatter, buffer, ref offset, ref value.verts);
		}
	}

	internal class Vert3Formatter : IFormatter<Vert3>
	{
		CerasSerializer Serializer;
		CerasSerializer.SchemaDeserializer SchemaDeserializer;
		IFormatter<int> _intFormatter;

		public void Serialize(ref byte[] buffer, ref int offset, Vert3 value)
		{
			Serializer.SerializeSchema<Vert3>(ref buffer, ref offset, "x", "y", "z");
			Serializer.SchemaSerialize(_intFormatter, ref buffer, ref offset, value.x);
			Serializer.SchemaSerialize(_intFormatter, ref buffer, ref offset, value.y);
			Serializer.SchemaSerialize(_intFormatter, ref buffer, ref offset, value.z);
		}

		public void Deserialize(byte[] buffer, ref int offset, ref Vert3 value)
		{
			Serializer.DeserializeSchema<Vert3>(ref SchemaDeserializer, buffer, ref offset, "x", "y", "z");
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _intFormatter, buffer, ref offset, ref value.x);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _intFormatter, buffer, ref offset, ref value.y);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _intFormatter, buffer, ref offset, ref value.z);
		}
	}

	internal class BasePCVCellFormatter : IFormatter<BasePCVVolume.BasePCVCell>
	{
		CerasSerializer Serializer;
		CerasSerializer.SchemaDeserializer SchemaDeserializer;
		IFormatter<int> _intFormatter;
		IFormatter<float> _floatFormatter;
		IFormatter<UnityEngine.Vector3> _vector3Formatter;

		public void Serialize(ref byte[] buffer, ref int offset, BasePCVVolume.BasePCVCell value)
		{
			Serializer.SerializeSchema<BasePCVVolume.BasePCVCell>(ref buffer, ref offset, "Index", "Layer", "UnitIndex", "x", "y", "z", "GroundingScore", "Position");
			Serializer.SchemaSerialize(_intFormatter, ref buffer, ref offset, value.Index);
			Serializer.SchemaSerialize(_intFormatter, ref buffer, ref offset, value.Layer);
			Serializer.SchemaSerialize(_intFormatter, ref buffer, ref offset, value.UnitIndex);
			Serializer.SchemaSerialize(_intFormatter, ref buffer, ref offset, value.x);
			Serializer.SchemaSerialize(_intFormatter, ref buffer, ref offset, value.y);
			Serializer.SchemaSerialize(_intFormatter, ref buffer, ref offset, value.z);
			Serializer.SchemaSerialize(_floatFormatter, ref buffer, ref offset, value.GroundingScore);
			Serializer.SchemaSerialize(_vector3Formatter, ref buffer, ref offset, value.Position);
		}

		public void Deserialize(byte[] buffer, ref int offset, ref BasePCVVolume.BasePCVCell value)
		{
			Serializer.DeserializeSchema<BasePCVVolume.BasePCVCell>(ref SchemaDeserializer, buffer, ref offset, "Index", "Layer", "UnitIndex", "x", "y", "z", "GroundingScore", "Position");
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _intFormatter, buffer, ref offset, ref value.Index);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _intFormatter, buffer, ref offset, ref value.Layer);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _intFormatter, buffer, ref offset, ref value.UnitIndex);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _intFormatter, buffer, ref offset, ref value.x);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _intFormatter, buffer, ref offset, ref value.y);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _intFormatter, buffer, ref offset, ref value.z);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _floatFormatter, buffer, ref offset, ref value.GroundingScore);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _vector3Formatter, buffer, ref offset, ref value.Position);
		}
	}

	internal class BaseNObjectNetworkDataFormatter : IFormatter<BaseNObjectNetworkData>
	{
		CerasSerializer Serializer;
		CerasSerializer.SchemaDeserializer SchemaDeserializer;
		IFormatter<System.Collections.Generic.List<BaseNObjectMemberData>> _list_BaseNObjectMemberDataFormatter;

		public void Serialize(ref byte[] buffer, ref int offset, BaseNObjectNetworkData value)
		{
			Serializer.SerializeSchema<BaseNObjectNetworkData>(ref buffer, ref offset, "data");
			Serializer.SchemaSerialize(_list_BaseNObjectMemberDataFormatter, ref buffer, ref offset, value.data);
		}

		public void Deserialize(byte[] buffer, ref int offset, ref BaseNObjectNetworkData value)
		{
			Serializer.DeserializeSchema<BaseNObjectNetworkData>(ref SchemaDeserializer, buffer, ref offset, "data");
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _list_BaseNObjectMemberDataFormatter, buffer, ref offset, ref value.data);
		}
	}

	internal class SaveFileStructFormatter : IFormatter<HRSaveFile.SaveFileStruct>
	{
		CerasSerializer Serializer;
		CerasSerializer.SchemaDeserializer SchemaDeserializer;
		IFormatter<HRSaveFile.SaveFileObject[]> _saveFileObjectArrayFormatter;
		IFormatter<System.Byte[]> _byteArrayFormatter;
		IFormatter<System.Collections.Generic.Queue<int>> _queue_intFormatter;
		IFormatter<int> _intFormatter;

		public void Serialize(ref byte[] buffer, ref int offset, HRSaveFile.SaveFileStruct value)
		{
			Serializer.SerializeSchema<HRSaveFile.SaveFileStruct>(ref buffer, ref offset, "SaveFileObjects", "SaveFileObjectsData", "AvailableLargeSpace", "AvailableMediumSpace", "AvailableSmallSpace", "WriterHead");
			Serializer.SchemaSerialize(_saveFileObjectArrayFormatter, ref buffer, ref offset, value.SaveFileObjects);
			Serializer.SchemaSerialize(_byteArrayFormatter, ref buffer, ref offset, value.SaveFileObjectsData);
			Serializer.SchemaSerialize(_queue_intFormatter, ref buffer, ref offset, value.AvailableLargeSpace);
			Serializer.SchemaSerialize(_queue_intFormatter, ref buffer, ref offset, value.AvailableMediumSpace);
			Serializer.SchemaSerialize(_queue_intFormatter, ref buffer, ref offset, value.AvailableSmallSpace);
			Serializer.SchemaSerialize(_intFormatter, ref buffer, ref offset, value.WriterHead);
		}

		public void Deserialize(byte[] buffer, ref int offset, ref HRSaveFile.SaveFileStruct value)
		{
			Serializer.DeserializeSchema<HRSaveFile.SaveFileStruct>(ref SchemaDeserializer, buffer, ref offset, "SaveFileObjects", "SaveFileObjectsData", "AvailableLargeSpace", "AvailableMediumSpace", "AvailableSmallSpace", "WriterHead");
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _saveFileObjectArrayFormatter, buffer, ref offset, ref value.SaveFileObjects);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _byteArrayFormatter, buffer, ref offset, ref value.SaveFileObjectsData);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _queue_intFormatter, buffer, ref offset, ref value.AvailableLargeSpace);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _queue_intFormatter, buffer, ref offset, ref value.AvailableMediumSpace);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _queue_intFormatter, buffer, ref offset, ref value.AvailableSmallSpace);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _intFormatter, buffer, ref offset, ref value.WriterHead);
		}
	}

	internal class SaveFileObjectFormatter : IFormatter<HRSaveFile.SaveFileObject>
	{
		CerasSerializer Serializer;
		CerasSerializer.SchemaDeserializer SchemaDeserializer;
		IFormatter<int> _intFormatter;
		IFormatter<string> _stringFormatter;

		public void Serialize(ref byte[] buffer, ref int offset, HRSaveFile.SaveFileObject value)
		{
			Serializer.SerializeSchema<HRSaveFile.SaveFileObject>(ref buffer, ref offset, "DataOffset", "DataSize", "ObjectKey");
			Serializer.SchemaSerialize(_intFormatter, ref buffer, ref offset, value.DataOffset);
			Serializer.SchemaSerialize(_intFormatter, ref buffer, ref offset, value.DataSize);
			Serializer.SchemaSerialize(_stringFormatter, ref buffer, ref offset, value.ObjectKey);
		}

		public void Deserialize(byte[] buffer, ref int offset, ref HRSaveFile.SaveFileObject value)
		{
			Serializer.DeserializeSchema<HRSaveFile.SaveFileObject>(ref SchemaDeserializer, buffer, ref offset, "DataOffset", "DataSize", "ObjectKey");
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _intFormatter, buffer, ref offset, ref value.DataOffset);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _intFormatter, buffer, ref offset, ref value.DataSize);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _stringFormatter, buffer, ref offset, ref value.ObjectKey);
		}
	}

	internal class NObjectChunkDataFormatter : IFormatter<NObjectChunkData>
	{
		CerasSerializer Serializer;
		CerasSerializer.SchemaDeserializer SchemaDeserializer;
		IFormatter<NObjectChunk[,]> _nObjectChunk2DArrayFormatter;

		public void Serialize(ref byte[] buffer, ref int offset, NObjectChunkData value)
		{
			Serializer.SerializeSchema<NObjectChunkData>(ref buffer, ref offset, "chunks");
			Serializer.SchemaSerialize(_nObjectChunk2DArrayFormatter, ref buffer, ref offset, value.chunks);
		}

		public void Deserialize(byte[] buffer, ref int offset, ref NObjectChunkData value)
		{
			Serializer.DeserializeSchema<NObjectChunkData>(ref SchemaDeserializer, buffer, ref offset, "chunks");
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _nObjectChunk2DArrayFormatter, buffer, ref offset, ref value.chunks);
		}
	}

	internal class NObjectChunkFormatter : IFormatter<NObjectChunk>
	{
		CerasSerializer Serializer;
		CerasSerializer.SchemaDeserializer SchemaDeserializer;
		IFormatter<System.Collections.Generic.List<int>> _list_intFormatter;
		IFormatter<int> _intFormatter;

		public void Serialize(ref byte[] buffer, ref int offset, NObjectChunk value)
		{
			Serializer.SerializeSchema<NObjectChunk>(ref buffer, ref offset, "nObjectIDs", "chunkID");
			Serializer.SchemaSerialize(_list_intFormatter, ref buffer, ref offset, value.nObjectIDs);
			Serializer.SchemaSerialize(_intFormatter, ref buffer, ref offset, value.chunkID);
		}

		public void Deserialize(byte[] buffer, ref int offset, ref NObjectChunk value)
		{
			Serializer.DeserializeSchema<NObjectChunk>(ref SchemaDeserializer, buffer, ref offset, "nObjectIDs", "chunkID");
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _list_intFormatter, buffer, ref offset, ref value.nObjectIDs);
			Serializer.SchemaDeserialize(ref SchemaDeserializer, _intFormatter, buffer, ref offset, ref value.chunkID);
		}
	}

	public static class GeneratedFormatters
	{	
        public static void UseFormatters(SerializerConfig config)
		{
			config.ConfigType<HRWateringCan>().CustomFormatter = new HRWateringCanFormatter();
			config.ConfigType<BaseAchievementManager>().CustomFormatter = new BaseAchievementManagerFormatter();
			config.ConfigType<BaseArmorComponent>().CustomFormatter = new BaseArmorComponentFormatter();
			config.ConfigType<BaseArmorManager>().CustomFormatter = new BaseArmorManagerFormatter();
			config.ConfigType<BaseHP>().CustomFormatter = new BaseHPFormatter();
			config.ConfigType<BaseProjectilePhysics>().CustomFormatter = new BaseProjectilePhysicsFormatter();
			config.ConfigType<BaseAreaReplacer>().CustomFormatter = new BaseAreaReplacerFormatter();
			config.ConfigType<BaseColorCustomizationComponent>().CustomFormatter = new BaseColorCustomizationComponentFormatter();
			config.ConfigType<BaseAutomaticDoor>().CustomFormatter = new BaseAutomaticDoorFormatter();
			config.ConfigType<BaseManualDoor>().CustomFormatter = new BaseManualDoorFormatter();
			config.ConfigType<BaseGunComponent>().CustomFormatter = new BaseGunComponentFormatter();
			config.ConfigType<BaseLockedItem>().CustomFormatter = new BaseLockedItemFormatter();
			config.ConfigType<BaseMortarComponent>().CustomFormatter = new BaseMortarComponentFormatter();
			config.ConfigType<BaseProjectileGun>().CustomFormatter = new BaseProjectileGunFormatter();
			config.ConfigType<BaseWeapon>().CustomFormatter = new BaseWeaponFormatter();
			config.ConfigType<BaseWeaponMelee>().CustomFormatter = new BaseWeaponMeleeFormatter();
			config.ConfigType<MultiPointGunComponent>().CustomFormatter = new MultiPointGunComponentFormatter();
			config.ConfigType<ShepherdGunComponent>().CustomFormatter = new ShepherdGunComponentFormatter();
			config.ConfigType<ZenaGunComponent>().CustomFormatter = new ZenaGunComponentFormatter();
			config.ConfigType<BaseContainer>().CustomFormatter = new BaseContainerFormatter();
			config.ConfigType<BaseInventory>().CustomFormatter = new BaseInventoryFormatter();
			config.ConfigType<HRAchievementManager>().CustomFormatter = new HRAchievementManagerFormatter();
			config.ConfigType<HRAttributeManager>().CustomFormatter = new HRAttributeManagerFormatter();
			config.ConfigType<HRAttributeData>().CustomFormatter = new HRAttributeDataFormatter();
			config.ConfigType<HRAttributeManagerSave>().CustomFormatter = new HRAttributeManagerSaveFormatter();
			config.ConfigType<HRAttributeRandomizer>().CustomFormatter = new HRAttributeRandomizerFormatter();
			config.ConfigType<HRBankSystem>().CustomFormatter = new HRBankSystemFormatter();
			config.ConfigType<BankAccountData>().CustomFormatter = new BankAccountDataFormatter();
			config.ConfigType<BankAccountOwnerData>().CustomFormatter = new BankAccountOwnerDataFormatter();
			config.ConfigType<HRConstructionComponent>().CustomFormatter = new HRConstructionComponentFormatter();
			config.ConfigType<HRDateStruct>().CustomFormatter = new HRDateStructFormatter();
			config.ConfigType<HRConstructionManager>().CustomFormatter = new HRConstructionManagerFormatter();
			config.ConfigType<HRCraftingComponent>().CustomFormatter = new HRCraftingComponentFormatter();
			config.ConfigType<HRCraftingComponent.CraftedItemData>().CustomFormatter = new CraftedItemDataFormatter();
			config.ConfigType<HRNetworkManager.HRLocalPlayerData>().CustomFormatter = new HRLocalPlayerDataFormatter();
			config.ConfigType<HRCraftingInfo.HRCraftingIngredient>().CustomFormatter = new HRCraftingIngredientFormatter();
			config.ConfigType<HRCraftingComponent.SavedCraftedItemData>().CustomFormatter = new SavedCraftedItemDataFormatter();
			config.ConfigType<HRPaintingSystem>().CustomFormatter = new HRPaintingSystemFormatter();
			config.ConfigType<HRFearComponent>().CustomFormatter = new HRFearComponentFormatter();
			config.ConfigType<HRFloorTile>().CustomFormatter = new HRFloorTileFormatter();
			config.ConfigType<HRItemValue>().CustomFormatter = new HRItemValueFormatter();
			config.ConfigType<HRNPCShopSystem>().CustomFormatter = new HRNPCShopSystemFormatter();
			config.ConfigType<HRDateAndTimeStruct>().CustomFormatter = new HRDateAndTimeStructFormatter();
			config.ConfigType<HRQuestCustomers>().CustomFormatter = new HRQuestCustomersFormatter();
			config.ConfigType<HRQuestCustomer_Saved>().CustomFormatter = new HRQuestCustomer_SavedFormatter();
			config.ConfigType<HRReputation>().CustomFormatter = new HRReputationFormatter();
			config.ConfigType<HRShopManager>().CustomFormatter = new HRShopManagerFormatter();
			config.ConfigType<HRRatingData>().CustomFormatter = new HRRatingDataFormatter();
			config.ConfigType<HRShopPlot>().CustomFormatter = new HRShopPlotFormatter();
			config.ConfigType<HREmployeeSystem>().CustomFormatter = new HREmployeeSystemFormatter();
			config.ConfigType<HREmployeeSystem.EmployeeSaveData>().CustomFormatter = new EmployeeSaveDataFormatter();
			config.ConfigType<HRPlotManager>().CustomFormatter = new HRPlotManagerFormatter();
			config.ConfigType<PlotManagerSaveData>().CustomFormatter = new PlotManagerSaveDataFormatter();
			config.ConfigType<HREncounterSpawner>().CustomFormatter = new HREncounterSpawnerFormatter();
			config.ConfigType<HREncounterSaveData>().CustomFormatter = new HREncounterSaveDataFormatter();
			config.ConfigType<HREncounterDynamicEnemyData>().CustomFormatter = new HREncounterDynamicEnemyDataFormatter();
			config.ConfigType<EncounterObjectiveSaveData>().CustomFormatter = new EncounterObjectiveSaveDataFormatter();
			config.ConfigType<HRPlantComponent>().CustomFormatter = new HRPlantComponentFormatter();
			config.ConfigType<InnSystem>().CustomFormatter = new InnSystemFormatter();
			config.ConfigType<InnSystem.SavedRoomStatus>().CustomFormatter = new SavedRoomStatusFormatter();
			config.ConfigType<ReplaceOnHit>().CustomFormatter = new ReplaceOnHitFormatter();
			config.ConfigType<ReplaceOnHitInteract>().CustomFormatter = new ReplaceOnHitInteractFormatter();
			config.ConfigType<HRShopEntityComponent>().CustomFormatter = new HRShopEntityComponentFormatter();
			config.ConfigType<HRZenaFightHelper>().CustomFormatter = new HRZenaFightHelperFormatter();
			config.ConfigType<HRCalendarManager>().CustomFormatter = new HRCalendarManagerFormatter();
			config.ConfigType<HRConversationPoolManager>().CustomFormatter = new HRConversationPoolManagerFormatter();
			config.ConfigType<HRConversationPoolManager.ConversationPool>().CustomFormatter = new ConversationPoolFormatter();
			config.ConfigType<HRConversationPoolManager.ConversationPoolCollection>().CustomFormatter = new ConversationPoolCollectionFormatter();
			config.ConfigType<HRDayManager>().CustomFormatter = new HRDayManagerFormatter();
			config.ConfigType<HRTimeTrigger>().CustomFormatter = new HRTimeTriggerFormatter();
			config.ConfigType<HRConsumableRecipeUnlock>().CustomFormatter = new HRConsumableRecipeUnlockFormatter();
			config.ConfigType<HRDeathBox>().CustomFormatter = new HRDeathBoxFormatter();
			config.ConfigType<HRDisplayContainer>().CustomFormatter = new HRDisplayContainerFormatter();
			config.ConfigType<HRDynamicZipline>().CustomFormatter = new HRDynamicZiplineFormatter();
			config.ConfigType<HRItemCustomizationStation>().CustomFormatter = new HRItemCustomizationStationFormatter();
			config.ConfigType<HRMannequinContainer>().CustomFormatter = new HRMannequinContainerFormatter();
			config.ConfigType<HROpenSign>().CustomFormatter = new HROpenSignFormatter();
			config.ConfigType<HRUpgradeTable>().CustomFormatter = new HRUpgradeTableFormatter();
			config.ConfigType<HRZiplineAnchor>().CustomFormatter = new HRZiplineAnchorFormatter();
			config.ConfigType<HRClothingComponent>().CustomFormatter = new HRClothingComponentFormatter();
			config.ConfigType<HRContainer>().CustomFormatter = new HRContainerFormatter();
			config.ConfigType<HRFuelInventory>().CustomFormatter = new HRFuelInventoryFormatter();
			config.ConfigType<HRInventory>().CustomFormatter = new HRInventoryFormatter();
			config.ConfigType<HRGolfManager>().CustomFormatter = new HRGolfManagerFormatter();
			config.ConfigType<HRTypingInputMinigameComponent>().CustomFormatter = new HRTypingInputMinigameComponentFormatter();
			config.ConfigType<HRNeedManager>().CustomFormatter = new HRNeedManagerFormatter();
			config.ConfigType<NeedDataToSave>().CustomFormatter = new NeedDataToSaveFormatter();
			config.ConfigType<HRNeedRuntimeData>().CustomFormatter = new HRNeedRuntimeDataFormatter();
			config.ConfigType<HRStaminaComponent>().CustomFormatter = new HRStaminaComponentFormatter();
			config.ConfigType<HRXPComponent>().CustomFormatter = new HRXPComponentFormatter();
			config.ConfigType<HRQuestTrigger>().CustomFormatter = new HRQuestTriggerFormatter();
			config.ConfigType<HRSaveComponent>().CustomFormatter = new HRSaveComponentFormatter();
			config.ConfigType<HRSaveSystem>().CustomFormatter = new HRSaveSystemFormatter();
			config.ConfigType<HRShopSpecialtiesManager>().CustomFormatter = new HRShopSpecialtiesManagerFormatter();
			config.ConfigType<HRGlobalPlayerSkillTreeInstance>().CustomFormatter = new HRGlobalPlayerSkillTreeInstanceFormatter();
			config.ConfigType<HRSkillTreeInstance.SkillUnlockData>().CustomFormatter = new SkillUnlockDataFormatter();
			config.ConfigType<HRSkillTreeInstance>().CustomFormatter = new HRSkillTreeInstanceFormatter();
			config.ConfigType<HRPlayerCustomizationUI>().CustomFormatter = new HRPlayerCustomizationUIFormatter();
			config.ConfigType<HRPlayerTotem>().CustomFormatter = new HRPlayerTotemFormatter();
			config.ConfigType<HRWorldMapTotemManager>().CustomFormatter = new HRWorldMapTotemManagerFormatter();
			config.ConfigType<MapTotemSaveData>().CustomFormatter = new MapTotemSaveDataFormatter();
			config.ConfigType<PlayerTotemSaveData>().CustomFormatter = new PlayerTotemSaveDataFormatter();
			config.ConfigType<PlayerLastUsedTotemSaveData>().CustomFormatter = new PlayerLastUsedTotemSaveDataFormatter();
			config.ConfigType<HRWorldEnemySpawner>().CustomFormatter = new HRWorldEnemySpawnerFormatter();
			config.ConfigType<BaseNObjectData>().CustomFormatter = new BaseNObjectDataFormatter();
			config.ConfigType<BaseNObjectMemberData>().CustomFormatter = new BaseNObjectMemberDataFormatter();
			config.ConfigType<HRCollectableManager.CollectableData>().CustomFormatter = new CollectableDataFormatter();
			config.ConfigType<TransformData>().CustomFormatter = new TransformDataFormatter();
			config.ConfigType<HRBarteringSystem.SavedSaleInfo>().CustomFormatter = new SavedSaleInfoFormatter();
			config.ConfigType<HREncyclopediaSystem.EncyclopediaSaveData>().CustomFormatter = new EncyclopediaSaveDataFormatter();
			config.ConfigType<SavedSpawnedObjectInfo>().CustomFormatter = new SavedSpawnedObjectInfoFormatter();
			config.ConfigType<WorldMap.Marker>().CustomFormatter = new MarkerFormatter();
			config.ConfigType<TerrainRecast>().CustomFormatter = new TerrainRecastFormatter();
			config.ConfigType<TileData>().CustomFormatter = new TileDataFormatter();
			config.ConfigType<Vert3>().CustomFormatter = new Vert3Formatter();
			config.ConfigType<BasePCVVolume.BasePCVCell>().CustomFormatter = new BasePCVCellFormatter();
			config.ConfigType<BaseNObjectNetworkData>().CustomFormatter = new BaseNObjectNetworkDataFormatter();
			config.ConfigType<HRSaveFile.SaveFileStruct>().CustomFormatter = new SaveFileStructFormatter();
			config.ConfigType<HRSaveFile.SaveFileObject>().CustomFormatter = new SaveFileObjectFormatter();
			config.ConfigType<NObjectChunkData>().CustomFormatter = new NObjectChunkDataFormatter();
			config.ConfigType<NObjectChunk>().CustomFormatter = new NObjectChunkFormatter();

        }

        private static void AotHint(SerializerConfig config)
		{
			Ceras.Formatters.CollectionFormatter<System.Collections.Generic.HashSet<string>, string> var0 = default;
			config.ConfigType<System.Collections.Generic.HashSet<string>>().CustomFormatter = var0;
			Ceras.Formatters.CollectionFormatter<System.Collections.Generic.HashSet<System.ValueTuple<string, long>>, System.ValueTuple<string, long>> var1 = default;
			config.ConfigType<System.Collections.Generic.HashSet<System.ValueTuple<string, long>>>().CustomFormatter = var1;
			Ceras.Formatters.ValueTupleFormatter<string, long> var2 = default;
			config.ConfigType<System.ValueTuple<string, long>>().CustomFormatter = var2;
			Ceras.Formatters.CollectionFormatter<System.Collections.Generic.List<HRCategory>, HRCategory> var3 = default;
			config.ConfigType<System.Collections.Generic.List<HRCategory>>().CustomFormatter = var3;
			Ceras.Formatters.ReinterpretFormatter<HRCategory> var4 = default;
			config.ConfigType<HRCategory>().CustomFormatter = var4;
			Ceras.Formatters.CollectionFormatter<System.Collections.Generic.List<int>, int> var5 = default;
			config.ConfigType<System.Collections.Generic.List<int>>().CustomFormatter = var5;
			Ceras.Formatters.ArrayFormatter<string> var6 = default;
			config.ConfigType<System.String[]>().CustomFormatter = var6;
			Ceras.Formatters.ArrayFormatter<int> var7 = default;
			config.ConfigType<System.Int32[]>().CustomFormatter = var7;
			Ceras.Formatters.CollectionFormatter<System.Collections.Generic.List<HRAttributeData>, HRAttributeData> var8 = default;
			config.ConfigType<System.Collections.Generic.List<HRAttributeData>>().CustomFormatter = var8;
			Ceras.Formatters.ArrayFormatter<BankAccountData> var9 = default;
			config.ConfigType<BankAccountData[]>().CustomFormatter = var9;
			Ceras.Formatters.ArrayFormatter<HRCraftingInfo.HRCraftingIngredient> var10 = default;
			config.ConfigType<HRCraftingInfo.HRCraftingIngredient[]>().CustomFormatter = var10;
			Ceras.Formatters.CollectionFormatter<System.Collections.Generic.List<HRCraftingComponent.SavedCraftedItemData>, HRCraftingComponent.SavedCraftedItemData> var11 = default;
			config.ConfigType<System.Collections.Generic.List<HRCraftingComponent.SavedCraftedItemData>>().CustomFormatter = var11;
			Ceras.Formatters.CollectionFormatter<System.Collections.Generic.Dictionary<string, System.String[]>, System.Collections.Generic.KeyValuePair<string, System.String[]>> var12 = default;
			config.ConfigType<System.Collections.Generic.Dictionary<string, System.String[]>>().CustomFormatter = var12;
			Ceras.Resolvers.StandardFormatterResolver.KeyValuePairFormatter<string, System.String[]> var13 = default;
			config.ConfigType<System.Collections.Generic.KeyValuePair<string, System.String[]>>().CustomFormatter = var13;
			Ceras.Formatters.ArrayFormatter<bool> var14 = default;
			config.ConfigType<System.Boolean[]>().CustomFormatter = var14;
			Ceras.Formatters.ArrayFormatter<HRQuestCustomer_Saved> var15 = default;
			config.ConfigType<HRQuestCustomer_Saved[]>().CustomFormatter = var15;
			Ceras.Formatters.CollectionFormatter<System.Collections.Generic.List<HRRatingData>, HRRatingData> var16 = default;
			config.ConfigType<System.Collections.Generic.List<HRRatingData>>().CustomFormatter = var16;
			Ceras.Formatters.ArrayFormatter<byte> var17 = default;
			config.ConfigType<System.Byte[]>().CustomFormatter = var17;
			Ceras.Formatters.CollectionFormatter<System.Collections.Generic.List<HREmployeeSystem.EmployeeSaveData>, HREmployeeSystem.EmployeeSaveData> var18 = default;
			config.ConfigType<System.Collections.Generic.List<HREmployeeSystem.EmployeeSaveData>>().CustomFormatter = var18;
			Ceras.Formatters.MultiDimensionalArrayFormatter<int> var19 = default;
			config.ConfigType<System.Int32[,]>().CustomFormatter = var19;
			Ceras.Formatters.ArrayFormatter<short> var20 = default;
			config.ConfigType<System.Int16[]>().CustomFormatter = var20;
			Ceras.Formatters.ArrayFormatter<EmployeeSystem.Gathering.GatheringArea> var21 = default;
			config.ConfigType<EmployeeSystem.Gathering.GatheringArea[]>().CustomFormatter = var21;
			Ceras.Formatters.ReinterpretFormatter<EmployeeSystem.Gathering.GatheringArea> var22 = default;
			config.ConfigType<EmployeeSystem.Gathering.GatheringArea>().CustomFormatter = var22;
			Ceras.Formatters.CollectionFormatter<System.Collections.Generic.List<HREncounterSaveData>, HREncounterSaveData> var23 = default;
			config.ConfigType<System.Collections.Generic.List<HREncounterSaveData>>().CustomFormatter = var23;
			Ceras.Formatters.CollectionFormatter<System.Collections.Generic.List<HREncounterDynamicEnemyData>, HREncounterDynamicEnemyData> var24 = default;
			config.ConfigType<System.Collections.Generic.List<HREncounterDynamicEnemyData>>().CustomFormatter = var24;
			Ceras.Formatters.ArrayFormatter<EncounterObjectiveSaveData> var25 = default;
			config.ConfigType<EncounterObjectiveSaveData[]>().CustomFormatter = var25;
			Ceras.Formatters.CollectionFormatter<System.Collections.Generic.List<InnSystem.SavedRoomStatus>, InnSystem.SavedRoomStatus> var26 = default;
			config.ConfigType<System.Collections.Generic.List<InnSystem.SavedRoomStatus>>().CustomFormatter = var26;
			Ceras.Formatters.CollectionFormatter<System.Collections.Generic.List<bool>, bool> var27 = default;
			config.ConfigType<System.Collections.Generic.List<bool>>().CustomFormatter = var27;
			Ceras.Formatters.CollectionFormatter<System.Collections.Generic.List<HRConversationPoolManager.ConversationPool>, HRConversationPoolManager.ConversationPool> var28 = default;
			config.ConfigType<System.Collections.Generic.List<HRConversationPoolManager.ConversationPool>>().CustomFormatter = var28;
			Ceras.Formatters.CollectionFormatter<System.Collections.Generic.List<HRConversationPoolManager.ConversationPoolCollection>, HRConversationPoolManager.ConversationPoolCollection> var29 = default;
			config.ConfigType<System.Collections.Generic.List<HRConversationPoolManager.ConversationPoolCollection>>().CustomFormatter = var29;
			Ceras.Formatters.ReinterpretFormatter<UnityEngine.Vector3> var30 = default;
			config.ConfigType<UnityEngine.Vector3>().CustomFormatter = var30;
			Ceras.Formatters.CollectionFormatter<System.Collections.Generic.List<NeedDataToSave>, NeedDataToSave> var31 = default;
			config.ConfigType<System.Collections.Generic.List<NeedDataToSave>>().CustomFormatter = var31;
			Ceras.Formatters.ReinterpretFormatter<HRENeed> var32 = default;
			config.ConfigType<HRENeed>().CustomFormatter = var32;
			Ceras.Formatters.ReinterpretFormatter<UnityEngine.Quaternion> var33 = default;
			config.ConfigType<UnityEngine.Quaternion>().CustomFormatter = var33;
			Ceras.Formatters.CollectionFormatter<System.Collections.Generic.List<string>, string> var34 = default;
			config.ConfigType<System.Collections.Generic.List<string>>().CustomFormatter = var34;
			Ceras.Formatters.CollectionFormatter<System.Collections.Generic.List<HRSkillTreeInstance.SkillUnlockData>, HRSkillTreeInstance.SkillUnlockData> var35 = default;
			config.ConfigType<System.Collections.Generic.List<HRSkillTreeInstance.SkillUnlockData>>().CustomFormatter = var35;
			Ceras.Formatters.ArrayFormatter<MapTotemSaveData> var36 = default;
			config.ConfigType<MapTotemSaveData[]>().CustomFormatter = var36;
			Ceras.Formatters.ArrayFormatter<PlayerTotemSaveData> var37 = default;
			config.ConfigType<PlayerTotemSaveData[]>().CustomFormatter = var37;
			Ceras.Formatters.ArrayFormatter<PlayerLastUsedTotemSaveData> var38 = default;
			config.ConfigType<PlayerLastUsedTotemSaveData[]>().CustomFormatter = var38;
			Ceras.Formatters.ArrayFormatter<BaseNObjectMemberData> var39 = default;
			config.ConfigType<BaseNObjectMemberData[]>().CustomFormatter = var39;
			Ceras.Formatters.CollectionFormatter<System.Collections.Generic.Dictionary<int, int>, System.Collections.Generic.KeyValuePair<int, int>> var40 = default;
			config.ConfigType<System.Collections.Generic.Dictionary<int, int>>().CustomFormatter = var40;
			Ceras.Resolvers.StandardFormatterResolver.KeyValuePairFormatter<int, int> var41 = default;
			config.ConfigType<System.Collections.Generic.KeyValuePair<int, int>>().CustomFormatter = var41;
			Ceras.Formatters.CollectionFormatter<System.Collections.Generic.Dictionary<HeroPlayerCharacter.Faction, int>, System.Collections.Generic.KeyValuePair<HeroPlayerCharacter.Faction, int>> var42 = default;
			config.ConfigType<System.Collections.Generic.Dictionary<HeroPlayerCharacter.Faction, int>>().CustomFormatter = var42;
			Ceras.Resolvers.StandardFormatterResolver.KeyValuePairFormatter<HeroPlayerCharacter.Faction, int> var43 = default;
			config.ConfigType<System.Collections.Generic.KeyValuePair<HeroPlayerCharacter.Faction, int>>().CustomFormatter = var43;
			Ceras.Formatters.ReinterpretFormatter<HeroPlayerCharacter.Faction> var44 = default;
			config.ConfigType<HeroPlayerCharacter.Faction>().CustomFormatter = var44;
			Ceras.Formatters.CollectionFormatter<System.Collections.Generic.List<HRCollectableManager.CollectableData>, HRCollectableManager.CollectableData> var45 = default;
			config.ConfigType<System.Collections.Generic.List<HRCollectableManager.CollectableData>>().CustomFormatter = var45;
			Ceras.Formatters.CollectionFormatter<System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<string>>, System.Collections.Generic.KeyValuePair<string, System.Collections.Generic.List<string>>> var46 = default;
			config.ConfigType<System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<string>>>().CustomFormatter = var46;
			Ceras.Resolvers.StandardFormatterResolver.KeyValuePairFormatter<string, System.Collections.Generic.List<string>> var47 = default;
			config.ConfigType<System.Collections.Generic.KeyValuePair<string, System.Collections.Generic.List<string>>>().CustomFormatter = var47;
			Ceras.Formatters.CollectionFormatter<System.Collections.Generic.Dictionary<string, string>, System.Collections.Generic.KeyValuePair<string, string>> var48 = default;
			config.ConfigType<System.Collections.Generic.Dictionary<string, string>>().CustomFormatter = var48;
			Ceras.Resolvers.StandardFormatterResolver.KeyValuePairFormatter<string, string> var49 = default;
			config.ConfigType<System.Collections.Generic.KeyValuePair<string, string>>().CustomFormatter = var49;
			Ceras.Formatters.CollectionFormatter<System.Collections.Generic.Dictionary<int, HRBarteringSystem.SavedSaleInfo>, System.Collections.Generic.KeyValuePair<int, HRBarteringSystem.SavedSaleInfo>> var50 = default;
			config.ConfigType<System.Collections.Generic.Dictionary<int, HRBarteringSystem.SavedSaleInfo>>().CustomFormatter = var50;
			Ceras.Resolvers.StandardFormatterResolver.KeyValuePairFormatter<int, HRBarteringSystem.SavedSaleInfo> var51 = default;
			config.ConfigType<System.Collections.Generic.KeyValuePair<int, HRBarteringSystem.SavedSaleInfo>>().CustomFormatter = var51;
			Ceras.Formatters.ArrayFormatter<HREncyclopediaSystem.EncyclopediaSaveData> var52 = default;
			config.ConfigType<HREncyclopediaSystem.EncyclopediaSaveData[]>().CustomFormatter = var52;
			Ceras.Formatters.CollectionFormatter<System.Collections.Generic.Dictionary<string, System.Collections.Generic.HashSet<UnityEngine.Vector3>>, System.Collections.Generic.KeyValuePair<string, System.Collections.Generic.HashSet<UnityEngine.Vector3>>> var53 = default;
			config.ConfigType<System.Collections.Generic.Dictionary<string, System.Collections.Generic.HashSet<UnityEngine.Vector3>>>().CustomFormatter = var53;
			Ceras.Resolvers.StandardFormatterResolver.KeyValuePairFormatter<string, System.Collections.Generic.HashSet<UnityEngine.Vector3>> var54 = default;
			config.ConfigType<System.Collections.Generic.KeyValuePair<string, System.Collections.Generic.HashSet<UnityEngine.Vector3>>>().CustomFormatter = var54;
			Ceras.Formatters.CollectionFormatter<System.Collections.Generic.HashSet<UnityEngine.Vector3>, UnityEngine.Vector3> var55 = default;
			config.ConfigType<System.Collections.Generic.HashSet<UnityEngine.Vector3>>().CustomFormatter = var55;
			Ceras.Formatters.CollectionFormatter<System.Collections.Generic.List<SavedSpawnedObjectInfo>, SavedSpawnedObjectInfo> var56 = default;
			config.ConfigType<System.Collections.Generic.List<SavedSpawnedObjectInfo>>().CustomFormatter = var56;
			Ceras.Formatters.CollectionFormatter<System.Collections.Generic.List<WorldMap.Marker>, WorldMap.Marker> var57 = default;
			config.ConfigType<System.Collections.Generic.List<WorldMap.Marker>>().CustomFormatter = var57;
			Ceras.Formatters.ReinterpretFormatter<WorldMap.MarkerType> var58 = default;
			config.ConfigType<WorldMap.MarkerType>().CustomFormatter = var58;
			Ceras.Formatters.ArrayFormatter<TileData> var59 = default;
			config.ConfigType<TileData[]>().CustomFormatter = var59;
			Ceras.Formatters.ArrayFormatter<Vert3> var60 = default;
			config.ConfigType<Vert3[]>().CustomFormatter = var60;
			Ceras.Formatters.ArrayFormatter<BasePCVVolume.BasePCVCell> var61 = default;
			config.ConfigType<BasePCVVolume.BasePCVCell[]>().CustomFormatter = var61;
			Ceras.Formatters.CollectionFormatter<System.Collections.Generic.List<BaseNObjectMemberData>, BaseNObjectMemberData> var62 = default;
			config.ConfigType<System.Collections.Generic.List<BaseNObjectMemberData>>().CustomFormatter = var62;
			Ceras.Formatters.QueueFormatter<int> var63 = default;
			config.ConfigType<System.Collections.Generic.Queue<int>>().CustomFormatter = var63;
			Ceras.Formatters.ArrayFormatter<HRSaveFile.SaveFileObject> var64 = default;
			config.ConfigType<HRSaveFile.SaveFileObject[]>().CustomFormatter = var64;
			Ceras.Formatters.MultiDimensionalArrayFormatter<NObjectChunk> var65 = default;
			config.ConfigType<NObjectChunk[,]>().CustomFormatter = var65;
			ReinterpretArrayFormatter<System.Int32> Int32ReinterpretArrayFormatter = default;
            config.ConfigType<System.Int32[]>().CustomFormatter = Int32ReinterpretArrayFormatter;
			ReinterpretArrayFormatter<System.Boolean> BooleanReinterpretArrayFormatter = default;
            config.ConfigType<System.Boolean[]>().CustomFormatter = BooleanReinterpretArrayFormatter;
			ReinterpretArrayFormatter<HRQuestCustomer_Saved> HRQuestCustomer_SavedReinterpretArrayFormatter = default;
            config.ConfigType<HRQuestCustomer_Saved[]>().CustomFormatter = HRQuestCustomer_SavedReinterpretArrayFormatter;
			ReinterpretArrayFormatter<System.Byte> ByteReinterpretArrayFormatter = default;
            config.ConfigType<System.Byte[]>().CustomFormatter = ByteReinterpretArrayFormatter;
			ReinterpretArrayFormatter<System.Int16> Int16ReinterpretArrayFormatter = default;
            config.ConfigType<System.Int16[]>().CustomFormatter = Int16ReinterpretArrayFormatter;
			ReinterpretArrayFormatter<EmployeeSystem.Gathering.GatheringArea> GatheringAreaReinterpretArrayFormatter = default;
            config.ConfigType<EmployeeSystem.Gathering.GatheringArea[]>().CustomFormatter = GatheringAreaReinterpretArrayFormatter;
			ReinterpretArrayFormatter<EncounterObjectiveSaveData> EncounterObjectiveSaveDataReinterpretArrayFormatter = default;
            config.ConfigType<EncounterObjectiveSaveData[]>().CustomFormatter = EncounterObjectiveSaveDataReinterpretArrayFormatter;
			ReinterpretArrayFormatter<HREncyclopediaSystem.EncyclopediaSaveData> EncyclopediaSaveDataReinterpretArrayFormatter = default;
            config.ConfigType<HREncyclopediaSystem.EncyclopediaSaveData[]>().CustomFormatter = EncyclopediaSaveDataReinterpretArrayFormatter;
			ReinterpretArrayFormatter<Vert3> Vert3ReinterpretArrayFormatter = default;
            config.ConfigType<Vert3[]>().CustomFormatter = Vert3ReinterpretArrayFormatter;
			ReinterpretArrayFormatter<BasePCVVolume.BasePCVCell> BasePCVCellReinterpretArrayFormatter = default;
            config.ConfigType<BasePCVVolume.BasePCVCell[]>().CustomFormatter = BasePCVCellReinterpretArrayFormatter;
		}
	}
}
#nullable restore
#pragma warning restore 649


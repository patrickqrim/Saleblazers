using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemLootTable", menuName = "Data Assets/ItemLootTable", order = 1)]
public class HRItemLootTableDB : ScriptableObject
{
    public HRItemDatabase MainItemDB;

    [System.Serializable]
    public struct HRItemLootTableGroup
    {
        public BaseRarity Rarity;
        public float PercentageChance;
        public List<HRItemLootTableEntry> Items;

        public bool RollRandomLootItem(out HRItemLootTableEntry LootTableEntry, bool bRemoveEntryAfterwards = false, int NestedGroupIndex = -1)
        {
            LootTableEntry = new HRItemLootTableEntry();

            if (Items.Count <= 0) return false;

            float Sum = 0;
            for (int i = 0; i < Items.Count; i++)
            {
                Sum += Items[i].PercentageChance;
            }

            float RandomRoll = Random.Range(0.0f, Sum);
            float Cumulative = 0f;

            for (int i = 0; i < Items.Count; i++)
            {
                Cumulative += Items[i].PercentageChance;
                if (RandomRoll < Cumulative && Items[i].ItemID >= 0)
                {
                    HRItemLootTableEntry Entry = Items[i];

                    if (Entry.NestedLootTableDB != null && NestedGroupIndex >= 0)
                    {
                        return Items[i].NestedLootTableDB.LootTableItems[NestedGroupIndex].RollRandomLootItem(out LootTableEntry, bRemoveEntryAfterwards, -1);
                    }
                    else if(Entry.ItemPrefab != null)
                    {
                        LootTableEntry = Items[i];

                        if (bRemoveEntryAfterwards)
                        {
                            Items.RemoveAt(i);
                        }
                    }
                    else
                    {
                        return false;
                    }


                    return true;
                }
            }

            return false;
        }
    }

    [System.Serializable]
    public struct HRItemLootTableEntry
    {
        public GameObject ItemPrefab;
        public HRItemLootTableDB NestedLootTableDB;
        public string ItemName;
        public int ItemPrice;
        public int ItemID;
        public float PercentageChance;
        public BaseProbabilityTable<BaseRarity> RolledRarities;
        public bool bDontRandomizeColorway;
        public HRRecipeUnlockData RecipeUnlockData;
    }

    public HRItemLootTableGroup[] LootTableItems;

    // THIS IS REALLY BAD BUT IT'S THE EASIEST WAY I CAN THINK OF DOING WATER TYPES WITHOUT REDOING THE WHOLE LEVEL
    public EBaseWaterType WaterType;

    public bool RollRandomLootItem(out HRItemLootTableEntry LootTableEntry)
    {
        float Sum = 0;
        for (int i = 0; i < LootTableItems.Length; i++)
        {
            Sum += LootTableItems[i].PercentageChance;
        }

        float RandomRoll = Random.Range(0.0f, Sum);
        float Cumulative = 0f;
        for (int i = 0; i < LootTableItems.Length; i++)
        {
            Cumulative += LootTableItems[i].PercentageChance;
            //roll for rarity of item
            if (RandomRoll < Cumulative)
            {
                return LootTableItems[i].RollRandomLootItem(out LootTableEntry);
            }
        }

        LootTableEntry = new HRItemLootTableEntry();
        return false;
    }

    public int RollRandomLootGroup(out HRItemLootTableGroup LootTableGroup)
    {
        float Sum = 0;
        for (int i = 0; i < LootTableItems.Length; i++)
        {
            Sum += LootTableItems[i].PercentageChance;
        }

        float RandomRoll = Random.Range(0.0f, Sum);
        float Cumulative = 0f;
        for (int i = 0; i < LootTableItems.Length; i++)
        {
            Cumulative += LootTableItems[i].PercentageChance;

            if (RandomRoll < Cumulative)
            {
                LootTableGroup = LootTableItems[i];
                return i;
            }
        }

        LootTableGroup = new HRItemLootTableGroup();
        return -1;
    }
}


#if UNITY_EDITOR

[CustomPropertyDrawer(typeof(HRItemLootTableDB.HRItemLootTableEntry))]
public class HRItemLootTableEntryEditor : PropertyDrawer
{

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        SerializedProperty NestedLootTableDB = property.FindPropertyRelative("NestedLootTableDB");
        SerializedProperty ItemID = property.FindPropertyRelative("ItemID");

        if (NestedLootTableDB.objectReferenceValue != null)
        {
            return 40f;
        }

        float RecipeDataHeight = ItemID.intValue == 838 ? EditorGUI.GetPropertyHeight(property.FindPropertyRelative("RecipeUnlockData"), true) : 0;
        float RolledRaritiesHeight = ItemID.intValue != 838 ? EditorGUI.GetPropertyHeight(property.FindPropertyRelative("RolledRarities"), true) : 0;

        return 140f + RolledRaritiesHeight + RecipeDataHeight;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        SerializedProperty ItemName = property.FindPropertyRelative("ItemName");
        SerializedProperty ItemPrefab = property.FindPropertyRelative("ItemPrefab");
        SerializedProperty ItemPrice = property.FindPropertyRelative("ItemPrice");
        SerializedProperty NestedLootTableDB = property.FindPropertyRelative("NestedLootTableDB");
        SerializedProperty ItemID = property.FindPropertyRelative("ItemID");
        SerializedProperty PercentageChance = property.FindPropertyRelative("PercentageChance");
        SerializedProperty RolledRarities = property.FindPropertyRelative("RolledRarities");

        HRItemLootTableDB LootTableRef = (HRItemLootTableDB)property.serializedObject.targetObject;

        float Height = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        position.height = Height;
        EditorGUI.PropertyField(position, NestedLootTableDB, new GUIContent("Nested Loot Table DB"));
        position.y += Height;

        if (NestedLootTableDB.objectReferenceValue == null)
        {
            EditorGUI.PropertyField(position, ItemPrefab, new GUIContent("Item Prefab"));
            position.y += Height;
            EditorGUI.LabelField(position, "Item Name", ItemName.stringValue);
            position.y += Height;
            EditorGUI.LabelField(position, "Item ID", ItemID.intValue.ToString());
            position.y += Height;
            ItemPrice.intValue = EditorGUI.IntField(position, "Item Price", ItemPrice.intValue);
            position.y += Height;

            if (ItemPrefab.objectReferenceValue != null && ItemPrefab.objectReferenceValue is GameObject GO)
            {
                if (!PrefabUtility.IsPartOfPrefabAsset(GO))
                {
                    ItemPrefab.objectReferenceValue = null;
                }
                else
                {
                    BaseWeapon WeaponRef = GO.GetComponent<BaseWeapon>();

                    if (WeaponRef)
                    {
                        ItemID.intValue = WeaponRef.ItemID;
                        ItemName.stringValue = WeaponRef.ItemName;
                    }
                }
            }
            else
            {
                if (LootTableRef.MainItemDB != null && ItemID.intValue >= 0 && ItemID.intValue < LootTableRef.MainItemDB.ItemArray.Length)
                {
                    ItemPrefab.objectReferenceValue = LootTableRef.MainItemDB.ItemArray[ItemID.intValue].ItemPrefab;
                }
            }
        }

        PercentageChance.floatValue = EditorGUI.Slider(position,"Drop Weight", PercentageChance.floatValue, 0f, 1f);
        position.y += Height;

        if(ItemID.intValue == 838)
        {
            SerializedProperty RecipeUnlockData = property.FindPropertyRelative("RecipeUnlockData");

            EditorGUI.PropertyField(position, RecipeUnlockData, new GUIContent("Recipe Unlock Data"), true);
        }

        if (NestedLootTableDB.objectReferenceValue == null && ItemID.intValue != 838)
        {
            EditorGUI.PropertyField(position, RolledRarities, new GUIContent("Rolled Rarities"), true);
        }
        else
        {
            if(RolledRarities.isArray && RolledRarities.arraySize > 0)
            {
                RolledRarities.ClearArray();
            }
        }
    }
}

#endif
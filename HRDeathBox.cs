using UnityEngine;
using UnityEngine.Localization;

[Ceras.SerializedType]
public class HRDeathBox : Mirror.NetworkBehaviour, IHRSaveable
{
    const string DefaultBoxName = "Death Box";
    const string PossessiveLocalizationKey = "possessive";

    public BaseWeapon OwningWeapon;
    public TMPro.TMP_Text NameText;

    [ES3Serializable]
    [Mirror.SyncVar(hook = nameof(OnDropCharacterNameChanged))]
    string DropCharacterName;

    [System.NonSerialized, Ceras.SerializedField]
    public string SavedDropCharacterName = null;

    public override void OnStartClient()
    {
        base.OnStartClient();

        if (OwningWeapon)
        {
            OwningWeapon.OnWeaponInitializedDelegate += (InWeapon, bIsPressed) => UpdateName();
            if (OwningWeapon.OwningInteractable)
            {
                OwningWeapon.OwningInteractable.bLocalizeName = false;
            }
        }
        HRGameInstance.LocaleChangedDelegate += UpdateName;
        UpdateName();
    }

    public void SetDropCharacter(HeroPlayerCharacter DropCharacter)
    {
        if (DropCharacter)
        {
            DropCharacterName = DropCharacter.UserName;
        }
        else
        {
            DropCharacterName = null;
        }
    }

    void OnDropCharacterNameChanged(string prevName, string newName) => UpdateName();

    void UpdateName()
    {
        string localizedBoxName = HRItemDatabase.GetLocalizedItemName(DefaultBoxName, DefaultBoxName);

        string itemName;
        if (string.IsNullOrEmpty(DropCharacterName))
        {
            itemName = localizedBoxName;
        }
        else
        {
            itemName = HRGameInstance.GetLocalizedPossessive(
                DropCharacterName, localizedBoxName, $"{localizedBoxName} ({DropCharacterName})");
        }

        if (OwningWeapon)
        {
            OwningWeapon.ItemName = itemName;
            OwningWeapon.UpdateNameAndValue();
            OwningWeapon.OwningInteractable.SetInteractionName(itemName);
        }

        if (NameText)
        {
            var originalNameText = NameText.text;
            NameText.text = DropCharacterName;
            if (NameText == null)
            {
                NameText.text = originalNameText;
            }
        }
    }

    #region SaveLoad
    public void HandleLoaded()
    {
        if(!string.IsNullOrEmpty(SavedDropCharacterName))
        {
            DropCharacterName = SavedDropCharacterName;
        }
    }

    public void HandlePreSave()
    {
        SavedDropCharacterName = DropCharacterName;
    }

    public void HandleReset()
    {
    }

    public void HandleSaveComponentInitialize(HRSaveComponent InSaveComponent, int ComponentID, int AuxIndex)
    {
    }

    public void HandleSaved()
    {
    }

    public bool IsSaveDirty()
    {
        return true;
    }
    #endregion
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BaseScripts;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using System.Linq;
using TMPro;
using DG.Tweening;
using UnityEngine.Localization;
using UnityEngine.AddressableAssets;

[Ceras.SerializedType]
public class HRPlayerCustomizationUI : MonoBehaviour, IHRSaveable
{
    [System.Serializable]
    public struct CustomizationToggle
    {
        public CustomizationTab Tab;
        public CustomizationTabType TabType;
        public Toggle Toggle;
        public string TabName;
        public bool bUseClothingType;
        public bool bShowColor;
        public Vector2 GridSize;
        public BaseClothingDatabase.BaseClothingType[] LinkedClothingTypes;
    }

    [System.Serializable]
    private struct BodyTypeUIData
    {
        public BaseBodyType BodyType;
        public Sprite Icon;
    }

    [System.Serializable]
    private struct VoiceTypeUIData
    {
        public string DisplayName;
        public string VoiceTypeID;
        public Sprite Icon;
    }

    private const string DEFAULT_CHARACTER_NAME = "AIRSTRAFER";

    public BasePopupUI CustomizationPopup;
    public BasePopupUI SelectionPopup;
    public BasePopupUI OKPopup;
    public BasePopupUI YesNoPopup;
    public BasePopupUI ImportPopup;
    public BasePopupUI RenamePopup;
    public GameObject BackButton;
    public TMP_InputField ImportField;
    public TMP_InputField RenameField;
    public enum TabType { BodyType, SkinColor, ClothingType, Item, Color }
    public enum CustomizationTab { BodyType, Hat, Eyes, Eyebrows, Hair, FacialHair, Accessories, Voice, DTop, DSTop, DBottom, DShoes, DRGlove, DLGlove, SkinColor, Item }
    public enum CustomizationTabType { Body, Hair, Outfit, Accessory, Item }

    bool bIsCustomizing;

    [Header("Camera Values")]
    BasePlayerCamera PlayerCamera;
    public Camera UICamera;
    public float DefaultZoomAmount = 3f;
    public float MouseRotationRate = 1;
    public float MaxZoomAmount;
    public float MinZoomAmount;
    public float ZoomHeightOffset;
    public AnimationCurve ZoomHeightCurve;
    public float MinCameraHeightOffset;
    public float MaxCameraHeightOffset;
    public float VerticalCameraMovementRate;
    float CurrentCameraHeightOffset;
    Vector3 OriginalCameraPosition;
    public float ZoomTickInterval;
    float CurrentZoomAmount;
    public GameObject CameraOutput;
    bool bRotating = false;

    [Header("Core")]
    public bool bUseLegacyGeneration = false;
    public BaseCustomizationSystem CustomizationSystem;
    public HeroPlayerCharacter CharacterToCustomize;
    public GameObject MainPanel;
    public GameObject ColorPanel;
    public GameObject HSVPanel;
    public GameObject InventoryPanel;
    public GameObject ColorwayArea;
    public GameObject HSVArea;
    public List<HSVPicker.ColorPicker> HSVColorPickers;
    public Slider HSVExposureSlider;
    public List<GameObject> PanelBG = new List<GameObject>();
    public RectTransform SelectionButtonArea;
    public RectTransform ColorButtonArea;
    public HRPlayerCustomizationPageSelectButton DefaultPage;
    public List<HRPlayerCustomizationPageSelectButton> PageSelect = new List<HRPlayerCustomizationPageSelectButton>();
    private List<Toggle> TogglesInUse = new List<Toggle>();
    [SerializeField]
    private List<CustomizationToggle> ToggleToTab = new List<CustomizationToggle>();
    [SerializeField]
    private HRPlayerCustomizationUIButton ItemButtonPrefab;
    [SerializeField]
    private int ButtonPrefabPoolSize = 100;
    private List<HRPlayerCustomizationUIButton> ButtonPrefabPool;
    [SerializeField]
    private HRPlayerCustomizationUIButton ColorButtonPrefab;
    [SerializeField]
    private int ColorButtonPrefabPoolSize = 25;
    private List<HRPlayerCustomizationUIButton> ColorButtonPrefabPool;
    [SerializeField]
    private TextMeshProUGUI TabLabel;
    [SerializeField]
    private TextMeshProUGUI ItemNameLabel;
    [SerializeField]
    private TextMeshProUGUI ColorNameLabel;
    public int CurrentCharacterID { get; set; }
    public int SelectedCharacterID { get; set; }
    public string CurrentCharacterName { get; set; }
    public string OriginalCharacterCode { get; set; }
    public string SelectedCharacterCode { get; set; }
    public string DefaultCode { get; set; }
    private bool bOpenCharacterSelectOnClose = false;
    private CustomizationTabType CurrentTabType = CustomizationTabType.Body;

    [Header("Selection")]
    public TMP_InputField CharacterNameInput;
    public Button NextButton;
    public Button PreviousButton;
    public Sprite DefaultButtonSprite;
    public List<BaseClothingDatabase.BaseClothingType> ClothingTypesToModify = new List<BaseClothingDatabase.BaseClothingType>();
    private Dictionary<CustomizationTab, HRPlayerCustomizationUIButton> LastSelection = new Dictionary<CustomizationTab, HRPlayerCustomizationUIButton>();
    private HRPlayerCustomizationUIButton LastColorSelection;

    private List<GameObject> CustomizationListItems = new List<GameObject>();

    [Header("Body Type")]
    [SerializeField]
    private List<BodyTypeUIData> BodyTypeData;

    [Header("Voice Type")]
    [SerializeField]
    private List<VoiceTypeUIData> VoiceTypeDatas;

    [Header("Display Data")]
    [SerializeField]
    private bool bUseNames = false;
    public List<BaseClothingDatabase.BaseClothingType> DisallowNone = new List<BaseClothingDatabase.BaseClothingType>();

    public ListFilterDatabase DefaultFilters;
    // The filters list is cleared every time the menu is closed.
    [HideInInspector]
    public List<ListFilter> TabFilters = new List<ListFilter>();
    public Dictionary<BaseBodyType, Dictionary<BaseClothingDatabase.BaseClothingType, List<ListFilter>>> ClothingFilters =
        new Dictionary<BaseBodyType, Dictionary<BaseClothingDatabase.BaseClothingType, List<ListFilter>>>();
    public Dictionary<GameObject, List<ListFilter>> ColorwayFilters =
        new Dictionary<GameObject, List<ListFilter>>();
    public Dictionary<AssetReference, List<ListFilter>> ColorwayMeshes =
        new Dictionary<AssetReference, List<ListFilter>>();

    [Header("Presets")]
    public GameObject PresetEntryPrefab;
    public GameObject NewCharacterListEntry;
    public GameObject ItemEntryHeaderPrefab;
    public GameObject ItemEntryContentPrefab;
    public RectTransform PresetEntryRoot;
    private List<HRSaveSystem.HRCharacterSave.HRCharacter> CharactersCache;
    private List<HRPlayerCustomizationUICharacterEntry> PresetEntries = new List<HRPlayerCustomizationUICharacterEntry>();

    [Header("Toggles")]
    public GridLayoutGroup ToggleGroup;
    public bool CanChangeBody = true;
    public bool CanChangeSkin = true;
    public bool CanChangeClothingType = true;
    public bool CanChangeItem = true;
    public bool CanChangeColor = true;

    private const int TOGGLE_SIZE_REF = 220;
    private const int MAX_TOGGLE_SIZE = 60;
    private const int MIN_TOGGLE_SIZE = 30;

    // Old
    public Button PreviousTypeButton;
    public Button NextTypeButton;
    public Button PreviousItemButton;
    public Button NextItemButton;
    public Button PreviousColorButton;
    public Button NextColorButton;

    BaseClothingDatabase.BaseClothingType CurrentClothingType;
    int CurrentClothingTypeIndex;
    string CurrentItemID = "";
    bool bBodyType;

    List<BaseClothingDatabase.BaseClothingType> OrderedClothingTypes;
    public List<BaseClothingDatabase.BaseClothingType> PrefabClothingTypes;

    Vector3 LastFrameMousePosition = Vector3.zero;
    Vector3 MouseDelta = Vector3.zero;
    Quaternion OriginalPlayerRotation = Quaternion.identity;

    [System.NonSerialized, Ceras.SerializedField]
    public int SavedPlayerBodyType;
    [System.NonSerialized, Ceras.SerializedField]
    public int SavedPlayerSkinColorIndex;
    [System.NonSerialized, Ceras.SerializedField]
    public int[] SavedPlayerEquippedItemIDs;
    [System.NonSerialized, Ceras.SerializedField]
    public int[] SavedPlayerEquippedMaterialIDs;

    bool bOriginalInvincible;
    bool bOpening = false;

    CustomizationTab CurrentTab;
    bool bItemTab = false;
    int CurrentPage = 0;
    HRPlayerCustomizationUICharacterEntry DeletionTarget;

    public int customizationLayer { get; private set; }
    private int playerLayer;

    public Sprite NoneIcon;

    [Header("Starter Items")]
    public List<Image> InventorySlots;
    private List<(int, HRPlayerCustomizationUIButton)> SelectedPresetItemButtons = new List<(int, HRPlayerCustomizationUIButton)>();
    [HideInInspector]
    public List<int> PresetItemIDs;

    [Header("Localization")]
    [SerializeField] private LocalizedStringTable CustomizationLocalizedStringTable;
    [SerializeField] private BaseLocalizationLocaleValues Locale;

    [Header("Debug")]
    public GameObject DebugObject;

    public delegate void OnCharacterCountSignature(int InNumCharacters);
    public OnCharacterCountSignature OnCharacterCountChanged;

    public delegate void OnCharacterSelectReturned();
    public OnCharacterSelectReturned OnCharacterSelectReturn;

    // the barber
    private HeroPlayerCharacter npc;
    // was the customization camera turned towards the ceiling
    public bool bCameraCeiling = false;

    private void Start()
    {
        SortClothingTypes(true);
        StopCustomization(false);
        if (!CharacterToCustomize)
        {
            SetCharacterToCustomize(BaseGameInstance.Get.GetFirstPawn() as HeroPlayerCharacter);
        }

        BaseGameInstance.Get.GameManagerPreStartedDelegate += HandleLevelLoaded;
        BaseGameInstance.Get.StartLevelUnloadDelegate += HandleLevelUnloaded;

        customizationLayer = LayerMask.NameToLayer("UIPlayer");

        ButtonPrefabPool = new List<HRPlayerCustomizationUIButton>(ButtonPrefabPoolSize);

        for (int i = 0; i < ButtonPrefabPoolSize; i++)
        {
            var button = Instantiate(ItemButtonPrefab, SelectionButtonArea).GetComponent<HRPlayerCustomizationUIButton>();
            button.CustomizationUI = this;
            button.gameObject.SetActive(false);
            ButtonPrefabPool.Add(button);
        }

        ColorButtonPrefabPool = new List<HRPlayerCustomizationUIButton>(ColorButtonPrefabPoolSize);

        for (int i = 0; i < ColorButtonPrefabPoolSize; i++)
        {
            var button = Instantiate(ColorButtonPrefab, ColorButtonArea).GetComponent<HRPlayerCustomizationUIButton>();
            button.CustomizationUI = this;
            button.bColor = true;
            button.gameObject.SetActive(false);
            ColorButtonPrefabPool.Add(button);
        }

        ImportPopup.OnPopupClosedDelegate += OnAttemptImportCharacter;
        RenamePopup.OnPopupClosedDelegate += OnAttemptRenameCharacter;
    }

    // getter for whether the player is currently customizing
    public bool IsCustomizing()
    {
        return bIsCustomizing;
    }

    public void ToggleClicked(Toggle InToggle)
    {
        if (!InToggle.isOn)
        {
            return;
        }

        foreach (var toggle in ToggleToTab)
        {
            if (toggle.Toggle == InToggle)
            {
                CurrentTab = toggle.Tab;
                TabLabel.text = toggle.TabName;

                if (toggle.bShowColor)
                {
                    ColorPanel.transform.DOScale(Vector3.one, 0.2f).SetEase(Ease.OutExpo);
                }
                else
                {
                    ColorPanel.transform.DOScale(Vector3.zero, 0.2f).SetEase(Ease.OutExpo);
                }

                OpenToTab();
            }
        }
    }

    public void OpenToTab()
    {
        if (bUseLegacyGeneration)
        {
            ReturnButtonsToPool();

            foreach (var toggle in ToggleToTab)
            {
                if (toggle.Tab == CurrentTab && toggle.bUseClothingType)
                {
                    bItemTab = true;

                    TabLabel.text = toggle.TabName;

                    foreach (var clothingType in toggle.LinkedClothingTypes)
                    {
                        if (clothingType != BaseClothingDatabase.BaseClothingType.Eyes)
                            GenerateItemSelectButtons(clothingType, toggle.Tab);
                        else
                        {

                        }
                    }

                    return;
                }
            }

            bItemTab = false;

            switch (CurrentTab)
            {
                case CustomizationTab.BodyType:
                    GenerateBodyListButtons();
                    break;
                case CustomizationTab.SkinColor:
                    GenerateSkinColorListButtons(CustomizationSystem.Target.BodyType);
                    break;
                case CustomizationTab.Voice:
                    GenerateVoiceListButtons();
                    break;
            }
        }
        else
        {
            ColorPanel.SetActive(false);
            HSVPanel.SetActive(false);
            InventoryPanel.SetActive(false);
            SelectionButtonArea.anchoredPosition = Vector2.zero;

            for (int i = 0; i < CustomizationListItems.Count; ++i)
            {
                Destroy(CustomizationListItems[i]);
            }

            CustomizationListItems.Clear();

            foreach (var toggle in ToggleToTab)
            {
                if (toggle.TabType == CurrentTabType)
                {
                    if (CurrentTabType != CustomizationTabType.Item)
                    {
                        var Header = Instantiate(ItemEntryHeaderPrefab, SelectionButtonArea);
                        var label = Header.GetComponentInChildren<TextMeshProUGUI>();
                        BaseLocalizationHandler.SetTextLocalized(this, CustomizationLocalizedStringTable, "character_header_" + toggle.TabName.Replace(" ", "").ToLower(),
                            label);

                        CustomizationListItems.Add(Header);

                        var Content = Instantiate(ItemEntryContentPrefab, SelectionButtonArea).GetComponent<HRPlayerCustomizationUIPlayerItemList>();
                        Content.Initialize(this, toggle);
                        Content.GenerateButtons();

                        CustomizationListItems.Add(Content.gameObject);
                    }
                    else
                    {
                        InventoryPanel.SetActive(true);

                        var AvailableItems = (HRGameInstance.Get as HRGameInstance).DLCManager.AvailableItems;

                        foreach (var ItemDB in AvailableItems.DatabaseWeapons)
                        {
                            if (ItemDB.IsDatabaseUnlocked())
                            {
                                var Header = Instantiate(ItemEntryHeaderPrefab, SelectionButtonArea);
                                var label = Header.GetComponentInChildren<TextMeshProUGUI>();
                                BaseLocalizationHandler.SetTextLocalized(this, CustomizationLocalizedStringTable, "character_header_" + ItemDB.DatabaseName.Replace(" ", "").ToLower(),
                                    label);

                                CustomizationListItems.Add(Header);

                                var Content = Instantiate(ItemEntryContentPrefab, SelectionButtonArea).GetComponent<HRPlayerCustomizationUIPlayerItemList>();
                                Content.Initialize(this, toggle);
                                Content.ItemDatabase = ItemDB;
                                Content.GenerateButtons();
                                foreach (HRPlayerCustomizationUIButton button in Content.GetComponentsInChildren<HRPlayerCustomizationUIButton>()) {
                                    bool selected = false;
                                    for(int i = 0; i < SelectedPresetItemButtons.Count; i++)
                                    {
                                        if (SelectedPresetItemButtons[i].Item1 == button.Index)
                                        {
                                            SelectedPresetItemButtons[i] = (button.Index, button);
                                            selected = true;
                                        }
                                    }
                                    button.SetSelected(selected);
                                }
                                RegenerateItemNames();
                                CustomizationListItems.Add(Content.gameObject);
                            }
                        }
                    }
                }
            }
        }
    }

    public void ReturnButtonsToPool()
    {
        for (int i = 0; i < ButtonPrefabPool.Count; i++)
        {
            ButtonPrefabPool[i].gameObject.SetActive(false);
            ButtonPrefabPool[i].SetSelected(false);
        }
    }

    public void GenerateBodyListButtons(GameObject prefab = null, Transform root = null, bool bUsePool = true)
    {
        for (int i = 0; i < BodyTypeData.Count; i++)
        {
            var Target = prefab != null ? Instantiate(prefab, root).GetComponent<HRPlayerCustomizationUIButton>() : ButtonPrefabPool[i];

            if (prefab != null)
            {
                Target.CustomizationUI = this;
                Target.bUseTab = false;
                Target.bUseLastSelect = true;
                Target.OwningTab = CustomizationTab.BodyType;
            }

            Target.gameObject.SetActive(true);
            Target.GetComponent<Button>().interactable = true;
            Target.ButtonIcon.sprite = BodyTypeData[i].Icon;
            Target.ButtonIcon.preserveAspect = true;
            BaseLocalizationHandler.SetTextLocalized(this, CustomizationLocalizedStringTable,
                "character_item_" + BodyTypeData[i].BodyType.ToString().Replace("Hero", "").Replace(" ", "").ToLower(),
                Target.ButtonText);
            Target.ItemName = BodyTypeData[i].BodyType.ToString();
            Target.Index = i;

            if (BodyTypeData[i].BodyType == CustomizationSystem.Target.BodyType)
            {
                if (LastSelection.ContainsKey(CustomizationTab.BodyType))
                {
                    LastSelection[CustomizationTab.BodyType] = Target;
                }
                else
                {
                    LastSelection.Add(CustomizationTab.BodyType, Target);
                }

                Target.SetSelected(true);
                ItemNameLabel.text = BodyTypeData[i].BodyType.ToString();
            }
        }

        for (int i = 0; i < ColorButtonPrefabPool.Count; i++)
        {
            ColorButtonPrefabPool[i].gameObject.SetActive(false);
            ColorButtonPrefabPool[i].SetSelected(false);
        }

        ColorNameLabel.text = "";
    }

    public void GenerateSkinColorListButtons(BaseBodyType BodyType, GameObject prefab = null, Transform root = null, bool bUsePool = true)
    {
        var BodyTypeData = CustomizationSystem.ClothingDatabase.GetBodyTypeData(BodyType);
        if (BodyTypeData != null)
        {
            for (int i = 0; i < ButtonPrefabPool.Count; i++)
            {
                ButtonPrefabPool[i].gameObject.SetActive(false);
                ButtonPrefabPool[i].SetSelected(false);
            }

            if (BodyTypeData.Data.SkinColors.Length == 0)
            {
                return;
            }

            int NumButtons = BodyTypeData.Data.SkinColors.Length;
            int EquippedSkinColor = CustomizationSystem.Target.CurrentSkinColorIndex;

            for (int i = 0; i < NumButtons; ++i)
            {
                var Target = prefab != null ? Instantiate(prefab, root).GetComponent<HRPlayerCustomizationUIButton>() : ButtonPrefabPool[i];

                if (prefab != null)
                {
                    Target.CustomizationUI = this;
                    Target.bUseTab = false;
                    Target.bUseLastSelect = true;
                    Target.OwningTab = CustomizationTab.SkinColor;
                }

                Target.gameObject.SetActive(true);
                Target.ButtonText.SetText("");
                Target.Index = i;
                Target.StringValue = BodyTypeData.Data.SkinColors[i].Name;
                Target.ItemName = BodyTypeData.Data.SkinColors[i].Name;
                Target.ButtonIcon.sprite = BodyTypeData.Data.SkinColors[i].Icon;

                if (BodyTypeData.Data.SkinColors[i].Icon == null)
                {
                    Target.ButtonText.SetText(BodyTypeData.Data.SkinColors[i].Name);
                }

                // Set current selection
                if (EquippedSkinColor == i)
                {
                    if (LastSelection.ContainsKey(CustomizationTab.SkinColor))
                    {
                        LastSelection[CustomizationTab.SkinColor] = Target;
                    }
                    else
                    {
                        LastSelection.Add(CustomizationTab.SkinColor, Target);
                    }

                    Target.SetSelected(true);
                    ColorNameLabel.text = BodyTypeData.Data.SkinColors[i].Name;
                }
            }
        }
    }

    public void GenerateVoiceListButtons(GameObject prefab = null, Transform root = null, bool bUsePool = true)
    {
        for (int i = 0; i < VoiceTypeDatas.Count; ++i)
        {
            var Target = prefab != null ? Instantiate(prefab, root).GetComponent<HRPlayerCustomizationUIButton>() : ButtonPrefabPool[i];

            if (prefab != null)
            {
                Target.CustomizationUI = this;
                Target.bUseTab = false;
                Target.bUseLastSelect = true;
                Target.OwningTab = CustomizationTab.Voice;
                Target.GetComponent<BaseButtonListener>().OnPointUpEvent = new UnityEngine.Events.UnityEvent();
                Target.GetComponent<BaseButtonListener>().OnPointDownEvent = new UnityEngine.Events.UnityEvent();
            }

            Target.gameObject.SetActive(true);
            Target.GetComponent<Button>().interactable = true;
            Target.ButtonIcon.sprite = VoiceTypeDatas[i].Icon;
            Target.ButtonIcon.preserveAspect = true;
            BaseLocalizationHandler.SetTextLocalized(this, CustomizationLocalizedStringTable,
                "character_item_" + VoiceTypeDatas[i].DisplayName.Replace(" ", "").ToLower(),
                Target.ButtonText);
            Target.ItemName = VoiceTypeDatas[i].DisplayName;
            Target.Index = i;

            if (VoiceTypeDatas[i].VoiceTypeID == CustomizationSystem.Target.CharacterVoice.VoiceType)
            {
                if (LastSelection.ContainsKey(CustomizationTab.Voice))
                {
                    LastSelection[CustomizationTab.Voice] = Target;
                }
                else
                {
                    LastSelection.Add(CustomizationTab.Voice, Target);
                }

                Target.SetSelected(true);
                ItemNameLabel.text = VoiceTypeDatas[i].DisplayName;
            }
        }

        for (int i = 0; i < ColorButtonPrefabPool.Count; i++)
        {
            ColorButtonPrefabPool[i].gameObject.SetActive(false);
            ColorButtonPrefabPool[i].SetSelected(false);
        }

        ColorNameLabel.text = "";
    }

    public void GenerateStartItemButtons(HRDLCItemDB ItemDatabase, GameObject prefab = null, Transform root = null, bool bUsePool = true)
    {
        for (int i = 0; i < ItemDatabase.DatabaseWeapons.Count; ++i)
        {
            var Target = prefab != null ? Instantiate(prefab, root).GetComponent<HRPlayerCustomizationUIButton>() : ButtonPrefabPool[i];

            if (prefab != null)
            {
                Target.CustomizationUI = this;
                Target.bUseTab = false;
                Target.bUseLastSelect = false;
                Target.OwningTab = CustomizationTab.Item;
                Target.GetComponent<BaseButtonListener>().OnPointUpEvent = new UnityEngine.Events.UnityEvent();
                Target.GetComponent<BaseButtonListener>().OnPointDownEvent = new UnityEngine.Events.UnityEvent();
            }

            Target.gameObject.SetActive(true);
            Target.GetComponent<Button>().interactable = true;
            Target.ButtonIcon.sprite = ItemDatabase.DatabaseWeapons[i].WeaponSprite;
            Target.ButtonIcon.preserveAspect = true;
            int selectedIndex = -1;
            for(int j = 0; j < SelectedPresetItemButtons.Count; j++)
            {
                if (SelectedPresetItemButtons[j].Item1 == ItemDatabase.DatabaseWeapons[j].ItemID) {
                    selectedIndex = j;
                    break;
                }
            }
            if (selectedIndex != -1)
            {
                Target.ButtonText.SetText((selectedIndex + 1).ToString());
                Target.SetSelected(true);
            }
            else
            {
                Target.ButtonText.SetText(ItemDatabase.DatabaseWeapons[i].ItemName);
            }

            Target.ItemName = ItemDatabase.DatabaseWeapons[i].ItemName;
            Target.Index = ItemDatabase.DatabaseWeapons[i].ItemID;
        }
    }

    public void GenerateItemSelectButtons(BaseClothingDatabase.BaseClothingType ClothingType, CustomizationTab Tab,
        GameObject prefab = null, Transform root = null, bool bUsePool = true)
    {
        if (ClothingType == BaseClothingDatabase.BaseClothingType.Eyes)
        {
            if (!CustomizationSystem.Target.RendererData[(int)ClothingType].HasEquipped())
            {
                CustomizationSystem.Target.RendererData[(int)ClothingType].ItemID = "default_eyes";
            }
            GenerateEyeColorSelectButtons();
            return;
        }
        if (!CustomizationSystem.ClothingDatabase.ClothingItems.ContainsKey(ClothingType))
        {
            Debug.LogError($"NOTE: { ClothingType.ToString() } does not exist in the database.");
            return;
        }

        var Items = CustomizationSystem.ClothingDatabase.ClothingItems[ClothingType];
        var ItemList = new List<HROutfitPieceMesh>();
        bool bPrefabsOnly = PrefabClothingTypes.Contains(ClothingType);

        foreach (var Item in Items.Values)
        {
            if (Item.WeaponPrefab != null || !bPrefabsOnly)
            {
                ItemList.Add(Item);
            }
        }

        var AvailableItems = (HRGameInstance.Get as HRGameInstance).DLCManager.AvailableItems;

        foreach (var Database in AvailableItems.DatabaseWeapons)
        {
            if (!Database.IsDatabaseUnlocked())
            {
                foreach (var Item in Database.DatabaseClothingItems)
                {
                    if (Item.ClothingType == ClothingType)
                    {
                        //Debug.LogError("Removing ID~" + Item.ItemID);
                        var inst = ItemList.FindIndex(0, (i) => i.ID == Item.ItemID);

                        if (inst != -1)
                        {
                            ItemList.RemoveAt(inst);
                        }
                    }
                }
            }
        }

        if (ClothingFilters.ContainsKey(CustomizationSystem.Target.BodyType) &&
            ClothingFilters[CustomizationSystem.Target.BodyType].ContainsKey(ClothingType) && !BaseGameInstance.Get.bDebugMode)
        {
            ItemList = ListFilter.ApplyFilters(ItemList, ClothingFilters[CustomizationSystem.Target.BodyType][ClothingType]) as List<HROutfitPieceMesh>;
        }

        if (!DisallowNone.Contains(ClothingType))
        {
            ItemList.Insert(0, new HROutfitPieceMesh()
            {
                Name = "None",
                ID = ""
            });
        }

        int NumButtons = bUsePool ? Mathf.Min(ItemList.Count, ButtonPrefabPool.Count) : ItemList.Count;

        string equippedItem = CustomizationSystem.Target.GetCurrentEquippedItem(ClothingType);

        ItemNameLabel.text = "";
        ColorNameLabel.text = "";

        for (int i = 0; i < NumButtons; i++)
        {
            var Item = ItemList.ElementAt(i);

            var Target = prefab != null ? Instantiate(prefab, root).GetComponent<HRPlayerCustomizationUIButton>() : ButtonPrefabPool[i];

            if (prefab != null)
            {
                Target.CustomizationUI = this;
                Target.bUseTab = false;
                Target.bItemTab = true;
                Target.bColor = false;
                Target.bUseLastSelect = true;
                Target.OwningTab = Tab;
            }

            Target.gameObject.SetActive(true);

            if (bUseNames)
            {
                if (Item.WeaponPrefab)
                {
                    var WeaponData = Item.WeaponPrefab.GetComponent<BaseWeapon>();

                    if (WeaponData)
                        Target.ButtonText.SetText(HRItemDatabase.GetLocalizedItemName(WeaponData.ItemName.Replace(" ", "").ToLower(), Item.Name));
                    else
                        Target.ButtonText.SetText(HRItemDatabase.GetLocalizedItemName(Item.ID.Replace(" ", "").Replace("-", "").ToLower(), Item.Name));
                }
                else
                {
                    Target.ButtonText.SetText(HRItemDatabase.GetLocalizedItemName(Item.ID.Replace(" ", "").Replace("-", "").Replace("_", "").ToLower(), Item.Name));
                }
            }
            else
            {
                Target.ButtonText.SetText("");
            }
            Target.Index = i;
            Target.StringValue = Item.ID;
            Target.ItemName = Item.Name;
            Target.ClothingType = ClothingType;
            Target.bUseHSV = Item.bUseHSVSelect;
            Target.ActiveHSVColorPickers = new bool[]
            {
                Item.bUsePrimaryColor,
                Item.bUseSecondaryColor,
                Item.bUseAccessoryColor,
            };
            Target.HSVIndex = Item.HSVIndex;

            if (Item.WeaponPrefab != null)
            {
                Target.GetComponent<Button>().interactable = true;
                var Weapon = Item.WeaponPrefab.GetComponent<BaseWeapon>();

                if (Weapon != null)
                {
                    Target.ButtonIcon.sprite = Weapon.WeaponSprite;
                }
                else
                {
                    Target.ButtonIcon.sprite = DefaultButtonSprite;
                }

            }
            else
            {
                Target.GetComponent<Button>().interactable = !(bPrefabsOnly && Item.Name != "None");
                Sprite icon = DefaultButtonSprite;
                if (Item != null)
                {
                    if (Item.Name == "None")
                    {
                        icon = NoneIcon;
                    }
                    else
                    {
                        icon = Item.GetFirstIcon() ?? DefaultButtonSprite;
                    }
                    if (icon != DefaultButtonSprite)
                    {
                        Target.ButtonText.SetText("");
                    }
                }
                Target.ButtonIcon.sprite = icon;

                if (!Target.GetComponent<Button>().interactable)
                {
                    Debug.LogError($"NOTE: { Item.Name } does not have a prefab and will not be selectable.");
                }
            }

            if (equippedItem == Item.ID)
            {
                if (LastSelection.ContainsKey(Tab))
                {
                    LastSelection[Tab] = Target;
                }
                else
                {
                    LastSelection.Add(Tab, Target);
                }

                Target.SetSelected(true);
                ItemNameLabel.text = Item.Name;
                CurrentItemID = Item.ID;

                // Generate Color Select
                //GenerateColorSelectButtons(Item.ID, ClothingType);
            }
        }
    }

    public void GenerateEyeColorSelectButtons(GameObject prefab = null, Transform root = null, bool bUsePool = true)
    {
        ItemNameLabel.text = "None";

        BaseFaceDetailMaterials faceDetail = CustomizationSystem.ClothingDatabase.GetBodyTypeData(CustomizationSystem.Target.BodyType).Data.GetFirstFaceDetail();
        if (faceDetail != null && faceDetail.EyeMaterials)
        {
            for (int i = 0; i < ColorButtonPrefabPool.Count; i++)
            {
                ColorButtonPrefabPool[i].gameObject.SetActive(false);
                ColorButtonPrefabPool[i].SetSelected(false);
            }

            int NumButtons = faceDetail.EyeMaterials.Colorways.Length;

            string equippedColorway = CustomizationSystem.Target.GetCurrentEquippedColorway(BaseClothingDatabase.BaseClothingType.Eyes);

            for (int i = 0; i < NumButtons; i++)
            {
                // TEMP
                if (faceDetail.EyeMaterials.Colorways[i].ID.Contains("small") || faceDetail.EyeMaterials.Colorways[i].ID.Contains("large"))
                {
                    continue;
                }

                var Target = prefab != null ? Instantiate(prefab, root).GetComponent<HRPlayerCustomizationUIButton>() : ColorButtonPrefabPool[i];

                if (prefab != null)
                {
                    Target.CustomizationUI = this;
                    Target.bUseTab = false;
                    Target.bItemTab = true;
                    Target.bColor = true;
                    Target.bUseLastSelect = true;
                    Target.OwningTab = CustomizationTab.Eyes;
                }

                Target.gameObject.SetActive(true);
                Target.ButtonText.SetText(faceDetail.EyeMaterials.Colorways[i].Name);
                Target.Index = i;
                Target.StringValue = faceDetail.EyeMaterials.Colorways[i].ID;
                Target.ItemName = faceDetail.EyeMaterials.Colorways[i].Name;
                Target.ClothingType = BaseClothingDatabase.BaseClothingType.Eyes;
                Target.ButtonIcon.sprite = faceDetail.EyeMaterials.Colorways[i].Icon;

                if (faceDetail.EyeMaterials.Colorways[i].Icon == null)
                {
                    Target.ButtonText.SetText(faceDetail.EyeMaterials.Colorways[i].Name);
                }

                // Set current selection
                if (equippedColorway == faceDetail.EyeMaterials.Colorways[i].ID)
                {
                    if (LastSelection.ContainsKey(CustomizationTab.Eyes))
                    {
                        LastSelection[CustomizationTab.Eyes] = Target;
                    }
                    else
                    {
                        LastSelection.Add(CustomizationTab.Eyes, Target);
                    }

                    Target.SetSelected(true);
                    ColorNameLabel.text = faceDetail.EyeMaterials.Colorways[i].Name;
                }
            }
        }
    }

    public void GenerateColorSelectButtons(string ItemID, BaseClothingDatabase.BaseClothingType ClothingType, CustomizationTab Tab)
    {
        var OutfitPiece = CustomizationSystem.ClothingDatabase.GetOutfitPieceByID_Slow(ItemID);
        if (OutfitPiece != null)
        {
            if (OutfitPiece.Colorways.Length == 0)
            {
                //Debug.Log($"No Colorways for item ID {CurrentItemID}");
                ColorPanel.SetActive(false);
                HSVPanel.SetActive(false);
                return;
            }

            for (int i = 0; i < ColorButtonPrefabPool.Count; i++)
            {
                ColorButtonPrefabPool[i].gameObject.SetActive(false);
                ColorButtonPrefabPool[i].SetSelected(false);
            }

            var ColorwayList = OutfitPiece.Colorways.ToList();

            if (OutfitPiece.WeaponPrefab != null && ColorwayFilters.ContainsKey(OutfitPiece.WeaponPrefab) && !BaseGameInstance.Get.bDebugMode)
            {
                ColorwayList = ListFilter.ApplyFilters(ColorwayList, ColorwayFilters[OutfitPiece.WeaponPrefab]) as List<HROutfitItemColorwayData>;
            }
            else
            {
                for (int i = 0; i < OutfitPiece.Meshes.Length; i++)
                {
                    if (!BaseGameInstance.Get.bDebugMode && ColorwayMeshes != null && OutfitPiece != null && OutfitPiece.Meshes[i].meshAssetReference != null &&
                        ColorwayMeshes.ContainsKey(OutfitPiece.Meshes[i].meshAssetReference))
                    {
                        ColorwayList = ListFilter.ApplyFilters(ColorwayList, ColorwayMeshes[OutfitPiece.Meshes[i].meshAssetReference]) as List<HROutfitItemColorwayData>;
                        break;
                    }
                }
            }

            int NumButtons = ColorwayList.Count;

            string equippedColorway = CustomizationSystem.Target.GetCurrentEquippedColorway(ClothingType);

            ColorNameLabel.text = "";

            if (NumButtons == 0)
            {
                ColorPanel.SetActive(false);
                HSVPanel.SetActive(false);
            }

            for (int i = 0; i < NumButtons; i++)
            {
                ColorButtonPrefabPool[i].bColor = true;
                ColorButtonPrefabPool[i].bItemTab = true;
                ColorButtonPrefabPool[i].bUseTab = false;
                ColorButtonPrefabPool[i].gameObject.SetActive(true);
                ColorButtonPrefabPool[i].ButtonText.SetText("");
                ColorButtonPrefabPool[i].Index = i;
                ColorButtonPrefabPool[i].StringValue = ColorwayList[i].ID;
                ColorButtonPrefabPool[i].ItemName = ColorwayList[i].Name;
                ColorButtonPrefabPool[i].ClothingType = ClothingType;
                ColorButtonPrefabPool[i].OwningTab = Tab;
                ColorButtonPrefabPool[i].ButtonIcon.sprite = ColorwayList[i].Icon;

                if (ColorwayList[i].Icon == null)
                {
                    ColorButtonPrefabPool[i].ButtonText.SetText(ColorwayList[i].Name);
                }

                // Set current selection
                if (equippedColorway == ColorwayList[i].ID)
                {
                    LastColorSelection = ColorButtonPrefabPool[i];
                    ColorButtonPrefabPool[i].SetSelected(true);
                    ColorNameLabel.text = ColorwayList[i].Name;
                }
            }
        }
        else
        {
            ColorNameLabel.text = "";

            for (int i = 0; i < ColorButtonPrefabPool.Count; i++)
            {
                ColorButtonPrefabPool[i].gameObject.SetActive(false);
                ColorButtonPrefabPool[i].SetSelected(false);
            }
        }
    }

    public void GenerateCharacterEntries()
    {
        for (int i = 0; i < PresetEntries.Count; i++)
        {
            if (PresetEntries[i] != null)
            {
                Destroy(PresetEntries[i].gameObject);
            }
        }

        PresetEntries.Clear();

        foreach (var Character in CharactersCache)
        {
            if (Character.bActive)
            {
                CurrentCharacterID = Character.ID;
                CurrentCharacterName = Character.CharacterName;

                SelectedPresetItemButtons.Clear();

                int Index = Character.StarterWeapons != null ? Character.StarterWeapons.Count : 0;

                for (int i = 0; i < Index; i++)
                {
                    //PresetItemIDs.Add(Character.StarterWeapons[i]);
                }

                for (int i = Index; i < 6; i++)
                {
                    //PresetItemIDs.Add(-1);
                }
            }

            var Entry = Instantiate(PresetEntryPrefab, PresetEntryRoot).GetComponent<HRPlayerCustomizationUICharacterEntry>();

            if (Entry)
            {
                if (Character.bActive)
                {
                    SelectedCharacterID = Character.ID;
                    SelectedCharacterCode = Character.CharacterCode;
                }

                Entry.Initialize(this, Character.ID, Character.CharacterName, Character.LastAccessedTime,
                    Character.CharacterCode, Character.StarterWeapons, Character.bActive, Character.CharacterLevel);
            }

            PresetEntries.Add(Entry);
        }

        NewCharacterListEntry.transform.SetAsLastSibling();
    }


    public void SetCurrentTabType(int i)
    {
        var Previous = CurrentTabType;
        CurrentTabType = (CustomizationTabType)i;

        if (CurrentTabType == CustomizationTabType.Accessory)
        {
            if (Previous != CustomizationTabType.Accessory)
            {
                TweenZoomAmount(2.4f);
            }
        }
        else
        {
            if (Previous == CustomizationTabType.Accessory)
            {
                TweenZoomAmount(1.1f);
            }
        }

        OpenToTab();
    }

    public void RefreshCharacterEntries(int ActiveID)
    {
        for (int i = 0; i < PresetEntries.Count; i++)
        {
            if (PresetEntries[i] != null)
            {
                PresetEntries[i].SetHighlighted(PresetEntries[i].SaveID == ActiveID);
            }
        }
    }

    public void AttemptDeleteCharacter(HRPlayerCustomizationUICharacterEntry EntryToDelete)
    {
        DeletionTarget = EntryToDelete;

        YesNoPopup.OpenPopup();

        Locale.CharacterName1 = EntryToDelete.CharacterName.text;

        BaseLocalizationHandler.SetTextLocalizedInterp(this, CustomizationLocalizedStringTable, "character_delete", Locale, YesNoPopup.PopupText);
        YesNoPopup.OnPopupClosedDelegate += OnDeleteCharacter;
    }


    public void OnDeleteCharacter(BasePopupUI InPopupUI, int InResult)
    {
        YesNoPopup.OnPopupClosedDelegate -= OnDeleteCharacter;

        if (InResult > 0)
        {
            DeleteCharacter();
        }
    }

    public void DeleteCharacter()
    {
        CustomizationSystem.Target.DeleteCharacterCode(HRNetworkManager.Get.LocalPlayerController, DeletionTarget.SaveID, DeletionTarget.CharacterName.text);
        CustomizationSystem.Target.CharacterDeleteDelegate += OnCharacterDelete;
        //Popup.ButtonTwoDelegate -= DeleteCharacter;

        PresetEntries.Remove(DeletionTarget);
        Destroy(DeletionTarget.gameObject);
    }

    private void OnCharacterDelete(BaseCustomizationComponent Source, string CharacterName)
    {
        Source.CharacterDeleteDelegate -= OnCharacterDelete;

        OKPopup.OpenPopup();
        Locale.CharacterName1 = CharacterName;
        BaseLocalizationHandler.SetTextLocalizedInterp(this, CustomizationLocalizedStringTable, "character_on_deleted", Locale, OKPopup.PopupText);
        UpdateCharacterCount();

        if (PresetEntries.Count > 0)
        {
            CustomizationSystem.Target.SetActiveCharacter(PresetEntries[0].SaveID);
            RefreshCharacterEntries(PresetEntries[0].SaveID);

            OnCharacterSelectReturn?.Invoke();
        }
    }

    public void SetCharacterToCustomize(HeroPlayerCharacter InPlayerCharacter)
    {
        if (InPlayerCharacter)
        {
            CharacterToCustomize = InPlayerCharacter.GetComponent<HeroPlayerCharacter>();
            PlayerCamera = CharacterToCustomize?.PlayerCamera;
            CustomizationSystem = InPlayerCharacter.GetComponentInChildren<BaseCustomizationSystem>();
        }
    }

    private void HandleLevelLoaded(BaseGameManager GameManager)
    {
        if (!CharacterToCustomize)
        {
            BasePawn CurrentPawn = BaseGameInstance.Get.GetFirstPawn();
            if (CurrentPawn)
            {
                CharacterToCustomize = CurrentPawn.GetComponent<HeroPlayerCharacter>();

                SetCharacterToCustomize(CharacterToCustomize);
            }
        }
    }

    private void HandleLevelUnloaded(BaseGameInstance GameInstance)
    {

    }

    private void Update()
    {
        if (CharacterToCustomize && (CharacterToCustomize.CurrentInteractState == HeroPlayerCharacter.InteractState.Dying || CharacterToCustomize.CurrentInteractState == HeroPlayerCharacter.InteractState.Dead))
        {
            if (bIsCustomizing)
            {
                StopCustomization(false);
            }

            return;
        }

        // Hot key
        if (Input.GetKey(KeyCode.F1) && HRGameInstance.Get.CurrentOnlineSubsystem.IsDevBranch())
        {
            if (Input.GetKeyDown(KeyCode.C) && !bOpening)
            {
                if (!bIsCustomizing)
                {
                    AttemptCharacterSelection();
                }
                else
                {
                    StopCustomization(false);
                }
            }
        }

        // Mouse Rotation
        if (bIsCustomizing && CharacterToCustomize && UICamera)
        {
            if (EventSystem.current.currentInputModule as BaseStandaloneInputModule)
            {
                var pointed = (EventSystem.current.currentInputModule as BaseStandaloneInputModule).GetGameObjectUnderPointer();

                if (pointed != null && pointed.gameObject == CameraOutput)
                {
                    if (Input.GetMouseButtonDown(0))
                    {
                        bRotating = true;
                    }
                    /*else if (Input.GetMouseButton(2))
                    {
                        SetHeightOffset(CurrentCameraHeightOffset - (Vector3.Dot(MouseDelta, UICamera.transform.up) * VerticalCameraMovementRate * Time.deltaTime));
                    }*/
                    if (Input.GetAxis("Mouse ScrollWheel") > 0f)
                    {
                        ZoomTick(true);
                    }
                    else if (Input.GetAxis("Mouse ScrollWheel") < 0f)
                    {
                        ZoomTick(false);
                    }
                }
            }

            if (bRotating)
            {
                MouseDelta = Input.mousePosition - LastFrameMousePosition;
                CharacterToCustomize.transform.Rotate(Vector3.up, -Vector3.Dot(MouseDelta, UICamera.transform.right) * MouseRotationRate * Time.deltaTime);
            }

            if (Input.GetMouseButtonUp(0))
            {
                bRotating = false;
            }
        }

        LastFrameMousePosition = Input.mousePosition;
    }

    public void CreateNewCharacter()
    {
        if (CustomizationSystem)
        {
            if (string.IsNullOrEmpty(DefaultCode))
            {
                DefaultCode = GetCharacterCode();
            }

            CustomizationSystem.Target.UpdateCustomizationCode(DefaultCode);
            CharacterNameInput.text = DEFAULT_CHARACTER_NAME;
            CurrentCharacterName = "";
            CurrentCharacterID = HRSaveSystem.Get.SavedCharacters.ID;

            SelectedPresetItemButtons.Clear();


            bOpenCharacterSelectOnClose = true;

            //Debug.LogError("NEW CHARACTER ID IS: " + CurrentCharacterID);

            StartCustomization();
        }
    }

    public void AttemptCharacterSelection(bool bOpen = true)
    {
        if (CustomizationSystem)
        {
            SelectionPopup.gameObject.SetActive(true);

            bOpening = bOpen;
            playerLayer = CustomizationSystem.Target.gameObject.layer;
            CustomizationSystem.Target.CharacterRequestDelegate += CharacterRequestToCSS;
            CustomizationSystem.Target.RequestCharacters(HRNetworkManager.Get.LocalPlayerController);
            if (PlayerCamera)
            {
                PlayerCamera.AddNoMouseRequest(this.gameObject);
            }
        }
    }

    public void AttemptStartCustomization()
    {
        if (CustomizationSystem)
        {
            bOpening = true;
            CustomizationSystem.Target.CharacterRequestDelegate += CharacterRequest;
            CustomizationSystem.Target.RequestCharacters(HRNetworkManager.Get.LocalPlayerController);
        }
    }


    public void UpdateCharacterCount()
    {
        CustomizationSystem.Target.CharacterRequestDelegate += UpdateCharacterCountResult;
        CustomizationSystem.Target.RequestCharacters(HRNetworkManager.Get.LocalPlayerController);
    }


    private void UpdateCharacterCountResult(BaseCustomizationComponent Source, List<HRSaveSystem.HRCharacterSave.HRCharacter> Characters)
    {
        CustomizationSystem.Target.CharacterRequestDelegate -= UpdateCharacterCountResult;
        OnCharacterCountChanged?.Invoke(Characters.Count);
    }

    public void SetLayer(Transform transform, int layer, bool bRelease = false)
    {
        if (transform.GetComponent<BaseWeapon>())
        {
            SetWeaponLayer(transform.GetComponent<BaseWeapon>(), layer, bRelease);
            return;
        }

        transform.gameObject.layer = layer;

        foreach (Transform t in transform)
        {
            var weapon = t.GetComponent<BaseWeapon>();

            if (weapon == null)
                SetLayer(t, layer, bRelease);
            else
            {
                SetWeaponLayer(weapon, layer, bRelease);
            }
        }
    }

    private void SetWeaponLayer(BaseWeapon weapon, int layer, bool bRelease = false)
    {
        if(weapon.MeshObject)
        {
            if (bRelease)
            {
                SetLayer(weapon.MeshObject.transform, weapon.IsNonPlaceable ?
                    LayerMask.NameToLayer("NonPlaceableWeapon") : LayerMask.NameToLayer("PlaceableWeapon"), bRelease);
            }
            else
            {
                SetLayer(weapon.MeshObject.transform, layer, bRelease);
            }
        }
    }

    public void TurnCameraCeiling()
    {
        // TODO: Disable dialogue camera
        npc = PixelCrushers.DialogueSystem.DialogueManager.currentActor.GetComponent<HeroPlayerCharacter>();
        npc.InteractDialogueInstance.VirtualCameraToEnable.gameObject.transform.Rotate(270, 0, 0);
        bCameraCeiling = true;
    }
    public void StartCustomization(CustomizationTab StartTab = CustomizationTab.Hair, bool showPopup = true, bool fromSeqCom = false)
    { 
        if (DebugObject)
            DebugObject.SetActive(BaseGameInstance.Get.bDebugMode);

        foreach (var bg in PanelBG)
        {
            bg.SetActive(true);
        }

        if (CustomizationSystem && CustomizationSystem.Target)
        {
            // If from sequencer command, disable back button
            if (fromSeqCom)
            {
                // Make avatar show up immediately
                CustomizationSystem.SetBodyType(CustomizationSystem.Target.BodyType, true, true, false, true);
                //
                BackButton.SetActive(false);
            }

            SelectionPopup.gameObject.SetActive(false);

            CustomizationPopup.OpenPopup();

            // We need to save the original code in case the player backs out of the menu.
            OriginalCharacterCode = CustomizationSystem.Target.SavedCustomizationCode;

            // Move the player's renderers to the UI layer 
            if (showPopup)
                SetLayer(CustomizationSystem.Target.transform, customizationLayer);

            CurrentTab = StartTab;
            CurrentTabType = CustomizationTabType.Hair;

            // Initialize camera
            if (UICamera && showPopup)
            {
                if (PlayerCamera)
                {
                    //PlayerCamera.gameObject.SetActive(false);
                }
                PlaceCamera();
                UICamera.gameObject.SetActive(true);
                if (CharacterToCustomize)
                {
                    CharacterToCustomize.MovementComponent.RotateTowards(UICamera.transform.position, false);
                    bOriginalInvincible = CharacterToCustomize.HP.bInvincible;
                    CharacterToCustomize.HP.bInvincible = true;
                }
            }

            MainPanel.SetActive(true);
            bIsCustomizing = true;
            BasePauseManager PauseManager = BaseGameInstance.Get?.PauseManager;

            // Request a pause
            if (PauseManager)
            {
                PauseManager.RequestPause(this, true, false);

                HRPlayerUI PlayerUI = ((HRPlayerController)BaseGameInstance.Get.GetFirstPawn()?.PlayerController)?.PlayerUI;
                if (PlayerUI)
                {
                    // Pausing enables the black bars which messes up the UI view. We will disable them here.
                    PlayerUI.BlackBarsUI.gameObject.SetActive(false);
                }
            }

            // Apply the filters
            if (DefaultFilters)
            {
                foreach (var filter in DefaultFilters.ListFilters)
                {
                    TabFilters.Add(filter);
                }
            }

            foreach (var pageButton in PageSelect)
            {
                pageButton.SetSelected(pageButton == DefaultPage);

                if (pageButton == DefaultPage)
                {
                    pageButton.GetComponent<Button>().onClick?.Invoke();
                }
            }

            if (TabFilters.Count > 0 && !BaseGameInstance.Get.bDebugMode)
                ListFilter.ApplyFilters(this, TabFilters);
            else
                ApplyTabFilters(null, true);

        }
        else
        {
            StopCustomization(false);
        }
    }


    private void RotateToCamera()
    {

    }


    public void ApplyTabFilters(List<CustomizationTab> TabsToUse, bool bInclude)
    {
        TogglesInUse = new List<Toggle>();
        bool bColor = false;

        if (TabsToUse != null && TabsToUse.Count == 0)
        {
            foreach (var toggle in ToggleToTab)
            {
                if (bInclude)
                {
                    if (TabsToUse.Contains(toggle.Tab))
                    {
                        if (TogglesInUse.Count == 0)
                        {
                            bColor = toggle.bShowColor;
                            TabLabel.text = toggle.TabName;
                        }

                        TogglesInUse.Add(toggle.Toggle);
                    }
                }
                else
                {
                    if (!TabsToUse.Contains(toggle.Tab))
                    {
                        if (TogglesInUse.Count == 0)
                        {
                            bColor = toggle.bShowColor;
                            TabLabel.text = toggle.TabName;
                        }

                        TogglesInUse.Add(toggle.Toggle);
                    }
                }

                toggle.Toggle.gameObject.SetActive(TabsToUse.Contains(toggle.Tab));
            }
        }
        else
        {
            foreach (var toggle in ToggleToTab)
            {
                if (toggle.Toggle == null)
                {
                    continue;
                }

                if (TogglesInUse.Count == 0 && TabLabel)
                {
                    TabLabel.text = toggle.TabName;
                }

                if (toggle.Toggle)
                {
                    toggle.Toggle.gameObject.SetActive(true);
                    TogglesInUse.Add(toggle.Toggle);
                }
            }
        }

        ColorPanel.transform.localScale = bColor ? Vector3.one : Vector3.zero;

        if (TogglesInUse.Count > 0)
        {
            TogglesInUse[0].isOn = true;
            TogglesInUse[0].onValueChanged.Invoke(true);
        }
    }


    public void AttemptStopCustomization()
    {
        if (CustomizationSystem.Target.SavedCustomizationCode != OriginalCharacterCode || CurrentCharacterName != CharacterNameInput.text)
        {
            YesNoPopup.OpenPopup();

            BaseLocalizationHandler.SetTextLocalizedInterp(this, CustomizationLocalizedStringTable, "character_exit_nosave", Locale, YesNoPopup.PopupText);
            YesNoPopup.OnPopupClosedDelegate += OnAttemptStopCustomization;
        }
        else
        {
            CustomizationPopup.ClosePopup();
        }
    }


    public void OnAttemptStopCustomization(BasePopupUI InPopupUI, int InResult)
    {
        YesNoPopup.OnPopupClosedDelegate -= OnAttemptStopCustomization;

        if (InResult > 0)
        {
            CustomizationPopup.ClosePopup();

            if (bOpenCharacterSelectOnClose)
            {
                SelectionPopup.gameObject.SetActive(true);
                bOpenCharacterSelectOnClose = false;

                // Reset Character
                SetLayer(CustomizationSystem.Target.transform, playerLayer);
                OnCharacterSelectReturn?.Invoke();
            }
        }
    }


    public void StopCustomization_Confirm()
    {
        if (SelectionPopup)
        {
            if (SelectionPopup.Active)
            {
                TabFilters.Clear();
                ClothingFilters.Clear();
                ColorwayMeshes.Clear();
            }

            SelectionPopup.ClosePopup(1);
        }
        StopCustomization(false);
    }


    public void StopCustomization_Cancel()
    {
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex == 0)
        {
            CustomizationSystem.Target.UpdateCustomizationCode(SelectedCharacterCode);
            CustomizationSystem.Target.SetActiveCharacter(SelectedCharacterID);
        }

        StopCustomization(false);
    }

    IEnumerator Wait(bool bSave)
    {
        // Wait two seconds for FADE OUT to take effect before camera jumps
        yield return new WaitForSeconds(2f);
        
        // Turn camera back if necessary
        if (bCameraCeiling)
        {
            npc.InteractDialogueInstance.VirtualCameraToEnable.gameObject.transform.Rotate(-270, 0, 0);
            bCameraCeiling = false;
        }

        if (SelectionPopup)
        {
            if (SelectionPopup.Active)
            {
                TabFilters.Clear();
                ClothingFilters.Clear();
                ColorwayMeshes.Clear();
            }

            SelectionPopup.ClosePopup();
        }

        foreach (var bg in PanelBG)
        {
            bg.SetActive(false);
        }

        MainPanel.SetActive(false);
        BasePauseManager PauseManager = BaseGameInstance.Get?.PauseManager;

        if (PauseManager)
        {
            PauseManager.RequestPause(this, false, false);
        }

        if (UICamera)
        {
            if (PlayerCamera)
            {
                if (!PlayerCamera.gameObject.activeSelf)
                {
                    //PlayerCamera.gameObject.SetActive(true);
                }
                PlayerCamera.RemoveNoMouseRequest(this.gameObject);
            }
            UICamera.gameObject.SetActive(false);
        }

        if (CharacterToCustomize)
        {
            SetLayer(CustomizationSystem.Target.transform, playerLayer);

            CharacterToCustomize.transform.rotation = OriginalPlayerRotation;
            CharacterToCustomize.HP.bInvincible = bOriginalInvincible;
        }

        if (!bSave && !string.IsNullOrEmpty(OriginalCharacterCode))
        {
            CustomizationSystem.Target.UpdateCustomizationCode(OriginalCharacterCode);
        }
    }
    public void StopCustomization(bool bSave)
    {
        // First, set to false so SequencerCommand stops
        bIsCustomizing = false;
        //SelectionPopup.gameObject.SetActive(false);
        StartCoroutine(Wait(bSave));
    }

    public void SaveAndQuit()
    {
        if (SelectedPresetItemButtons.Count != 0)
        {
            PresetItemIDs.Clear();
            for (int i = 0; i < SelectedPresetItemButtons.Count; i++)
            {
                if (SelectedPresetItemButtons[i].Item2 != null)
                    PresetItemIDs.Add(SelectedPresetItemButtons[i].Item1);
            }
        }
        if (CustomizationSystem)
        {
            CustomizationSystem.Target.CharacterSaveDelegate += OnCharacterSave;
            CustomizationSystem.Target.SaveCharacterCode(HRNetworkManager.Get.LocalPlayerController, CurrentCharacterID,
                LlamaSoftware.UNET.Chat.ChatSystem.ReplaceFilteredWords(CharacterNameInput.text.Replace(">", "")), PresetItemIDs);
        }
    }

    private void CharacterRequest(BaseCustomizationComponent Source, List<HRSaveSystem.HRCharacterSave.HRCharacter> Characters)
    {
        // TODO: This currently only shows the characters saved to the host. This is a pain to test without Steam IDs.
        // Make sure to pull all characters saved on the disc locally as well that may not exist on the host, check for duplicates.

        Source.CharacterRequestDelegate -= CharacterRequest;

        CharactersCache = Characters;
        var ActiveCharacter = new HRSaveSystem.HRCharacterSave.HRCharacter();

        foreach (var Character in Characters)
        {
            if (Character.bActive)
            {
                ActiveCharacter = Character;
            }
        }

        if (Characters.Count == 0)
        {
            CharacterNameInput.text = DEFAULT_CHARACTER_NAME;
        }
        else
        {
            CharacterNameInput.text = ActiveCharacter.CharacterName;
        }

        bOpening = false;
        StartCustomization();
        GenerateCharacterEntries();
    }

    private void CharacterRequestToCSS(BaseCustomizationComponent Source, List<HRSaveSystem.HRCharacterSave.HRCharacter> Characters)
    {
        Source.CharacterRequestDelegate -= CharacterRequestToCSS;

        CharactersCache = Characters;
        var ActiveCharacter = new HRSaveSystem.HRCharacterSave.HRCharacter();

        foreach (var Character in Characters)
        {
            if (Character.bActive)
            {
                ActiveCharacter = Character;
            }
        }

        if (Characters.Count == 0)
        {
            CharacterNameInput.text = DEFAULT_CHARACTER_NAME;
        }
        else
        {
            CharacterNameInput.text = ActiveCharacter.CharacterName;
        }

        GenerateCharacterEntries();

        if (bOpening)
        {
            OpenFromCSS();
        }
    }


    private void OpenFromCSS()
    {
        bOpening = false;
        StartCustomization(CustomizationTab.Accessories, false);

        CustomizationPopup.gameObject.SetActive(false);
        SelectionPopup.gameObject.SetActive(true);
        CustomizationPopup.ClosePopup();
        SelectionPopup.OpenPopup();

        bOpenCharacterSelectOnClose = true;
    }

    private void OnCharacterSave(BaseCustomizationComponent Source, string CharacterName, bool bCanSave, bool bUpdate)
    {
        Source.CharacterSaveDelegate -= OnCharacterSave;

        if (bCanSave)
        {
            HRSaveSystem.Get.SetCharacterSaveFileToCurrent();

            CustomizationPopup.ClosePopup();

            UpdateCharacterCount();

            // Get scene name
            string sceneName = SceneManager.GetActiveScene().name;

            // Only open the OK popup if in menu map
            if (sceneName == "0_TitleScreenMainMenu_MasterMap")
            {
                if (CustomizationSystem.Target.SavedCustomizationCode != OriginalCharacterCode || CurrentCharacterName != CharacterName)
                {
                    if (bUpdate)
                    {
                        OKPopup.OpenPopup();
                        OKPopup.PopupText.text = $"{CharacterName} has been updated.";
                    }
                    else
                    {
                        OKPopup.OpenPopup();
                        OKPopup.PopupText.text = $"{CharacterName} has been saved.";
                    }
                }
            }

            if (CharacterToCustomize)
            {
                SetLayer(CustomizationSystem.Target.transform, playerLayer);
                OnCharacterSelectReturn?.Invoke();
            }

            AttemptCharacterSelection(false);
            CurrentCharacterName = CharacterName;
            OriginalCharacterCode = CustomizationSystem.Target.SavedCustomizationCode;

            // StopCustomization if not in menu map
            if (sceneName != "0_TitleScreenMainMenu_MasterMap")
            {
                StopCustomization_Confirm();
            }
        }
        else
        {
            OKPopup.OpenPopup();
            OKPopup.PopupText.text = $"<b>{CharacterName}</b> could not be saved.\n\nAnother player has saved a character with the same name.";
        }
    }

    public void PlaceCamera()
    {
        if (CharacterToCustomize)
        {
            OriginalPlayerRotation = CharacterToCustomize.transform.rotation;
            UICamera.transform.position = CharacterToCustomize.transform.position - Vector3.down * 1.1f;
            OriginalCameraPosition = UICamera.transform.position;
            SetZoomAmount(2.4f);
            UICamera.transform.LookAt(CharacterToCustomize.transform.position - Vector3.down * 1.1f);
            CharacterToCustomize.transform.LookAt(CharacterToCustomize.transform.position - Vector3.forward);
            SetZoomAmount(DefaultZoomAmount);
            CharacterToCustomize.transform.localEulerAngles = new Vector3(0, -87.5f);
        }
    }

    public void PlaceCamera(HeroPlayerCharacter InCharacter, Camera Cam, bool bSet = true, float AngleOffset = 0, float distance = 2.35f, float height = 1.1f,
        bool bSetPlayerLayer = true, int Layer = -1, bool bRelease = false, Vector3 RotationOffset = new Vector3())
    {
        PlaceCamera(InCharacter.CustomizationSystem, Cam, InCharacter, bSet, AngleOffset, distance, height, bSetPlayerLayer, Layer, bRelease, RotationOffset);
    }

    public void PlaceCamera(BaseCustomizationSystem customizationSystem, Camera Cam, HeroPlayerCharacter InCharacter = null, bool bSet = true, float AngleOffset = 0, float distance = 2.35f, float height = 1.1f,
        bool bSetPlayerLayer = true, int Layer = -1, bool bRelease = false, Vector3 RotationOffset = new Vector3())
    {
        Transform targetTransform = InCharacter == null ? customizationSystem.transform : InCharacter.transform;

        if (bSet)
            Cam.gameObject.SetActive(true);

        //AngleOffset = 30;
        // -30 for player, 30 for customer

        Vector3 angle = Quaternion.Euler(0, AngleOffset, 0) * targetTransform.transform.forward;

        Cam.transform.position = (targetTransform.position - Vector3.down * height) +
            (angle * distance);
        Cam.transform.LookAt(targetTransform.position - Vector3.down * height);
        Cam.transform.eulerAngles += RotationOffset;

        if (bSet)
        {
            if (bSetPlayerLayer)
                playerLayer = customizationSystem.Target.gameObject.layer;
            SetLayer(customizationSystem.Target.transform, Layer == -1 ? customizationLayer : Layer, bRelease);
        }

    }

    public void ReleaseCamera(HeroPlayerCharacter InCharacter, Camera Cam, int Layer = -1)
    {
        /*
        if (InCharacter)
        {
            Cam.gameObject.SetActive(false);
            SetLayer(InCharacter.CustomizationSystem.Target.transform, Layer == -1 ? playerLayer : Layer);
        }
        */
        ReleaseCamera(InCharacter.customizationSystem, Cam, Layer);
    }

    public void ReleaseCamera(BaseCustomizationSystem customizationSystem, Camera Cam, int Layer = -1)
    {
        if (customizationSystem)
        {
            Cam.gameObject.SetActive(false);
            SetLayer(customizationSystem.Target.transform, Layer == -1 ? playerLayer : Layer, true);
        }
    }
    
    public void ButtonClick(HRPlayerCustomizationUIButton button, bool bUseTab = true, bool bItem = false, CustomizationTab tab = CustomizationTab.BodyType)
    {
        var TargetTab = bUseTab ? CurrentTab : tab;

        if (LastSelection.ContainsKey(TargetTab) && LastSelection[TargetTab] != null && button.bUseLastSelect)
        {
            LastSelection[TargetTab].SetSelected(false);
        }

        if (LastColorSelection && ColorButtonPrefabPool.Contains(button))
        {
            LastColorSelection.SetSelected(false);
        }

        button.SetSelected(true);

        if (button.bUseLastSelect)
        {
            if (LastSelection.ContainsKey(TargetTab))
                LastSelection[TargetTab] = button;
            else
                LastSelection.Add(TargetTab, button);
        }

        if (ColorButtonPrefabPool.Contains(button))
            LastColorSelection = button;

        if ((bUseTab && bItemTab) || (!bUseTab && bItem))
        {
            if (button.bColor)
            {
                CustomizationSystem.Target.RendererData[(int)button.ClothingType].ColorwayID =
                    CustomizationSystem.EquipColorwayByID(button.StringValue, button.ClothingType);
                //CustomizationSystem.Target.UpdateCustomizationCode();

                ColorNameLabel.text = button.ItemName;
                ColorPanel.SetActive(false);
                HSVPanel.SetActive(false);
            }
            else
            {
                ItemNameLabel.text = button.ItemName;
                CurrentItemID = button.StringValue;
                CurrentClothingType = button.ClothingType;
                var EquipmentComponent = CharacterToCustomize.GetComponent<BaseEquipmentComponent>();

                bool equipped = false;

                if (EquipmentComponent != null && PrefabClothingTypes.Contains(button.ClothingType))
                {
                    for (int i = 0; i < EquipmentComponent.ClothingTypes.Count; ++i)
                    {
                        // If a weapon exists in this slot
                        if (EquipmentComponent.ClothingTypes[i] == button.ClothingType)
                        {
                            if (button.StringValue == "")
                            {
                                EquipmentComponent.EquipItem(button.ClothingType, null);
                            }
                            else
                            {
                                EquipmentComponent.EquipItem(button.ClothingType,
                                    CustomizationSystem.ClothingDatabase.ClothingItems[button.ClothingType][button.StringValue].WeaponPrefab.GetComponent<HRClothingComponent>());
                            }

                            equipped = true;
                            break;
                        }
                    }
                }

                if (!equipped && CurrentItemID != "")
                {
                    var OutfitData = CustomizationSystem.ClothingDatabase.GetOutfitData(button.ClothingType.ToString());

                    string Colorway = "";

                    if (!button.bUseHSV)
                    {
                        Colorway = CustomizationSystem.ClothingDatabase.ClothingItems[button.ClothingType][button.StringValue].Colorways.Length > 0 ?
                            CustomizationSystem.ClothingDatabase.ClothingItems[button.ClothingType][button.StringValue].Colorways[0].ID : "";
                    }
                    else
                    {
                        if (button.HSVIndex >= 0 && button.HSVIndex < OutfitData.OutfitData.SharedColorways.Length)
                        {
                            Colorway = OutfitData.OutfitData.SharedColorways[button.HSVIndex].ID;
                        }

                        CustomizationSystem.OnMaterialLoadedDelegate += OnMaterialLoaded;
                    }

                    CustomizationSystem.EquipID(button.ClothingType, button.StringValue, Colorway, true);
                }
                else if (CurrentItemID == "")
                {
                    CustomizationSystem.EquipItemToRendererByID(button.StringValue, button.ClothingType, true);
                }

                // Generate Color Select
                GenerateColorSelectButtons(CurrentItemID, button.ClothingType, TargetTab);

                // TODO: show previously selected swatches instead of the color panel
                if (!button.bUseHSV)
                {
                    ColorPanel.SetActive(true);
                }

                ColorPanel.transform.DOScale(Vector3.one, 0.2f).SetEase(Ease.OutExpo);

                ColorwayArea.gameObject.SetActive(!button.bUseHSV);
                HSVArea.gameObject.SetActive(button.bUseHSV);
                HSVPanel.gameObject.SetActive(button.bUseHSV);

                if (button.bUseHSV)
                {
                    for (int i = 0; i < HSVColorPickers.Count; i++)
                    {
                        //LayoutRebuilder.ForceRebuildLayoutImmediate(HSVColorPickers[i].transform as RectTransform);
                        if (button.ActiveHSVColorPickers.Length > i && button.ActiveHSVColorPickers[i])
                        {
                            HSVColorPickers[i].gameObject.SetActive(true);
                            LayoutRebuilder.ForceRebuildLayoutImmediate(HSVColorPickers[i].transform as RectTransform);
                        }
                        else
                        {
                            HSVColorPickers[i].gameObject.SetActive(false);
                        }
                    }
                }
            }

            SetLayer(CustomizationSystem.Target.transform, customizationLayer);
        }
        else
        {
            switch (TargetTab)
            {
                case CustomizationTab.BodyType:

                    if (CustomizationSystem.Target.BodyType == BodyTypeData[button.Index].BodyType)
                    {
                        return;
                    }

                    ItemNameLabel.text = button.ItemName;
                    CustomizationSystem.SetBodyType(BodyTypeData[button.Index].BodyType, true, true, false, true);
                    SetLayer(CustomizationSystem.Target.transform, customizationLayer);
                    // TODO: regenerate buttons based on body availability
                    break;
                case CustomizationTab.SkinColor:
                    ItemNameLabel.text = button.ItemName;
                    CustomizationSystem.SetSkinColor(button.Index);
                    SetLayer(CustomizationSystem.Target.transform, customizationLayer);
                    break;
                case CustomizationTab.Voice:
                    ItemNameLabel.text = button.ItemName;
                    CustomizationSystem.SetVoiceType(VoiceTypeDatas[button.Index].VoiceTypeID, true);
                    CustomizationSystem.Target.CharacterVoice.PlaySampleAudio();
                    SetLayer(CustomizationSystem.Target.transform, customizationLayer);
                    break;
                case CustomizationTab.Item:
                    Debug.Log(button.Index);
                    int selectionIndex = -1;
                    for(int i = 0; i < SelectedPresetItemButtons.Count; i++)
                    {
                        if (SelectedPresetItemButtons[i].Item1 == button.Index)
                        {
                            //Debug.Log(string.Format("Item List Index: {0}, Button Index {1}", SelectedPresetItemButtons[i].Item1, button.Index));
                            //Debug.Log(i);
                            selectionIndex = i;
                            break;
                        }
                    }
                    if (selectionIndex != -1)
                    {
                        Debug.Log("Deselecting", SelectedPresetItemButtons[selectionIndex].Item2);
                        SelectedPresetItemButtons[selectionIndex].Item2.SetSelected(false);
                        SelectedPresetItemButtons[selectionIndex].Item2.ButtonText.SetText(SelectedPresetItemButtons[selectionIndex].Item2.ItemName);
                        SelectedPresetItemButtons.RemoveAt(selectionIndex);
                    }
                    else
                    {
                        ItemNameLabel.text = button.ItemName;
                        SelectedPresetItemButtons.Add((button.Index, button));
                        button.SetSelected(true);
                    }
                    UpdateInventoryPreview();
                    RegenerateItemNames();
                    break;
            }
        }
    }
    private void UpdateInventoryPreview()
    {
        Debug.Log(SelectedPresetItemButtons.Count);
        for(int i = 0; i < InventorySlots.Count; i++)
        {
            if (i < SelectedPresetItemButtons.Count)
            {
                if (SelectedPresetItemButtons[i].Item2)
                {
                    InventorySlots[i].sprite = SelectedPresetItemButtons[i].Item2.ButtonIcon.sprite;
                    InventorySlots[i].color = Color.white;
                }
            }
            else
                InventorySlots[i].color = Color.clear;
        }
    }

    private void OnMaterialLoaded(BaseCustomizationSystem InSystem)
    {
        InSystem.OnMaterialLoadedDelegate -= OnMaterialLoaded;

        RefreshHSVPanel(CurrentClothingType);
    }

    //awkward solution (ColorPicker cannot set values without firing OnChanged event)
    bool bRefreshingPanel;
    
    public void UpdateHSVColors()
    {
        if (bRefreshingPanel) 
            return;
        if (!CustomizationSystem)
            return;

        for (int i = 0; i < HSVColorPickers.Count; i++)
        {
            Color color = HSVColorPickers[i].CurrentColor;
            CustomizationSystem.SetHSVColor(CurrentClothingType, color, i);
            CustomizationSystem.ApplyHSVColorToMaterial(CurrentClothingType, color, i);
        }

        float exposure = HSVExposureSlider.value;
        CustomizationSystem.SetHSVExposure(CurrentClothingType, exposure);
        CustomizationSystem.ApplyHSVExposureToMaterial(CurrentClothingType, exposure);

        CustomizationSystem.Target.SetCustomizationDirty(true);
    }

    void RefreshHSVPanel(BaseClothingDatabase.BaseClothingType ClothingType)
    {
        bRefreshingPanel = true;

        if(!CustomizationSystem.TryGetMaterialParams(ClothingType, 
            out BaseCustomizationComponent.CustomizationCode.MaterialParams materialParams))
        {
            return;
        }

        if(materialParams.Colors != null)
        {
            int numColors = Mathf.Min(HSVColorPickers.Count, materialParams.Colors.Count);
            for (int i = 0; i < numColors; i++)
            {
                Color color = materialParams.Colors[i];
                HSVColorPickers[i].AssignColor(color);
            }
        }

        if(materialParams.Values != null && materialParams.Values.Count > 0)
        {
            HSVExposureSlider.value = materialParams.Values[0];
        }
        bRefreshingPanel = false;
    }


    private void RegenerateItemNames()
    {
        for (int i = 0; i < SelectedPresetItemButtons.Count; ++i)
        {
            SelectedPresetItemButtons[i].Item2.ButtonText.text = (i + 1).ToString();
        }
    }

    public void SetCurrentClothingType(int NewClothingTypeIndex)
    {
        CurrentClothingTypeIndex = NewClothingTypeIndex;

        //SetToBodyTypeMode(CurrentClothingTypeIndex == 0);

        /*if (CurrentClothingTypeIndex <= 0)
        {
            CurrentClothingTypeIndex = 0;
        }
        else
        {
            SortClothingTypes();
            if (CurrentClothingTypeIndex == OrderedClothingTypes.Length - 1)
            {
                NextTypeButton.gameObject.SetActive(false);
            }
            else if (CurrentClothingTypeIndex >= OrderedClothingTypes.Length)
            {
                CurrentClothingTypeIndex = OrderedClothingTypes.Length - 1;
            }
            else
            {
                NextTypeButton.gameObject.SetActive(true);
            }
        }
        UpdateText();*/
    }

    public void SetText(TextMeshProUGUI InTextMesh, string NewText)
    {
        if (InTextMesh)
        {
            InTextMesh.SetText(NewText);
        }
    }

    public void SetToBodyTypeMode(bool bEnabled)
    {
        bBodyType = bEnabled;
        /*if (bBodyType)
        {
            PreviousTypeButton.gameObject.SetActive(false);
            NextTypeButton.gameObject.SetActive(true);
        }
        else
        {
            PreviousTypeButton.gameObject.SetActive(true);
        }
        UpdateText();*/
    }

    public void PreviousType()
    {
        if (CurrentClothingTypeIndex - 1 == -1)
        {
            SetToBodyTypeMode(true);
        }
        else
        {
            SetCurrentClothingType(CurrentClothingTypeIndex - 1);
        }
    }

    public void NextType()
    {
        if (bBodyType)
        {
            SetToBodyTypeMode(false);
        }
        else
        {
            SetCurrentClothingType(CurrentClothingTypeIndex + 1);
        }
    }

    public void PreviousItem()
    {
        if (bBodyType)
        {
            CustomizationSystem.PreviousBodyType();
        }
        else
        {
            //CustomizationSystem.PreviousIndex(OrderedClothingTypes[CurrentClothingTypeIndex]);
        }
    }

    public void NextItem()
    {
        if (bBodyType)
        {
            CustomizationSystem.NextBodyType();
        }
        else
        {
            //CustomizationSystem.NextIndex(OrderedClothingTypes[CurrentClothingTypeIndex]);
        }
    }

    void SortClothingTypes(bool bForced = false)
    {
        if (bForced || OrderedClothingTypes == null)
        {
            OrderedClothingTypes = new List<BaseClothingDatabase.BaseClothingType> {
                BaseClothingDatabase.BaseClothingType.Hat,
                BaseClothingDatabase.BaseClothingType.Hair,
                BaseClothingDatabase.BaseClothingType.Eyebrows,
                BaseClothingDatabase.BaseClothingType.FaceAccessory,
                BaseClothingDatabase.BaseClothingType.FacialHair,
                BaseClothingDatabase.BaseClothingType.Top,
                BaseClothingDatabase.BaseClothingType.SecondaryTop,
                BaseClothingDatabase.BaseClothingType.Bottom,
                BaseClothingDatabase.BaseClothingType.Accessory,
                BaseClothingDatabase.BaseClothingType.LeftGlove,
                BaseClothingDatabase.BaseClothingType.RightGlove,
                BaseClothingDatabase.BaseClothingType.Shoes,
            };
        }
    }

    public void PreviousColor()
    {
        if (bBodyType)
        {
            CustomizationSystem.PreviousSkinColor();
        }
        else
        {
            //CustomizationSystem.PreviousCustomMaterial(OrderedClothingTypes[CurrentClothingTypeIndex]);
        }
    }

    public void NextColor()
    {
        if (bBodyType)
        {
            CustomizationSystem.NextSkinColor();
        }
        else
        {
            //CustomizationSystem.NextCustomMaterial(OrderedClothingTypes[CurrentClothingTypeIndex]);
        }
    }


    public void CopyCodeToClipboard(string CharacterName)
    {
        OKPopup.OpenPopup();
        Locale.CharacterName1 = CharacterName;
        BaseLocalizationHandler.SetTextLocalizedInterp(this, CustomizationLocalizedStringTable, "character_code_copy", Locale, OKPopup.PopupText);
    }


    public void AttemptImportCharacter()
    {
        ImportPopup.OpenPopup();
    }


    public void OnAttemptImportCharacter(BasePopupUI InPopupUI, int InResult)
    {
        if (InResult > 0)
        {
            CustomizationSystem.Target.UpdateCustomizationCode(ImportField.text);
            CharacterNameInput.text = DEFAULT_CHARACTER_NAME;
            CurrentCharacterName = "";
            CurrentCharacterID = HRSaveSystem.Get.SavedCharacters.ID;
            bOpenCharacterSelectOnClose = true;

            StartCustomization();
        }
    }


    public void AttemptRenameCharacter()
    {
        RenamePopup.OpenPopup();
    }


    public void OnAttemptRenameCharacter(BasePopupUI InPopupUI, int InResult)
    {
        if (SelectedPresetItemButtons.Count != 0)
        {
            PresetItemIDs.Clear();
            for (int i = 0; i < SelectedPresetItemButtons.Count; i++)
            {
                if (SelectedPresetItemButtons[i].Item2 != null)
                    PresetItemIDs.Add(SelectedPresetItemButtons[i].Item1);
            }
        }
        if (InResult > 0)
        {
            CustomizationSystem.Target.CharacterSaveDelegate += OnCharacterSave;
            CustomizationSystem.Target.SaveCharacterCode(HRNetworkManager.Get.LocalPlayerController, CurrentCharacterID,
                RenameField.text, PresetItemIDs);
        }
    }


    public void SetZoomAmount(float InZoomAmount)
    {
        InZoomAmount = Mathf.Clamp(InZoomAmount, MinZoomAmount, MaxZoomAmount);
        float ZoomPercent = ZoomHeightCurve.Evaluate(1 - ((InZoomAmount - MinZoomAmount) / MaxZoomAmount));

        if (UICamera)
        {
            UICamera.transform.position = new Vector3(UICamera.transform.position.x, OriginalCameraPosition.y + (ZoomPercent * ZoomHeightOffset),
                OriginalCameraPosition.z - InZoomAmount);
            CurrentZoomAmount = InZoomAmount;
        }
    }


    public void TweenZoomAmount(float InZoomAmount)
    {
        DOTween.To(() => CurrentZoomAmount, x => CurrentZoomAmount = x, InZoomAmount, 0.7f).SetEase(Ease.OutQuad).OnUpdate(() => SetZoomAmount(CurrentZoomAmount));
    }

    public void ZoomTick(bool bZoomIn)
    {
        int ZoomFactor = bZoomIn ? -1 : 1;
        float TargetZoom = CurrentZoomAmount + (ZoomFactor * ZoomTickInterval);
        SetZoomAmount(TargetZoom);
    }

    public void SetHeightOffset(float NewOffset)
    {
        NewOffset = Mathf.Clamp(NewOffset, MinCameraHeightOffset, MaxCameraHeightOffset);
        if (UICamera)
        {
            CurrentCameraHeightOffset = NewOffset;
            UICamera.transform.position = new Vector3(UICamera.transform.position.x, OriginalCameraPosition.y + CurrentCameraHeightOffset, UICamera.transform.position.z);
        }
    }

    public void ApplySavedRendererData()
    {
        if (CustomizationSystem)
        {
            if (CustomizationSystem.Target && SavedPlayerEquippedItemIDs != null && SavedPlayerEquippedMaterialIDs != null)
            {
                /*CustomizationSystem.SetBodyType(SavedPlayerBodyType);
                CustomizationSystem.SetSkinColor(SavedPlayerSkinColorIndex);
                CustomizationSystem.Target.SavedEquippedItemIDs = SavedPlayerEquippedItemIDs;
                CustomizationSystem.Target.SavedEquippedMaterialIDs = SavedPlayerEquippedMaterialIDs;
                CustomizationSystem.Target.ApplySavedItemIDs();
                CustomizationSystem.Target.ApplySavedMaterialIDs();
                CustomizationSystem.UpdateEquippedItems();
                CustomizationSystem.UpdateEquippedMaterials();*/
            }
        }
    }

    public void HandleSaveComponentInitialize(HRSaveComponent InSaveComponent, int ComponentID, int AuxID)
    {

    }

    public void HandlePreSave()
    {
        //if (CustomizationSystem)
        //{
        //    if (CustomizationSystem.Target)
        //    {
        //        CustomizationSystem.Target.CopyEquippedIDsForSave();
        //        SavedPlayerEquippedItemIDs = CustomizationSystem.Target.SavedEquippedItemIDs;
        //        SavedPlayerEquippedMaterialIDs = CustomizationSystem.Target.SavedEquippedMaterialIDs;
        //        SavedPlayerBodyType = CustomizationSystem.Target.BodyType;
        //        SavedPlayerSkinColorIndex = CustomizationSystem.Target.CurrentSkinColorIndex;
        //    }
        //}
    }

    public void HandleLoaded()
    {

    }

    public void HandleSaved()
    {

    }

    public void HandleReset()
    {

    }

    public bool IsSaveDirty()
    {
        return false;
    }

    public string GetCharacterCode()
    {
        return CustomizationSystem.Target.SavedCustomizationCode;
    }
}

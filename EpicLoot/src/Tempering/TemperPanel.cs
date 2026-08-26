using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EpicLoot.Compendium;
using EpicLoot.Config;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace EpicLoot;

public class TemperPanel : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler {
    private static bool fontsLoaded;
    public static TemperPanel Instance;
    public static implicit operator bool(TemperPanel i) => i != null;
    private const float TIME_TO_TEMPER = 2f;
    public StoreGui _storeGui;

    [Header("Background")]
    public Image frame;
    public Image selectedFrame;

    [Header("List Elements")]
    public GameObject itemListPrefab;
    public GameObject enchantmentListPrefab;
    public GameObject requirementListPrefab;

    [Header("List Roots")]
    public RectTransform itemListRoot;
    public ScrollRectEnsureVisible itemListEnsureVisible;
    public RectTransform enchantmentsListRoot;
    public ScrollRectEnsureVisible enchantmentListEnsureVisible;
    public RectTransform requirementListRoot;

    [Header("Tooltip Objects")]
    public TextSizeFitter itemTooltip;
    public TextSizeFitter infoTooltip;
    public TextSizeFitter logTooltip;
    private static readonly StringBuilder logSB = new StringBuilder();
    public UITooltip sundialTooltip;

    [Header("Button Objects")]
    public Button temperButton;
    public GameObject progressPanel;
    public Button cancelButton;
    public GuiBar progressBar;
    public float progressTimer;

    [Header("Gamepad Objects")]
    public RectTransform tooltipAnchor;
    public GameObject gamepadPanelHints;

    [Header("Currency Objects")]
    public GameObject currencies;
    public Image coinsImage;
    public TextMeshProUGUI coinsText;
    public Image forestImage;
    public TextMeshProUGUI forestText;
    public Image ironImage;
    public TextMeshProUGUI ironText;
    public Image goldImage;
    public TextMeshProUGUI goldText;

    [Header("Effects")]
    public EffectList temperStartEffects;
    public EffectList temperDoneEffects;
    public GameObject[] startEffects;

    [Header("Temporary Values")]
    public ItemElement selectedItemElement;
    public EnchantmentElement selectedEnchantmentElement;
    private TemperData temperData;

    [Header("Gamepad Variables")]
    public bool takeGamepadInput;
    public bool isItemListFocused;
    public bool isFirstChild = true;

    private RectTransform _rt;
    private Vector2 _dragOffset;

    public void Awake() {
        Instance = this;
        _storeGui = transform.parent.GetComponent<StoreGui>();

        frame = transform.Find("Frame").GetComponent<Image>();
        selectedFrame = transform.Find("SelectedFrame").GetComponent<Image>();
        itemListRoot = transform.Find("Inventory/Panel/ItemList").GetComponent<RectTransform>();
        itemListEnsureVisible = transform.Find("Inventory/Panel").GetComponent<ScrollRectEnsureVisible>();
        enchantmentsListRoot = transform.Find("Enchantments/Panel/ItemList").GetComponent<RectTransform>();
        enchantmentListEnsureVisible = transform.Find("Enchantments/Panel").GetComponent<ScrollRectEnsureVisible>();
        requirementListRoot = transform.Find("Requirements/Panel/ItemList").GetComponent<RectTransform>();
        itemTooltip = transform.Find("Tooltip/Panel/ItemList").gameObject.AddComponent<TextSizeFitter>();
        sundialTooltip = transform.Find("Sundial").GetComponent<UITooltip>();

        temperButton = transform.Find("Temper/Button").GetComponent<Button>();
        infoTooltip = transform.Find("Info/Panel/ItemList").gameObject.AddComponent<TextSizeFitter>();
        logTooltip = transform.Find("Log/Panel/ItemList").gameObject.AddComponent<TextSizeFitter>();

        itemListPrefab = transform.Find("Inventory/Panel/ItemElement").gameObject;
        itemListPrefab.AddComponent<ItemElement>();
        requirementListPrefab = transform.Find("Requirements/Panel/ItemElement").gameObject;
        requirementListPrefab.AddComponent<RequirementElement>();
        enchantmentListPrefab = transform.Find("Enchantments/Panel/ItemElement").gameObject;
        enchantmentListPrefab.AddComponent<EnchantmentElement>();

        currencies = transform.Find("Currencies").gameObject;
        coinsImage = transform.Find("Currencies/Coins").GetComponent<Image>();
        coinsText = transform.Find("Currencies/CoinsCount").GetComponent<TextMeshProUGUI>();
        forestImage = transform.Find("Currencies/ForestTokens").GetComponent<Image>();
        forestText = transform.Find("Currencies/ForestTokensCount").GetComponent<TextMeshProUGUI>();
        ironImage = transform.Find("Currencies/BountyTokensIron").GetComponent<Image>();
        ironText = transform.Find("Currencies/BountyTokensIronCount").GetComponent<TextMeshProUGUI>();
        goldImage = transform.Find("Currencies/BountyTokensGold").GetComponent<Image>();
        goldText = transform.Find("Currencies/BountyTokensGoldCount").GetComponent<TextMeshProUGUI>();
        progressPanel = transform.Find("Temper/Progress").gameObject;
        cancelButton = progressPanel.transform.Find("CancelButton").GetComponent<Button>();
        progressBar = progressPanel.transform.Find("ProgressBar").GetComponent<GuiBar>();

        gamepadPanelHints = transform.Find("gamepad_hint").gameObject;
        tooltipAnchor = transform.Find("TooltipAnchor").GetComponent<RectTransform>();

        gamepadPanelHints.SetActive(false);
        progressPanel.SetActive(false);
        selectedFrame.enabled = false;
        logTooltip.autoScrollToBottom = true;

        cancelButton.onClick.AddListener(OnCancel);
        temperButton.onClick.AddListener(OnStartTemper);
        temperButton.interactable = false;

        temperStartEffects = ZNetScene.instance.GetPrefab("forge").GetComponent<CraftingStation>().m_craftItemEffects;
        temperDoneEffects = _storeGui.m_buyEffects;

        LoadCurrencyIcons();
        SetUITooltipPrefabs();
        LoadButtonSfx();
        LoadImageSprites();
        FixScrollSensitivity();
        LocalizeUIGamePads();
        LocalizeTitles();
        LocalizeButtons();
        SetSundialTooltip();

        transform.SetAsFirstSibling();
        isFirstChild = true;

        // Cache the root RectTransform, make the background drag-receptive, and apply the
        // saved position. The background click bubbles the drag up to this component.
        _rt = (RectTransform)transform;
        if (frame != null) {
            frame.raycastTarget = true;
        }
        ApplyConfiguredPosition();
    }

    public void ApplyConfiguredPosition() {
        if (_rt == null) {
            return;
        }
        _rt.anchoredPosition = new Vector2(ELConfig.TemperPanelPositionX.Value, ELConfig.TemperPanelPositionY.Value);
    }

    public void OnBeginDrag(PointerEventData eventData) {
        var parent = (RectTransform)_rt.parent;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parent, eventData.position, eventData.pressEventCamera, out var localPoint)) {
            _dragOffset = _rt.anchoredPosition - localPoint;
        }
    }

    public void OnDrag(PointerEventData eventData) {
        var parent = (RectTransform)_rt.parent;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parent, eventData.position, eventData.pressEventCamera, out var localPoint)) {
            _rt.anchoredPosition = localPoint + _dragOffset;
        }
    }

    public void OnEndDrag(PointerEventData eventData) {
        // Persists automatically because cfg.SaveOnConfigSet is true.
        ELConfig.TemperPanelPositionX.Value = _rt.anchoredPosition.x;
        ELConfig.TemperPanelPositionY.Value = _rt.anchoredPosition.y;
    }

    private void LocalizeTitles() {
        transform.Find("Inventory/Title").GetComponent<TextMeshProUGUI>().text = Localization.instance.Localize("$settings_inventory");
        transform.Find("Enchantments/Title").GetComponent<TextMeshProUGUI>().text = Localization.instance.Localize("$mod_epicloot_enchantments");
        transform.Find("Tooltip/Title").GetComponent<TextMeshProUGUI>().text = Localization.instance.Localize("$mod_epicloot_output");
        transform.Find("Log/Title").GetComponent<TextMeshProUGUI>().text = Localization.instance.Localize("$mod_epicloot_history");
        transform.Find("Info/Title").GetComponent<TextMeshProUGUI>().text = Localization.instance.Localize("$mod_epicloot_info");
        transform.Find("Requirements/Title").GetComponent<TextMeshProUGUI>().text = Localization.instance.Localize("$mod_epicloot_requirements");
    }
    private void LocalizeButtons() {
        temperButton.transform.Find("Text").GetComponent<TextMeshProUGUI>().text = Localization.instance.Localize("$mod_epicloot_temper");
        cancelButton.transform.Find("Text").GetComponent<TextMeshProUGUI>().text = Localization.instance.Localize("$menu_cancel");
    }
    private void LoadCurrencyIcons() {
        ItemDrop.ItemData coins = ObjectDB.instance.GetItemPrefab("Coins").GetComponent<ItemDrop>().m_itemData;
        ItemDrop.ItemData forestTokens = ObjectDB.instance.GetItemPrefab("ForestToken").GetComponent<ItemDrop>().m_itemData;
        ItemDrop.ItemData ironTokens = ObjectDB.instance.GetItemPrefab("IronBountyToken").GetComponent<ItemDrop>().m_itemData;
        ItemDrop.ItemData goldTokens = ObjectDB.instance.GetItemPrefab("GoldBountyToken").GetComponent<ItemDrop>().m_itemData;
        coinsImage.sprite = coins.GetIcon();
        forestImage.sprite = forestTokens.GetIcon();
        ironImage.sprite = ironTokens.GetIcon();
        goldImage.sprite = goldTokens.GetIcon();

        coinsImage.GetComponent<UITooltip>().Set("", coins.m_shared.m_name);
        forestImage.GetComponent<UITooltip>().Set("", forestTokens.m_shared.m_name);
        ironImage.GetComponent<UITooltip>().Set("", ironTokens.m_shared.m_name);
        goldImage.GetComponent<UITooltip>().Set("", goldTokens.m_shared.m_name);
    }
    private void SetUITooltipPrefabs() {
        GameObject storeBuyButtonTooltip = _storeGui.m_buyButton.GetComponent<UITooltip>().m_tooltipPrefab;
        GameObject storeItemTooltip = _storeGui.m_listElement.GetComponent<UITooltip>().m_tooltipPrefab;

        UITooltip[] uiTooltips = GetComponentsInChildren<UITooltip>(true);
        for (int i = 0; i < uiTooltips.Length; ++i) {
            UITooltip uiTooltip = uiTooltips[i];
            if (uiTooltip.name == "Sundial" || uiTooltip.name == "ItemElement") {
                uiTooltip.m_tooltipPrefab = storeItemTooltip;
            } else {
                uiTooltip.m_tooltipPrefab = storeBuyButtonTooltip;
            }
        }
    }
    private void SetSundialTooltip() {
        // Intentionally empty for now: the tooltip copy needs proper localization before it ships.
        // See the git history for the draft English text this replaced.
    }
    private void LoadButtonSfx() {
        ButtonSfx sfx = InventoryGui.instance.GetComponentInChildren<ButtonSfx>(true);
        if (sfx != null) {
            Button[] buttons = GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; ++i) {
                Button button = buttons[i];
                button.gameObject.AddComponent<ButtonSfx>().m_sfxPrefab = sfx.m_sfxPrefab;
            }
        }
    }
    private void LoadImageSprites() {
        frame.sprite = EpicAssets.MerchantPanel.GetComponent<Image>().sprite;
        Image selectedImg = InventoryGui.instance.transform.Find("root/Player/selected_frame/selected (1)").GetComponent<Image>();
        selectedFrame.sprite = selectedImg.sprite;
        selectedFrame.color = selectedImg.color;
        frame.material = _storeGui.transform.Find("Store/SellPanel").GetComponent<Image>().material;
        temperButton.spriteState = _storeGui.m_buyButton.GetComponent<Button>().spriteState; ;
        temperButton.image.sprite = _storeGui.m_buyButton.image.sprite;

        transform.Find("Sundial").GetComponent<Image>().sprite = EpicAssets.MerchantPanel.transform.Find("Sundial").GetComponent<Image>().sprite;

        Sprite itemBkg = _storeGui.transform.Find("Store/ItemList/Items/ItemElement/bkg").GetComponent<Image>().sprite;
        Material itemMaterial = _storeGui.transform.Find("Store/ItemList/Items/ItemElement/icon").GetComponent<Image>().material;
        Color selectedColor = _storeGui.transform.Find("Store/ItemList/Items/ItemElement/selected").GetComponent<Image>().color;

        progressBar.m_barImage.sprite = itemBkg;

        Image[] images = GetComponentsInChildren<Image>(true);
        for (int i = 0; i < images.Length; ++i) {
            Image image = images[i];
            if (image.name
                is "Background"
                or "Scrollbar"
                or "Handle"
                or "Selected"
                or "ItemElement"
                or "ResultDialogue"
                or "RequirementPanel"
                or "Panel"
                or "ProgressBar") {
                image.sprite = itemBkg;
                if (image.name == "Selected") {
                    image.color = selectedColor;
                }
            } else if (image.name == "MagicBG") {
                image.sprite = EpicAssets.GenericItemBgSprite;
            } else if (image.name == "Icon") {
                image.material = itemMaterial;
            }
        }
    }
    private void FixScrollSensitivity() {
        ScrollRect storeScrollRect = _storeGui.GetComponentInChildren<ScrollRect>();
        ScrollRect[] scrollRects = GetComponentsInChildren<ScrollRect>(true);
        for (int i = 0; i < scrollRects.Length; ++i) {
            ScrollRect scrollRect = scrollRects[i];
            scrollRect.scrollSensitivity = storeScrollRect.scrollSensitivity;
        }
    }
    private void LocalizeUIGamePads() {
        TextMeshProUGUI[] hints = gamepadPanelHints.GetComponentsInChildren<TextMeshProUGUI>(true);
        for (int i = 0; i < hints.Length; ++i) {
            TextMeshProUGUI tmp = hints[i];
            string key = ZInput.instance.GetBoundKeyString(tmp.name);
            tmp.text = Localization.instance.Localize(key);
            if (tmp.name.StartsWith("JoyDPad")) {
                tmp.gameObject.SetActive(false);
            }
        }
        UIGamePad[] uiGamePads = GetComponentsInChildren<UIGamePad>(true);
        for (int i = 0; i < uiGamePads.Length; ++i) {
            UIGamePad uiGamePad = uiGamePads[i];
            TextMeshProUGUI tmp = uiGamePad.m_hint.GetComponentInChildren<TextMeshProUGUI>();
            string keyString = ZInput.instance.GetBoundKeyString(uiGamePad.m_zinputKey, true);
            tmp.text = Localization.instance.Localize(keyString);
        }
    }

    public void OnDestroy() {
        Instance = null;
    }

    public void Show(Player player) {
        gameObject.SetActive(true);
        progressPanel.SetActive(false);
        temperButton.gameObject.SetActive(true);
        selectedItemElement = null;
        selectedEnchantmentElement = null;
        temperData = null;
        FillItemList(player);
        UpdateInfo();
        UpdateCurrencies();
        ClearLog();
    }

    public void Hide() {
        gameObject.SetActive(false);
        progressPanel.SetActive(false);
        temperButton.gameObject.SetActive(true);
    }

    public void UpdateCurrencies() {
        if (!Player.m_localPlayer) {
            coinsText.text = "0";
            forestText.text = "0";
            ironText.text = "0";
            goldText.text = "0";
        } else {
            int coins = 0;
            int forest = 0;
            int iron = 0;
            int gold = 0;
            List<ItemDrop.ItemData> items = Player.m_localPlayer.GetInventory().GetAllItems();
            for (int i = 0; i < items.Count; ++i) {
                ItemDrop.ItemData item = items[i];
                switch (item.m_shared.m_name) {
                    case "$item_coins":
                        coins += item.m_stack;
                        break;
                    case "$mod_epicloot_assets_foresttoken":
                        forest += item.m_stack;
                        break;
                    case "$mod_epicloot_assets_ironbountytoken":
                        iron += item.m_stack;
                        break;
                    case "$mod_epicloot_assets_goldbountytoken":
                        gold += item.m_stack;
                        break;
                }
            }
            coinsText.text = coins.ToString();
            forestText.text = forest.ToString();
            ironText.text = iron.ToString();
            goldText.text = gold.ToString();
        }
    }

    public void UpdateSelectedItem() {
        for (int i = 0; i < ItemElement.elements.Count; ++i) {
            ItemElement element = ItemElement.elements[i];
            element.selected.gameObject.SetActive(element == selectedItemElement);
        }
    }

    public void UpdateSelectedEnchantment() {
        for (int i = 0; i < EnchantmentElement.elements.Count; ++i) {
            EnchantmentElement element = EnchantmentElement.elements[i];
            element.selected.gameObject.SetActive(element == selectedEnchantmentElement);
        }
    }

    public void UpdateItemTooltip() {
        itemTooltip.SetText(temperData == null ? "" : Localization.instance.Localize(temperData.GetTooltip()));
    }

    public void Update() {
        UpdateGamepadInput();
        UpdateItemTooltip();
        UpdateProgress();
    }

    private bool CanTemper() {
        // selectedValues is null only when a config reload removed the effect's value range after it
        // was listed; keep the button disabled rather than running a temper that can only fail.
        if (!Player.m_localPlayer || temperData == null || temperData.selectedValues == null) {
            return false;
        }
        return HaveRequirements(Player.m_localPlayer);
    }

    private bool HaveRequirements(Player player) {
        if (player.NoCostCheat()) {
            return true;
        }
        if (!selectedItemElement) {
            return false;
        }

        TemperRequirement[] requirements = TemperMan.GetRequirements(selectedItemElement._magicItem.Rarity);
        for (int i = 0; i < requirements.Length; ++i) {
            TemperRequirement requirement = requirements[i];
            // A missing prefab renders as an empty row in the list; treat it as unaffordable here
            // so a broken cost config can't make tempering free.
            if (!requirement.isValid) return false;
            int playerItemCount = player.GetInventory().CountItems(requirement.item.m_itemData.m_shared.m_name);
            if (playerItemCount < requirement.amount) return false;
        }
        return true;
    }

    private bool ConsumeRequirements(Player player) {
        if (player.NoCostCheat()) {
            return true;
        }
        if (!selectedItemElement) {
            return false;
        }

        TemperRequirement[] requirements = TemperMan.GetRequirements(selectedItemElement._magicItem.Rarity);
        Piece.Requirement[] pieceRequirements = requirements
            .Select(r => r.ToPieceRequirement())
            .ToArray();
        player.ConsumeResources(pieceRequirements, 1);

        UpdateCurrencies();
        return true;
    }
    public void FillItemList(Player player) {
        List<ItemDrop.ItemData> magicItems = player
            .GetInventory()
            .GetAllItems()
            .Where(x => x.IsMagic() && x.IsShardStone() == false)
            .ToList();


        for (int i = 0; i < itemListRoot.childCount; ++i) {
            Transform child = itemListRoot.GetChild(i);
            UnityEngine.Object.Destroy(child.gameObject);
        }

        ItemElement.elements.Clear();

        for (int i = 0; i < magicItems.Count; ++i) {
            ItemDrop.ItemData item = magicItems[i];
            GameObject instance = Instantiate(itemListPrefab, itemListRoot);
            instance.SetActive(true);
            ItemElement element = instance.GetComponent<ItemElement>();
            element.SetItem(item);
            element.index = i;
            element.button.onClick.AddListener(() => {
                SetSelectedItem(element);
            });
        }

        SetSelectedItem(0);
    }

    public void SetSelectedItem(int index = 0) {
        if (ItemElement.elements.Count <= 0) return;
        if (index > ItemElement.elements.Count - 1) index = 0;
        SetSelectedItem(ItemElement.elements[index]);
    }
    public void SetSelectedItem(ItemElement element) {
        selectedItemElement = element;
        UpdateSelectedItem();
        FillEnchantmentList();
        FillRequirementList();
        ClearLog();
        if (element) {
            itemListEnsureVisible.CenterOnItem(element.transform as RectTransform);
            if (ZInput.IsGamepadActive()) {
                if (_storeGui.m_selectedItem != null) {
                    _storeGui.SelectItem(-1, false);
                }
                element.button.Select();
            }
        } else {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    public void SetSelectedEnchantment(int index = 0) {
        if (EnchantmentElement.elements.Count <= 0) return;
        if (index > EnchantmentElement.elements.Count - 1) index = 0;
        SetSelectedEnchantment(EnchantmentElement.elements[index]);
    }

    public void SetSelectedEnchantment(EnchantmentElement element) {
        selectedEnchantmentElement = element;
        temperData = element ? new TemperData(selectedItemElement._item, element._effect.EffectType) : null;
        UpdateSelectedEnchantment();
        UpdateTemperButton();
        UpdateInfo();
        if (element) {
            enchantmentListEnsureVisible.CenterOnItem(element.transform as RectTransform);
            if (ZInput.IsGamepadActive()) {
                if (_storeGui.m_selectedItem != null) {
                    _storeGui.SelectItem(-1, false);
                }
                element.button.Select();
            }
        } else {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    public void FillEnchantmentList(string selectedEffect = null) {
        for (int i = 0; i < enchantmentsListRoot.childCount; ++i) {
            Transform child = enchantmentsListRoot.GetChild(i);
            UnityEngine.Object.Destroy(child.gameObject);
        }
        EnchantmentElement.elements.Clear();

        SetSelectedEnchantment(null);

        if (!selectedItemElement) {
            return;
        }

        List<MagicItemEffect> effects = selectedItemElement._magicItem.GetEffects();

        int listIndex = 0;
        for (int i = 0; i < effects.Count; ++i) {
            MagicItemEffect effect = effects[i];
            // A valueless effect (Warmth, Indestructible) has no roll range to temper against --
            // selecting one used to NRE in TemperData. Leave them out of the list entirely.
            if (!TemperData.IsTemperable(selectedItemElement._magicItem, effect)) {
                continue;
            }
            GameObject instance = Instantiate(enchantmentListPrefab, enchantmentsListRoot);
            instance.SetActive(true);
            if (instance.TryGetComponent(out EnchantmentElement element)) {
                element.selected.color = EpicLoot.GetRarityColorARGB(selectedItemElement._magicItem.Rarity);
                element.SetEffect(effect, selectedItemElement._magicItem.Rarity);
                // List position, NOT the effect index: gamepad navigation walks
                // EnchantmentElement.elements by this value, and skipping untemperable effects
                // above desyncs the two (the up-branch then indexed past the end of the list).
                element.index = listIndex++;
                element.button.onClick.AddListener(() => {
                    SetSelectedEnchantment(element);
                });
                if (!string.IsNullOrEmpty(selectedEffect) && effect.EffectType == selectedEffect) {
                    SetSelectedEnchantment(element);
                }
            }
        }
    }
    public void FillRequirementList() {
        for (int i = 0; i < requirementListRoot.childCount; ++i) {
            Transform child = requirementListRoot.GetChild(i);
            UnityEngine.Object.Destroy(child.gameObject);
        }
        RequirementElement.elements.Clear();

        if (!selectedItemElement) {
            for (int i = 0; i < 4; ++i) {
                GameObject instance = UnityEngine.Object.Instantiate(requirementListPrefab, requirementListRoot);
                instance.SetActive(true);
                if (instance.TryGetComponent(out RequirementElement element)) {
                    element.Set(null);
                }
            }
        } else {
            TemperRequirement[] requirements = TemperMan.GetRequirements(selectedItemElement._magicItem.Rarity);
            for (int i = 0; i < requirements.Length; ++i) {
                TemperRequirement requirement = requirements[i];
                if (!requirement.isValid) continue;
                GameObject instance = UnityEngine.Object.Instantiate(requirementListPrefab, requirementListRoot);
                instance.SetActive(true);
                if (instance.TryGetComponent(out RequirementElement element)) {
                    element.Set(requirement);
                }
            }

            if (RequirementElement.elements.Count < 4) {
                int difference = 4 - RequirementElement.elements.Count;
                for (int i = 0; i < difference; ++i) {
                    GameObject instance = UnityEngine.Object.Instantiate(requirementListPrefab, requirementListRoot);
                    instance.SetActive(true);
                    if (instance.TryGetComponent(out RequirementElement element)) {
                        element.Set(null);
                    }
                }
            }
        }
    }
    public void UpdateTemperButton() {
        temperButton.interactable = CanTemper();
    }
    public void OnStartTemper() {
        if (!Player.m_localPlayer || temperData == null) {
            return;
        }
        progressBar.SetValue(0f);
        progressBar.SetMaxValue(TIME_TO_TEMPER);
        progressTimer = 0f;
        progressPanel.SetActive(true);
        temperButton.gameObject.SetActive(false);
        startEffects = temperStartEffects.Create(Player.m_localPlayer.transform.position, Quaternion.identity);
    }
    public void UpdateProgress() {
        if (!progressPanel.activeSelf) {
            return;
        }
        progressTimer += Time.deltaTime;
        progressBar.SetValue(progressTimer);
        if (progressTimer >= TIME_TO_TEMPER) {
            OnTemper();
            progressPanel.SetActive(false);
            progressTimer = -1f;
            temperButton.gameObject.SetActive(true);
        }
    }
    public void OnCancel() {
        if (!progressPanel.activeSelf) {
            return;
        }
        progressPanel.SetActive(false);
        progressTimer = -1f;
        temperButton.gameObject.SetActive(true);
        if (startEffects != null) {
            for (int i = 0; i < startEffects.Length; ++i) {
                GameObject effect = startEffects[i];
                if (effect != null) {
                    ZNetScene.instance.Destroy(effect);
                }
            }

            startEffects = null;
        }
    }
    public void OnTemper() {
        if (!Player.m_localPlayer || temperData == null) {
            return;
        }
        if (!ConsumeRequirements(Player.m_localPlayer)) {
            EpicLoot.LogWarning("Tried to temper, but failed to consume requirements");
            return;
        }
        // The outcome is decided here, at execution -- not at selection -- so re-selecting an
        // enchantment in the panel can never silently reroll a pending temper.
        temperData.RollOutcome();
        if (temperData.success) {
            temperData.OnSuccess();
        } else {
            if (TemperData.CAN_DESTROY_ITEM && Random.value <= TemperData.DESTROY_CHANCE) {
                ItemDrop.ItemData itemToRemove = selectedItemElement._item;
                // Vanilla never auto-unequips a removed item: destroying an equipped piece without
                // this leaves Humanoid.m_*Item pointing at it (ghost stats and visuals).
                if (Player.m_localPlayer.IsItemEquiped(itemToRemove)) {
                    Player.m_localPlayer.UnequipItem(itemToRemove, false);
                }
                Player.m_localPlayer.GetInventory().RemoveOneItem(itemToRemove);
                ItemElement.elements.Remove(selectedItemElement);
                Destroy(selectedItemElement.gameObject);
                SetSelectedItem(null);
                UpdateLog(Localization.instance.Localize($"<color={TemperData.FAIL_RED}>{itemToRemove.GetDisplayName()} destroyed</color>"));
                temperDoneEffects.Create(Player.m_localPlayer.transform.position, Quaternion.identity);
                return;
            }

            temperData.OnFail();
        }

        selectedItemElement._magicItem = temperData.GetUpdatedMagicItem();
        selectedItemElement.uiTooltip.m_text = selectedItemElement._item.GetTooltip();
        FillEnchantmentList(selectedEnchantmentElement._effect.EffectType);

        temperDoneEffects.Create(Player.m_localPlayer.transform.position, Quaternion.identity);
    }
    public void ClearLog() {
        logTooltip.SetText("");
        logSB.Clear();
    }
    public void UpdateLog(string message) {
        logSB.AppendLine(message);
        logTooltip.SetText(logSB.ToString());
    }
    public void UpdateInfo() {
        if (temperData == null) {
            infoTooltip.SetText("");
        } else {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"$mod_epicloot_minmax: <color=yellow>[{temperData.selectedValues.MinValue}-{temperData.selectedValues.MaxValue}]</color>");
            sb.AppendLine($"$mod_epicloot_increment: <color=yellow>{temperData.selectedValues.Increment}</color>");
            sb.AppendLine($"$mod_epicloot_successchance: <color=yellow>{temperData.probability * 100.0f:F2}</color>%");
            sb.AppendLine($"$mod_epicloot_criticalsuccess: <color=yellow>{temperData.GetCriticalSuccessChance() * 100.0f:F2}</color>%");
            if (TemperData.CAN_DESTROY_ITEM) {
                sb.AppendLine($"$mod_epicloot_criticalfail: <color=yellow>{TemperData.DESTROY_CHANCE * 100.0f:F2}</color>%");
            }
            infoTooltip.SetText(Localization.instance.Localize(sb.ToString()));
        }
    }
    public void UpdateGamepadInput() {
        if (!ZInput.IsGamepadActive()) {
            selectedFrame.enabled = false;
            isItemListFocused = true;
            takeGamepadInput = false;
            gamepadPanelHints.SetActive(false);
        } else {
            bool didTakeGamepadInput = takeGamepadInput;

            gamepadPanelHints.SetActive(true);
            if (!takeGamepadInput && ZInput.GetButtonDown("JoyTabRight")) {
                selectedFrame.enabled = true;
                takeGamepadInput = true;
                isItemListFocused = true;
                SetSelectedItem(0);
            } else if (takeGamepadInput && ZInput.GetButtonDown("JoyTabLeft")) {
                transform.SetAsFirstSibling();
                isFirstChild = true;
                selectedFrame.enabled = false;
                takeGamepadInput = false;
                isItemListFocused = true;
                _storeGui.SelectItem(0, true);
                SetSelectedItem(null);
            }
            if (takeGamepadInput) {
                if (didTakeGamepadInput != takeGamepadInput) {
                    transform.SetAsLastSibling();
                    isFirstChild = false;
                    if (_storeGui.m_selectedItem != null) {
                        _storeGui.SelectItem(-1, false);
                    }
                }

                if (isItemListFocused) {
                    if (ItemElement.elements.Count <= 0) {
                        return;
                    }

                    if (!selectedItemElement) {
                        SetSelectedItem(0);
                    }

                    if (ZInput.GetButtonDown("JoyLStickDown") || ZInput.GetButtonDown("JoyDPadDown")) {
                        int index = selectedItemElement.index + 1;
                        if (index > ItemElement.elements.Count - 1) {
                            index = 0;
                        }

                        SetSelectedItem(index);
                    }

                    if (ZInput.GetButtonDown("JoyLStickUp") || ZInput.GetButtonDown("JoyDPadUp")) {
                        int index = selectedItemElement.index - 1;
                        if (index < 0) {
                            index = ItemElement.elements.Count - 1;
                        }
                        SetSelectedItem(index);
                    }

                    if (ZInput.GetButtonDown("JoyLStickRight") || ZInput.GetButtonDown("JoyDPadRight")) {
                        if (EnchantmentElement.elements.Count <= 0) {
                            return;
                        }
                        isItemListFocused = false;
                        SetSelectedEnchantment(0);
                    }
                } else {
                    if (EnchantmentElement.elements.Count <= 0) {
                        return;
                    }

                    if (EnchantmentElement.elements.All(x => !x.button.interactable)) {
                        return;
                    }

                    if (!selectedEnchantmentElement) {
                        SetSelectedEnchantment(0);
                    }

                    if (ZInput.GetButtonDown("JoyLStickDown") || ZInput.GetButtonDown("JoyDPadDown")) {
                        bool success = false;
                        int index = selectedEnchantmentElement.index + 1;

                        while (!success) {
                            if (index > EnchantmentElement.elements.Count - 1) {
                                index = 0;
                            }

                            if (EnchantmentElement.elements[index].button.interactable) {
                                SetSelectedEnchantment(index);
                                success = true;
                            } else {
                                index += 1;
                            }
                        }
                    }

                    if (ZInput.GetButtonDown("JoyLStickUp") || ZInput.GetButtonDown("JoyDPadUp")) {
                        bool success = false;
                        int index = selectedEnchantmentElement.index - 1;

                        while (!success) {
                            if (index < 0) {
                                index = EnchantmentElement.elements.Count - 1;
                            }

                            if (EnchantmentElement.elements[index].button.interactable) {
                                SetSelectedEnchantment(index);
                                success = true;
                            } else {
                                index -= 1;
                            }
                        }

                    }

                    if (ZInput.GetButtonDown("JoyLStickLeft") || ZInput.GetButtonDown("JoyDPadLeft")) {
                        isItemListFocused = true;
                        SetSelectedEnchantment(null);
                        if (selectedItemElement) {
                            SetSelectedItem(selectedItemElement);
                        }
                    }
                }

                if (ZInput.GetButton("JoyRStickUp")) {
                    if (ZInput.GetButton("JoyRTrigger")) {
                        infoTooltip._scrollbar.value = Mathf.Clamp01(infoTooltip._scrollbar.value + 0.1f);
                    } else {
                        logTooltip._scrollbar.value = Mathf.Clamp01(logTooltip._scrollbar.value + 0.1f);
                    }
                }

                if (ZInput.GetButton("JoyRStickDown")) {
                    if (ZInput.GetButton("JoyRTrigger")) {
                        infoTooltip._scrollbar.value = Mathf.Clamp01(infoTooltip._scrollbar.value - 0.1f);
                    } else {
                        logTooltip._scrollbar.value = Mathf.Clamp01(logTooltip._scrollbar.value - 0.1f);
                    }
                }
            } else {
                if (!isFirstChild || selectedItemElement) {
                    transform.SetAsFirstSibling();
                    isFirstChild = true;
                    SetSelectedItem(null);
                }
            }
        }
    }

    [HarmonyPatch(typeof(StoreGui), nameof(StoreGui.UpdateRecipeGamepadInput))]
    private static class StoreGui_UpdateRecipeGamepadInput_Patch {
        private static bool Prefix() {
            if (!Instance || !Instance.gameObject.activeSelf) {
                return true;
            }
            return !Instance.takeGamepadInput;
        }
    }

    public static void LoadFonts() {
        if (!fontsLoaded) {
            MagicFontManager.TMP_FontData averiaOutline = MagicFontManager.GetTMPFont(MagicFontManager.TMP_FontOptions.AveriaSansLibreOutline);
            MagicFontManager.TMP_FontData norseOutline = MagicFontManager.GetTMPFont(MagicFontManager.TMP_FontOptions.NorseBoldOutline);
            MagicFontManager.TMP_FontData averia = MagicFontManager.GetTMPFont(MagicFontManager.TMP_FontOptions.AveriaSansLibre);

            TextMeshProUGUI[] textMeshPros = EpicAssets.TemperPanel.GetComponentsInChildren<TextMeshProUGUI>(true);
            for (int i = 0; i < textMeshPros.Length; ++i) {
                TextMeshProUGUI tmp = textMeshPros[i];
                if (tmp.name == "Title") {
                    tmp.font = norseOutline.font;
                    tmp.fontSharedMaterial = norseOutline.material;

                } else if (tmp.transform.parent.name == "gamepad_hint") {
                    tmp.font = averia.font;
                    tmp.fontSharedMaterial = averia.material;
                } else {
                    tmp.font = averiaOutline.font;
                    tmp.fontSharedMaterial = averiaOutline.material;
                }
            }

            fontsLoaded = true;
        }
    }
}
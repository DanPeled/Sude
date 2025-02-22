using System.Collections;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
public class InventoryUI : MonoBehaviour
{
    List<ItemSlotUI> slotUIs;
    public GameObject itemList;
    public ItemSlotUI itemSlotUI;
    public Inventory inventory;
    int selectedItem = 0, selectedCategory = 0;
    public Image itemIcon;
    public TextMeshProUGUI descriptionText, categoryText;
    public RectTransform itemListRect;
    public PartyScreen partyScreen;
    const int itemsInViewport = 8;
    public Image upArrow, downArrow;
    InventoryUIState state;
    MoveBase moveToLearn;
    public MoveSelectionUI moveSelectionUI;
    Action<ItemBase> onItemUsed;
    void Awake()
    {
        inventory = Inventory.GetInventory();
        itemListRect = itemList.GetComponent<RectTransform>();
    }

    void Start()
    {
        UpdateItemList();
        inventory.onUpdated += UpdateItemList;
    }
    void Update()
    {
        if (gameObject.activeInHierarchy && GameController.instance.state == GameState.FreeRoam)
        {
            GameController.instance.state = GameState.Bag;
        }
    }
    public void UpdateItemList()
    {
        // Clear all the existing items
        foreach (Transform child in itemList.transform)
        {
            Destroy(child.gameObject);
        }
        slotUIs = new List<ItemSlotUI>();
        // if (inventory.GetSlotsByCategory(selectedCategory).Count > 0)
            foreach (var itemSlot in inventory.GetSlotsByCategory(selectedCategory))
            {
                var slot = Instantiate(itemSlotUI, itemList.transform);
                slot.SetData(itemSlot);

                slotUIs.Add(slot);
            }

        UpdateItemSelection();
    }
    public void HandleUpdate(Action onBack, Action<ItemBase> onItemUsed = null)
    {
        this.onItemUsed = onItemUsed;
        if (state == InventoryUIState.ItemSelection)
        {
            int prevSelection = selectedItem;
            int prevCategory = selectedCategory;
            if (InputSystem.down.isClicked())
            {
                selectedItem++;
            }
            else if (InputSystem.up.isClicked())
            {
                selectedItem--;
            }
            else if (InputSystem.right.isClicked())
            {
                selectedCategory++;
            }
            else if (InputSystem.left.isClicked())
            {
                selectedCategory--;
            }

            if (selectedCategory > Inventory.ItemCategories.Count - 1)
            {
                selectedCategory = 0;
            }
            else if (selectedCategory < 0)
            {
                selectedCategory = Inventory.ItemCategories.Count - 1;
            }
            selectedCategory = Mathf.Clamp(selectedCategory, 0, Inventory.ItemCategories.Count - 1);
            if (prevCategory != selectedCategory)
            {
                ResetSelection();
                categoryText.text = Inventory.ItemCategories[selectedCategory];
                UpdateItemList();
            }
            else if (prevSelection != selectedItem)
                UpdateItemSelection();
            if (InputSystem.action.isClicked())
            {
                StartCoroutine(ItemSelected());
            }
            else if (InputSystem.back.isClicked())
            {
                onBack?.Invoke();
            }
        }
        else if (state == InventoryUIState.PartySelection)
        {
            // Handle Party selection
            Action onSelected = () =>
            {
                // use the item on the selected creature
                StartCoroutine(UseItem());
            };
            Action onBackPartyScreen = () =>
            {
                ClosePartyScreen();
            };
            partyScreen.HandleUpdate(onSelected, onBackPartyScreen);
        }
        else if (state == InventoryUIState.MoveToForget)
        {

            Action<int> onMoveSelected = (int moveIndex) =>
            {
                StartCoroutine(onMoveToForgetSelected(moveIndex));
            };


            moveSelectionUI.HandleMoveSelection(onMoveSelected);
        }
    }
    IEnumerator ItemSelected()
    {
        state = InventoryUIState.Busy;
        var item = inventory.GetItem(selectedItem, selectedCategory);
        if (GameController.instance.state == GameState.Shop)
        {
            onItemUsed?.Invoke(item);
            state = InventoryUIState.ItemSelection;
            yield break;
        }
        if (GameController.instance.battleSystem.gameObject.activeInHierarchy)
        {
            // In a battle
            if (!item.CanUseInBattle)
            {
                // dont allow to use item
                yield return DialogManager.instance.ShowDialogText($"This item cannot be used in battle");
                state = InventoryUIState.ItemSelection;
                yield break;
            }
        }
        else
        {
            // outside a battle
            if (!item.CanUseOutsideBattle)
            {
                // dont allow to use item
                yield return DialogManager.instance.ShowDialogText($"This item cannot be used outside battle");
                state = InventoryUIState.ItemSelection;
                yield break;
            }

        }

        if (selectedCategory == (int)ItemCategory.Hexoballs)
        {
            // Hexoball
            StartCoroutine(UseItem());
        }
        else
        {
            OpenPartyScreen();

            if (item is TMItem)
            {
                // Show if the tm is usable
                partyScreen.ShowIfTmIsUsable(item as TMItem);
            }
        }
    }
    IEnumerator UseItem()
    {
        state = InventoryUIState.Busy;

        yield return HandleTMItems();
        var item = inventory.GetItem(selectedItem, selectedCategory);
        var creature = partyScreen.SelectedMember;
        // handle evolution items
        if (item is EvolutionItem)
        {
            var evo = creature.CheckForEvolution(item);
            if (evo != null)
            {
                yield return EvolutionManager.instance.Evolve(creature, evo);
            }
            else
            {
                yield return DialogManager.instance.ShowDialogText($"It won't have any effect!");
                ClosePartyScreen();
                yield break;
            }
        }
        var usedItem = inventory.UseItem(selectedItem, partyScreen.SelectedMember, selectedCategory);
        if (usedItem != null)
        {
            if (usedItem is RecoveryItem)
                yield return DialogManager.instance.ShowDialogText($"{Player.instance.playerName} used {usedItem.name}!");
            onItemUsed?.Invoke(usedItem);
        }
        else
        {
            if (selectedCategory == (int)ItemCategory.Items)
                yield return DialogManager.instance.ShowDialogText($"It won't have any effect!");
        }
        ClosePartyScreen();

    }

    IEnumerator HandleTMItems()
    {
        var tmItem = inventory.GetItem(selectedItem, selectedCategory) as TMItem;
        if (tmItem == null)
        {
            yield break;
        }
        var creature = partyScreen.SelectedMember;

        if (creature.HasMove(tmItem.move))
        {
            yield return DialogManager.instance.ShowDialogText($"{creature.GetName()} already knows {tmItem.move.name}");
            yield break;
        }
        if (!tmItem.CanBeTaught(creature))
        {
            yield return DialogManager.instance.ShowDialogText($"{creature.GetName()} can't learn {tmItem.move.name}");
            yield break;
        }
        if (creature.moves.Count < creature._base.maxNumberOfMoves)
        {

            creature.LearnMove(tmItem.move);
            yield return DialogManager.instance.ShowDialogText($"{creature.GetName()} learned {tmItem.move.name}");
        }
        else
        {
            yield return DialogManager.instance.ShowDialogText($"{creature.GetName()} is trying to learn {tmItem.move.name}");
            yield return DialogManager.instance.ShowDialogText($"But it cannot learn more than 4 moves");
            yield return ChooseMoveToForget(creature, tmItem.move);
            yield return new WaitUntil(() => state != InventoryUIState.MoveToForget);
        }

    }
    IEnumerator ChooseMoveToForget(Creature creature, MoveBase newMove)
    {
        state = InventoryUIState.Busy;
        yield return DialogManager.instance.ShowDialogText($"Choose a move you want to forget", autoClose: false);
        moveSelectionUI.gameObject.SetActive(true);
        moveSelectionUI.SetMoveData(creature.moves.Select(x => x.base_).ToList(), newMove);
        moveToLearn = newMove;
        state = InventoryUIState.MoveToForget;
    }

    void UpdateItemSelection()
    {
        var slots = inventory.GetSlotsByCategory(selectedCategory);
        selectedItem = Mathf.Clamp(selectedItem, 0, slots.Count - 1);
        if (slotUIs.Count > slots.Count)
        {
            Destroy(slotUIs[0]);
        }
        if (slotUIs.Count > 0)
            for (int i = 0; i < slotUIs.Count; i++)
            {
                if (i == selectedItem)
                {
                    slotUIs[i].nameText.color = GlobalSettings.i.highlightedColor;
                }
                else
                {
                    slotUIs[i].nameText.color = Color.black;
                }
            }

        if (slots.Count > 0)
        {
            itemIcon.gameObject.SetActive(true);
            var item = slots[selectedItem].item;
            itemIcon.color = Color.white;
            itemIcon.sprite = item.icon;
            descriptionText.text = item.description;
        }
        else
        {
            itemIcon.gameObject.SetActive(false);
            descriptionText.text = "";
        }
        HandleScrolling();
    }
    void HandleScrolling()
    {
        if (slotUIs.Count <= itemsInViewport)
        {
            downArrow.gameObject.SetActive(false);
            upArrow.gameObject.SetActive(false);
            return;
        }
        float scrollPos = Mathf.Clamp(selectedItem - itemsInViewport / 2, 0, selectedItem) * slotUIs[0].height;
        itemListRect.localPosition = new Vector2(itemListRect.localPosition.x - 22, scrollPos);

        bool showUpArrow = selectedItem > itemsInViewport / 2;
        upArrow.gameObject.SetActive(showUpArrow);
        bool showDownArrow = selectedItem + 4 < slotUIs.Count;
        downArrow.gameObject.SetActive(showDownArrow);
    }
    void ResetSelection()
    {
        selectedItem = 0;

        upArrow.gameObject.SetActive(false);
        downArrow.gameObject.SetActive(false);
        itemIcon.color = new Color(0, 0, 0, 0);
        itemIcon.sprite = null;
        descriptionText.text = "";
    }
    void OpenPartyScreen()
    {
        state = InventoryUIState.PartySelection;
        partyScreen.gameObject.SetActive(true);
    }
    void ClosePartyScreen()
    {
        state = InventoryUIState.ItemSelection;

        partyScreen.ClearMemberSlotMessages();
        partyScreen.gameObject.SetActive(false);
    }
    IEnumerator onMoveToForgetSelected(int moveIndex)
    {
        var creature = partyScreen.SelectedMember;
        moveSelectionUI.gameObject.SetActive(false);

        DialogManager.instance.CloseDialog();
        if (moveIndex == 4)
        {
            // Dont learn the new move
            yield return (DialogManager.instance.ShowDialogText($"{creature.GetName()} did not learn {moveToLearn.name}"));
        }
        else
        {
            // forget the selected move and learn new move
            var selectedMove = creature.moves[moveIndex].base_;
            yield return (DialogManager.instance.ShowDialogText($"{creature.GetName()} forgot {selectedMove.name} and learned {moveToLearn.name}"));

            creature.moves[moveIndex] = new Move(moveToLearn);
        }
        moveToLearn = null;
        state = InventoryUIState.ItemSelection;
    }
}
public enum InventoryUIState
{
    ItemSelection, PartySelection, Busy, MoveToForget
}
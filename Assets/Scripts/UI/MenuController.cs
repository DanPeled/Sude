using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;

public class MenuController : MonoBehaviour
{
    public GameObject menu;
    List<TextMeshProUGUI> menuItems;
    public event Action<int> onMenuSelected;
    public event Action onBack;
    int selectedItem = 0;

    void Awake()
    {
        menuItems = menu.GetComponentsInChildren<TextMeshProUGUI>().ToList();
    }

    void CloseMenu()
    {
        Time.timeScale = 1;
        menu.SetActive(false);
    }

    public void OpenMenu()
    {
        menu.SetActive(true);
        UpdateItemSelection();
    }

    public void HandleUpdate()
    {
        int prevSelection = selectedItem;
        if (InputSystem.down.isClicked())
        {
            selectedItem++;
        }
        else if (InputSystem.up.isClicked())
        {
            selectedItem--;
        }
        selectedItem = Mathf.Clamp(selectedItem, 0, menuItems.Count - 1);
        if (prevSelection != selectedItem)
            UpdateItemSelection();

        if (InputSystem.action.isClicked())
        {
            onMenuSelected?.Invoke(selectedItem);
            CloseMenu();
        }
        else if (InputSystem.back.isClicked())
        {
            onBack?.Invoke();
            CloseMenu();
        }
    }

    void UpdateItemSelection()
    {
        for (int i = 0; i < menuItems.Count; i++)
        {
            if (i == selectedItem)
            {
                menuItems[i].color = GlobalSettings.i.highlightedColor;
            }
            else
            {
                menuItems[i].color = Color.black;
            }
        }
    }

    void Update()
    {
        if (Player.instance != null && Player.instance.console.activeInHierarchy)
        {
            CloseMenu();
        }
    }
}

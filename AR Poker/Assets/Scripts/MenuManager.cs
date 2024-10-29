using UnityEngine;
using System.Collections.Generic;
using Unity.VisualScripting;

public class MenuManager : MonoBehaviour
{
    public GameObject mainMenu;
    public GameObject singlePlayerMenu;
    public GameObject quitConfirmation;

    private Stack<GameObject> menuStack = new Stack<GameObject>();

    void Start()
    {
        ShowMainMenu();
    }

    // Update is called once per frame
    public void ShowMainMenu()
    {
        mainMenu.SetActive(true);
        HideAllMenus();
        menuStack.Clear();
    }

    public void ShowSinglePlayerMenu()
    {
        ShowMenu(singlePlayerMenu);
        mainMenu.SetActive(false);
    }

    public void ShowQuitConfirmation()
    {
        ShowMenu(quitConfirmation);
        mainMenu.SetActive(false);
    }

    private void ShowMenu(GameObject menu)
    {
        if(menuStack.Count > 0)
        {
            menuStack.Peek().SetActive(false);
        }
        menu.SetActive(true);
        menuStack.Push(menu);
    }

    private void HideAllMenus()
    {
        singlePlayerMenu.SetActive(false);
        quitConfirmation.SetActive(false);
    }

    public void OnBackButtonPressed()
    {
        if(menuStack.Count > 1)
        {
            menuStack.Pop().SetActive(false);
            menuStack.Peek().SetActive(true);
        }
        else
        {
            ShowMainMenu();
        }
    }


}

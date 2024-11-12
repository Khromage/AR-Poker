using UnityEngine;
using System.Collections.Generic;
using Unity.VisualScripting;

public class MenuManager : MonoBehaviour
{

    // portrait game objects
    public GameObject mainMenu_portrait;
    public GameObject singlePlayerMenu_portrait;
    public GameObject quitConfirmation_portrait;
    //public GameObject singlePlayerMenuStart_portrait;
    public GameObject singlePlayerGameType_portrait;
    public GameObject singlePlayerAddPC_portrait;

    //landscape game objects
    public GameObject mainMenu_landscape;
    public GameObject singlePlayerMenu_landscape;
    public GameObject quitConfirmation_landscape;
    //public GameObject singlePlayerMenuStart_landscape;
    public GameObject singlePlayerGameType_landscape;
    public GameObject singlePlayerAddPC_landscape;

    // tracking
    private GameObject currentMenu;
    private GameObject previousMenu; // for back button
    private bool isPortrait;


    void Start() 
    {
        CheckOrientation();
        Debug.Log("Is this fucking working???");
    }

    void Update() 
    {
        /*
        if (isPortrait && Screen.width > Screen.height){
            SwitchToLandscape();
        }
        else if (!isPortrait && Screen.height > Screen.width)
        {
            SwitchToPortrait();
        }*/
        CheckOrientation();

    }

    private void CheckOrientation() 
    {
        if (isPortrait && Screen.width > Screen.height){
            SwitchToLandscape();
        }
        else if (!isPortrait && Screen.height > Screen.width)         
        {
            SwitchToPortrait();
        }
    }
    
    private void SwitchToPortrait() 
    {
        isPortrait = true;
        ShowCurrentMenu();
    }

    private void SwitchToLandscape()
    {
        isPortrait = false;
        ShowCurrentMenu();
    }
    
    private void ShowCurrentMenu()
    {
        if(currentMenu == null)
        {
            ShowMainMenu();
        }
        else 
        {
            if (currentMenu == mainMenu_portrait || currentMenu == mainMenu_landscape)
            {
                ShowMainMenu();
            }
            else if (currentMenu == singlePlayerMenu_portrait || currentMenu == singlePlayerMenu_landscape)
            {
                ShowSinglePlayerMenu();
            }
            else if (currentMenu == quitConfirmation_portrait || currentMenu == quitConfirmation_landscape)
            {
                ShowQuitConfirmation();
            }
        }
    }

    public void ShowMainMenu()
    {
        setActiveMenu(isPortrait ? mainMenu_portrait : mainMenu_landscape);
        Debug.Log("In main menu");

    }

    public void ShowSinglePlayerMenu()
    {
        setActiveMenu(isPortrait ? singlePlayerMenu_portrait : singlePlayerMenu_landscape);
    }

    public void ShowSinglePlayerGameType()
    {
        setActiveMenu(isPortrait ? singlePlayerGameType_portrait : singlePlayerGameType_landscape);
    }

    public void ShowSinglePlayerAddPC()
    {
        setActiveMenu(isPortrait ? singlePlayerAddPC_portrait : singlePlayerAddPC_landscape );
    }

    public void ShowQuitConfirmation()
    {
        setActiveMenu(isPortrait ? quitConfirmation_portrait : quitConfirmation_landscape);
    }

    private void setActiveMenu(GameObject newMenu)
    {
        if (currentMenu != null)
        {
            previousMenu = currentMenu;
            currentMenu.SetActive(false);
        }
        currentMenu = newMenu;
        currentMenu.SetActive(true);
    }

    public void OnBackButtonPressed()
    {
        if (previousMenu != null)
        {
            currentMenu.SetActive(false);
            currentMenu = previousMenu;
            previousMenu = null;
            currentMenu.SetActive(true);
        }
        else
        {
            ShowMainMenu();
        }
    }


}

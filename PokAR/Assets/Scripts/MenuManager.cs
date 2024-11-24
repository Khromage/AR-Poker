using UnityEngine;
using System.Collections.Generic;
using Unity.VisualScripting;
using System;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{

    // Define the delegate for the event
    public static event EventHandler<GameStartEventData> OnGameStart;

    // portrait game objects
    public GameObject mainMenu_portrait;
    public GameObject singlePlayerMenu_portrait;
    public GameObject quitConfirmation_portrait;
    //public GameObject singlePlayerMenuStart_portrait;
    public GameObject singlePlayerGameType_portrait;
    public GameObject singlePlayerAddPC_portrait;
    public GameObject multiPlayerMenu_portrait;
    public GameObject multiPlayerHostCodeView_portrait;
    public GameObject multiplayerJoinRoomView_portrait;

    //landscape game objects
    public GameObject mainMenu_landscape;
    public GameObject singlePlayerMenu_landscape;
    public GameObject quitConfirmation_landscape;
    //public GameObject singlePlayerMenuStart_landscape;
    public GameObject singlePlayerGameType_landscape;
    public GameObject singlePlayerAddPC_landscape;
    public GameObject multiPlayerMenu_landscape;
    public GameObject multiPlayerHostCodeView_landscape;
    public GameObject multiplayerJoinRoomView_landscape;

    // tracking
    private GameObject currentMenu;
    private GameObject previousMenu; // for back button
    private bool isPortrait;

    private string gameDifficulty;


    void Start() 
    {
        CheckOrientation();
        //Debug.Log("Is this fucking working???");
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
            else if (currentMenu == singlePlayerAddPC_portrait || currentMenu == singlePlayerAddPC_landscape)
            {
                ShowSinglePlayerAddPC();
            }
            else if (currentMenu == multiPlayerMenu_portrait || currentMenu == multiPlayerMenu_landscape)
            {
                ShowMultiPlayerMenu();
            }
            else if (currentMenu == multiPlayerHostCodeView_portrait || currentMenu == multiPlayerHostCodeView_landscape)
            {
                ShowHostCodeView();
            }
            else if (currentMenu == multiplayerJoinRoomView_portrait || currentMenu == multiplayerJoinRoomView_landscape)
            {
                ShowJoinRoomView();
            }
        }
    }

    public void ShowMainMenu() // good
    {
        setActiveMenu(isPortrait ? mainMenu_portrait : mainMenu_landscape);
        Debug.Log("In main menu");

    }

    public void ShowSinglePlayerMenu() // good
    {
        setActiveMenu(isPortrait ? singlePlayerMenu_portrait : singlePlayerMenu_landscape);
    }


    // update difficulty string
    public void SetDifficulty(GameObject difficultyButton)
    {
        gameDifficulty = difficultyButton.gameObject.name.ToString();
        Debug.Log($"SinglePlayer difficulty set to: {gameDifficulty}");
    }

    public void ShowSinglePlayerAddPC() //good
    {
        setActiveMenu(isPortrait ? singlePlayerAddPC_portrait : singlePlayerAddPC_landscape );
    }

    public void ShowQuitConfirmation() // good
    {
        setActiveMenu(isPortrait ? quitConfirmation_portrait : quitConfirmation_landscape);
    }

    public void ShowMultiPlayerMenu()
    {
        setActiveMenu(isPortrait ? multiPlayerMenu_portrait : multiPlayerMenu_landscape);
    }

    public void ShowHostCodeView()
    {
        setActiveMenu(isPortrait ? multiPlayerHostCodeView_portrait : multiPlayerHostCodeView_landscape);
    }

    public void ShowJoinRoomView()
    {
        setActiveMenu(isPortrait ? multiplayerJoinRoomView_portrait : multiplayerJoinRoomView_landscape);
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


    public void StartSinglePlayerGame(GameObject npcSlider)
    {
        int npcCount = (int)npcSlider.GetComponent<Slider>().value;

        // Create event data
        GameStartEventData eventData = new GameStartEventData("Single", gameDifficulty, npcCount);
        
        // Invoke the event
        Debug.Log($"Broadcasting StartGameEvent via MenuManager with params: Single, {gameDifficulty}, {npcCount}");
        OnGameStart?.Invoke(this, eventData);
    }

    public void QuitGame()
    {
        Application.Quit();
    }

}

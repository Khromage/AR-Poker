using UnityEngine;
using System.Collections.Generic;
using Unity.VisualScripting;
using System;
using UnityEngine.UI;
using TMPro;

using Fusion;

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


    // gameplay UI
    public GameObject gameplay_portrait;
    public GameObject gameplay_landscape;
    public GameObject gameMenu_portrait;
    public GameObject gameMenu_landscape;
    public GameObject gameMenu_tableConfirmation;

    // tracking
    private GameObject currentMenu;
    private GameObject previousMenu; // for back button
    private bool isPortrait;
    private bool isGameOn;
    private string gameDifficulty;



    void Start() 
    {
        ShowCurrentMenu();
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
        if (isGameOn)
        {
            ShowGameUI();
        }
        else
        {
            ShowCurrentMenu();
        }
    }

    private void SwitchToLandscape()
    {
        isPortrait = false;
        if (isGameOn)
        {
            ShowGameUI();
        }
        else
        {
            ShowCurrentMenu();    
        }
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

    public void ShowTablePlacementConfirm() // activitaing confirmation button
    {
        setActiveMenu(gameMenu_tableConfirmation);

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
        // should i remove this part?
        int npcCount = (int)npcSlider.GetComponent<Slider>().value;

        // Create event data
        GameStartEventData eventData = new GameStartEventData("Single", gameDifficulty, npcCount);
        
        // Invoke the event
        Debug.Log($"Broadcasting StartGameEvent via MenuManager with params: Single, {gameDifficulty}, {npcCount}");
        OnGameStart?.Invoke(this, eventData);
        
        //currentMenu.SetActive(false);

        ShowTablePlacementConfirm();
        isGameOn = true;

        //ShowGameUI();
    }

    public void StartMultiplayerHost()
    {
        GameManager.Instance.CurrentGame = Instantiate(GameManager.Instance.MultiPlayerPrefab);
        var multiPlayerManager = GameManager.Instance.CurrentGame.GetComponent<MultiPlayerGameManager>();
        multiPlayerManager.Initialize();

        // Display the generated code on the host UI
        ShowHostCodeView();
        Debug.Log($"Generated Lobby Code: {multiPlayerManager.GeneratedCode}");
    }

    public void JoinMultiplayerGame(InputField inputField)
    {
        string lobbyCode = inputField.text.ToUpper();
        if (lobbyCode.Length == 4)
        {
            Debug.Log($"Attempting to join lobby: {lobbyCode}");
            var multiPlayerManager = GameManager.Instance.CurrentGame.GetComponent<MultiPlayerGameManager>();
            multiPlayerManager.JoinRoom(lobbyCode);
        }
        else
        {
            Debug.LogError("Invalid lobby code entered!");
        }
    }

    public void EndGame()
    {
        Debug.Log("Game Ended");

        if (GameManager.Instance != null)
        {
            GameManager.Instance.EndCurrentGame();
        }

        isGameOn = false;
        gameDifficulty = null;

        if (gameplay_portrait != null) gameplay_portrait.SetActive(false);
        if (gameplay_landscape != null) gameplay_landscape.SetActive(false);


        ShowMainMenu();
    }

    public void ShowGameUI()
    {
        gameMenu_tableConfirmation.SetActive(false);
        if(isPortrait)
        {
            gameplay_landscape.SetActive(false);
            gameplay_portrait.SetActive(true);
        }
        else
        {
            gameplay_portrait.SetActive(false);
            gameplay_landscape.SetActive(true);
        }
    }

    public void ConfirmPress()
    {
        Debug.Log(GameManager.Instance);
        GameObject currentGame = GameManager.Instance.CurrentGame;
        Debug.Log(currentGame.name);

        var spg = currentGame.GetComponent<SinglePlayerGameManager>(); 
        if(spg != null)
        {
            spg.ConfirmPlacement();
        }
        ShowGameUI();
    }

    public void ShowGameMenu()
    {
        setActiveMenu(isPortrait ? gameMenu_portrait : gameMenu_landscape);
    }

    public void QuitGame()
    {
        Application.Quit();
    }

}

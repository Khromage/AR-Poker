using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.EventSystems;
using ExitGames.Client.Photon.StructWrapping;

public class SinglePlayerGameManager : MonoBehaviour
{
    [SerializeField]
    protected CasinoTables TableAssets;
    [SerializeField]
    protected CasinoChips ChipAssets;
    [SerializeField]
    protected PlayingCards CardAssets;
    [SerializeField]
    protected PlayerAvatars AvatarAssets;

    [SerializeField]
    private string difficulty;
    private int startingBalance;
    private int NPCstartingBalance;


    [SerializeField]
    private int numNPC;

    [SerializeField]
    private Player[] Players;

    private List<GameObject> activeCards = new List<GameObject>();
    private GameObject gameTable;
    private List<GameObject> chips = new List<GameObject>();

    private List<GameObject> communityCards = new List<GameObject>();
    private List<GameObject> playerCards = new List<GameObject>();
    private List<GameObject>[] npcCards;

    private List<Card> deck = new List<Card>();
    private Material CardBackMaterial;

    private ARRaycastManager arRaycastManager;
    private bool gameSetupStarted = false;

    private static List<ARRaycastHit> hits = new List<ARRaycastHit>();
    [SerializeField]
    private GameObject placementIndicator;
    private bool isPlacementIndicatorActive = false;

    [SerializeField]
    private GameObject placementIndicatorPrefab;  // Prefab for the visual indicator

    private Vector3 confirmedPosition;
    private Quaternion confirmedRotation;

    [System.Serializable]
    public struct Card
    {
        public string Suit;
        public int Value;
    }
    [System.Serializable]
    public struct Player
    {
        public bool isNPC;
        public int Balance;
        public Card[] Hand;

        public Player(bool isnpc, int balance)
        {
            isNPC = isnpc;
            Balance = balance;
            Hand = new Card[2]; // Initialize array with size 2
        }
    }

    public void Initialize(string gameDifficulty, int npcCount)
    {
        difficulty = gameDifficulty;
        switch(difficulty)
        {
            case "Beginner":
                startingBalance = 120;
                break;
            case "Gambler":
                startingBalance = 100;
                break;
            case "Pro":
                startingBalance = 80;
                break;    
        }
        numNPC = npcCount;
        npcCards = new List<GameObject>[numNPC];
        for (int i = 0; i < numNPC; i++)
        {
            npcCards[i] = new List<GameObject>();
        }
        Debug.Log("Single Player Manager PARAMS were INITIALIZED");
    }

    void Start()
    {
        Debug.Log("Single Player Manager WAS CREATED & START RAN");
        arRaycastManager = FindObjectOfType<ARRaycastManager>();

        // Instantiate the placement indicator and deactivate it initially
        placementIndicator = Instantiate(placementIndicatorPrefab, gameObject.transform);
        isPlacementIndicatorActive = true;
        //placementIndicator.SetActive(false);
    }

    void Update()
    {
        // replaced with button
        if (Keyboard.current.cKey.wasPressedThisFrame)
        {
            ConfirmPlacement();
        }

        if (!gameSetupStarted)
        {
            bool inputDetected = false;

            if (Touchscreen.current != null)
            {
                var touch = Touchscreen.current.primaryTouch;

                if (touch.press.isPressed)
                {
                    Vector2 touchPosition = touch.position.ReadValue();

                    // Check if the touch is over a UI element
                    if (!IsPointerOverUIObject(touchPosition))
                    {
                        UpdatePlacementPose(touchPosition);
                        inputDetected = true;
                    }
                }
            }

            if (!inputDetected && Mouse.current != null)
            {
                var mouse = Mouse.current;

                if (mouse.leftButton.isPressed)
                {
                    Vector2 mousePosition = mouse.position.ReadValue();

                    // Check if the mouse position is over a UI element
                    if (!IsPointerOverUIObject(mousePosition))
                    {
                        UpdatePlacementPose(mousePosition);
                        inputDetected = true;
                    }
                }
            }

            if (!inputDetected)
            {
                // Deactivate the placement indicator if there's no input
                //placementIndicator.SetActive(false);
                //isPlacementIndicatorActive = false;
            }
        }
    }

    private void UpdatePlacementPose(Vector2 screenPosition)
    {
        if (arRaycastManager.Raycast(screenPosition, hits, TrackableType.Planes))
        {
            Pose hitPose = hits[0].pose;

            // Activate the placement indicator
            if (!placementIndicator.activeInHierarchy)
            {
                placementIndicator.SetActive(true);
            }

            isPlacementIndicatorActive = true;

            // Movement smoothing
            float smoothingSpeed = 10f;
            placementIndicator.transform.position = Vector3.Lerp(placementIndicator.transform.position, hitPose.position, Time.deltaTime * smoothingSpeed);
            placementIndicator.transform.rotation = Quaternion.Lerp(placementIndicator.transform.rotation, hitPose.rotation, Time.deltaTime * smoothingSpeed);
        }
        else
        {
            // Deactivate the placement indicator if no valid plane is detected
            //placementIndicator.SetActive(false);
            //sPlacementIndicatorActive = false;
        }
    }

    private bool IsPointerOverUIObject(Vector2 screenPosition)
    {
        PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current)
        {
            position = screenPosition
        };
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventDataCurrentPosition, results);
        return results.Count > 0;
    }

    public void ConfirmPlacement()
    {
        Debug.Log("CONFIRMINGGGGGGGGGG");
        if (isPlacementIndicatorActive)
        {
            Debug.Log("GOING IN");
            // Store the confirmed position and rotation
            confirmedPosition = placementIndicator.transform.position;
            confirmedRotation = placementIndicator.transform.rotation;

            // Place the table at the confirmed location and start the game
            SetupTable(confirmedPosition, confirmedRotation);
            SetupGame();

            // Disable further changes to the placement indicator
            placementIndicator.SetActive(false);
            gameSetupStarted = true;

                    // Call ShowGameUI from the MenuManager
                    /*
            MenuManager menuManager = FindObjectOfType<MenuManager>();
            if (menuManager != null)
            {
                Debug.Log("yo is this workign");
                menuManager.ShowGameUI();
            }
            else
            {
                Debug.Log("yup, not working");
            }
            */
        }
    }

    private void SetupGame()
    {
        // Note: Removed redundant call to SetupTable since it's already called in ConfirmPlacement()
        SetupDeck();
        SetupPlayers();
        //SetupChips();
        StartCoroutine(GameLoop());
    }

    private void SetupPlayers()
    {
        Players = new Player[numNPC+1];

        // User Player
        Players[0] = new Player(false, startingBalance);
        // NPC Players
        for(int i=1; i<=numNPC; i++)
        {
            Players[i] = new Player(true, NPCstartingBalance);
        }

        DealCards();
        
        List<int> usedAvatarIndeces = new List<int>();
        for(int player=0; player < Players.Length; player++)
        {
            int avatarPrefabIndex = UnityEngine.Random.Range(0, AvatarAssets.Avatars.Length);
            while(usedAvatarIndeces.Contains(avatarPrefabIndex))
            {
                avatarPrefabIndex = UnityEngine.Random.Range(0, AvatarAssets.Avatars.Length);
            }


            Transform spawnLocation = gameTable.transform.GetChild(player+1).GetChild(3).transform;

            GameObject playerAvatar = Instantiate(AvatarAssets.Avatars[avatarPrefabIndex], spawnLocation.position, spawnLocation.rotation);

            usedAvatarIndeces.Add(avatarPrefabIndex);
        }


    }

    private void SetupTable(Vector3 position, Quaternion rotation)
    {
        gameTable = Instantiate(TableAssets.Tables[0].Prefab, position, rotation, gameObject.transform);
        Debug.Log("Table has been set up.");
    }

    private void SetupChips()
    {
        foreach (Chip chip in ChipAssets.Chips)
        {
            GameObject chipObj = Instantiate(ChipAssets.Prefab, gameObject.transform);
            chipObj.GetComponent<Renderer>().material = chip.material;
            chips.Add(chipObj);
        }
        Debug.Log("Chips have been set up.");
    }

    private void SetupDeck()
    {
        // Random int is exclusive range, so 0-3
        CardBackMaterial = CardAssets.Backs[UnityEngine.Random.Range(0, 3)];
        string[] suits = { CardAssets.Suits[0].Name, CardAssets.Suits[1].Name, CardAssets.Suits[2].Name, CardAssets.Suits[3].Name };
        for (int i = 0; i < suits.Length; i++)
        {
            for (int j = 1; j <= 13; j++)
            {
                Card card = new Card { Suit = suits[i], Value = j };
                deck.Add(card);
            }
        }
        Debug.Log("Deck has been initialized.");
    }

    // Deal cards to players
    private void DealCards()
    {
        // Deal two cards to the player
        for (int i = 0; i < 2; i++)
        {
            Card card = DrawCard();
            GameObject cardObj = Instantiate(CardAssets.Prefab, gameObject.transform);
            cardObj.transform.GetChild(0).GetComponent<Renderer>().material = GetCardMaterial(card);
            cardObj.transform.GetChild(1).GetComponent<Renderer>().material = CardBackMaterial;
            
            // Update Players Array
            Players[0].Hand[i] = card;
            
            //playerCards.Add(cardObj);
            activeCards.Add(cardObj);

            // Set card on tabe in player's position
            cardObj.transform.localScale /= 2;
            cardObj.transform.position = gameTable.transform.GetChild(1).GetChild(0).GetChild(i).transform.position;
            cardObj.transform.rotation = gameTable.transform.GetChild(1).GetChild(0).GetChild(i).transform.rotation;
        }

        // Deal two cards to each NPC
        for (int npc = 1; npc < Players.Length; npc++)
        {
            for (int i = 0; i < 2; i++)
            {
                Card card = DrawCard();
                GameObject cardObj = Instantiate(CardAssets.Prefab, gameObject.transform);
                cardObj.transform.GetChild(0).GetComponent<Renderer>().material = GetCardMaterial(card);
                cardObj.transform.GetChild(1).GetComponent<Renderer>().material = CardBackMaterial;
                
                // Update Players Array
                Players[npc].Hand[i] = card;

                //npcCards[npc].Add(cardObj);
                activeCards.Add(cardObj);

                // Set card on tabe in player's position
                cardObj.transform.localScale /= 2;
                cardObj.transform.position = gameTable.transform.GetChild(npc+1).GetChild(0).GetChild(i).transform.position;
                cardObj.transform.rotation = gameTable.transform.GetChild(npc+1).GetChild(0).GetChild(i).transform.rotation;
            }
        }
        Debug.Log("Cards have been dealt.");
    }

    // Set up community cards (the flop, turn, and river)
    private void DealCommunityCards(int numberOfCards)
    {
        for (int i = 0; i < numberOfCards; i++)
        {
            Card card = DrawCard();
            GameObject cardObj = Instantiate(CardAssets.Prefab, gameObject.transform);
            cardObj.transform.GetChild(0).GetComponent<Renderer>().material = GetCardMaterial(card);
            cardObj.transform.GetChild(1).GetComponent<Renderer>().material = CardBackMaterial;
            communityCards.Add(cardObj);
            activeCards.Add(cardObj);
        }
        Debug.Log("Community cards have been dealt.");
    }

    // Draw a card from the deck
    private Card DrawCard()
    {
        if (deck.Count == 0)
        {
            Debug.LogError("Deck is empty!");
            return default;
        }

        int randomIndex = UnityEngine.Random.Range(0, deck.Count);
        Card drawnCard = deck[randomIndex];
        deck.RemoveAt(randomIndex);
        return drawnCard;
    }

    // Get the material for a specific card
    private Material GetCardMaterial(Card card)
    {
        int suitIndex = System.Array.IndexOf(new string[] { "Hearts", "Diamonds", "Clubs", "Spades" }, card.Suit);
        if (suitIndex >= 0 && suitIndex < CardAssets.Suits.Length)
        {
            if (card.Value >= 1 && card.Value < CardAssets.Suits[suitIndex].Faces.Length)
            {
                return CardAssets.Suits[suitIndex].Faces[card.Value];
            }
        }
        Debug.LogError("Invalid card suit or value!");
        return null;
    }

    // Get a card struct from a material (reverse lookup)
    private Card GetCardFromMaterial(Material mat)
    {
        for (int suitIndex = 0; suitIndex < CardAssets.Suits.Length; suitIndex++)
        {
            Debug.Log("SEARCHING SUITS FOR CARD FROM MAT");
            for (int faceIndex = 1; faceIndex < CardAssets.Suits[suitIndex].Faces.Length; faceIndex++)
            {
                Debug.Log("SEARCHING CARDS FOR CARD FROM MAT");

                if (CardAssets.Suits[suitIndex].Faces[faceIndex] == mat)
                {
                    return new Card { Suit = CardAssets.Suits[suitIndex].Name, Value = faceIndex };
                }
            }
        }
        Debug.LogError("Card material not found!");
        return default;
    }

    // Determine the winner (basic implementation for demonstration)
    private void DetermineWinner()
    {
        Debug.Log("Determining winner based on player and community cards...");

        // Placeholder logic for determining winner - could be expanded with full poker hand ranking logic
        int highestValue = 0;

        List<Card> playerHand = new List<Card>();

        // Combine player cards with community cards
        foreach (GameObject cardObj in playerCards)
        {
            Material mat = cardObj.transform.GetChild(0).GetComponent<Renderer>().material;
            playerHand.Add(GetCardFromMaterial(mat));
        }

        foreach (GameObject cardObj in communityCards)
        {
            Material mat = cardObj.transform.GetChild(0).GetComponent<Renderer>().material;
            playerHand.Add(GetCardFromMaterial(mat));
        }

        // Simplified logic for finding the highest value card
        foreach (Card card in playerHand)
        {
            if (card.Value > highestValue)
            {
                highestValue = card.Value;
            }
        }

        Debug.Log("Player's highest card value: " + highestValue);
        // Similar logic would need to be applied for NPCs to determine the actual winner
    }

    // Coroutine for managing the poker game loop
    private IEnumerator GameLoop()
    {
        // Pre-flop phase
        //DealCards(); MOVED LINE TO BE CALLED IN SetupPlayers();
        //yield return new WaitForSeconds(2f);

        // Flop phase - deal three community cards
        DealCommunityCards(3);
        yield return new WaitForSeconds(2f);

        // Turn phase - deal one community card
        DealCommunityCards(1);
        yield return new WaitForSeconds(2f);

        // River phase - deal one community card
        DealCommunityCards(1);
        yield return new WaitForSeconds(2f);

        // Determine winner
        //DetermineWinner();
    }
}

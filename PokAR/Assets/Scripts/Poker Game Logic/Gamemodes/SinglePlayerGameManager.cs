using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.EventSystems;
using Unity.VisualScripting.FullSerializer.Internal;

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
    private int NPCstartingBalance = 100;

    [SerializeField]
    private int numNPC;
    [SerializeField]
    private Player[] Players;
    [SerializeField]
    private GameObject[] PlayerSlots;

    private bool userActionTaken = false;
    private int currentUserPlayerIndex = -1;

    private List<GameObject> activeCards = new List<GameObject>();
    private GameObject gameTable;
    private List<GameObject> chips = new List<GameObject>();

    private List<GameObject> communityCards = new List<GameObject>();
    private List<Card> communityCardStructs = new List<Card>();

    private Transform[] PotChipSlots;
    private List<GameObject>[] PotChips;


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

    // Betting and turn logic
    private int pot = 0;
    private int currentBet = 0;
    private bool[] foldedPlayers;
    private bool[] activePlayers;
    private int smallBlind = 5;
    private int bigBlind = 10;
    private int dealerIndex = 0;
    private int smallBlindIndex;
    private int bigBlindIndex;
    private int[] currentBets;

    [System.Serializable]
    public struct Card
    {
        public string Suit; // "Hearts", "Diamonds", "Clubs", "Spades"
        public int Value;   // 1 through 13, where 1=Ace, 11=Jack, 12=Queen, 13=King
    }

    [System.Serializable]
    public struct Chip
    {
        public GameObject GO;
        public int Value;
    }

    [System.Serializable]
    public struct Player
    {
        public bool isNPC;
        public int Balance;
        public Card[] Hand;
        public List<GameObject>[] Chips;
        public int[] NumChips;

        public Player(bool isnpc, int balance)
        {
            isNPC = isnpc;
            Balance = balance;
            Hand = new Card[2]; 
            NumChips = new int[4];
            Chips = new List<GameObject>[4];
            for (int i = 0; i < 4; i++)
            {
                Chips[i] = new List<GameObject>();
            }
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
        placementIndicator = Instantiate(placementIndicatorPrefab, gameObject.transform);
        isPlacementIndicatorActive = true;
    }

    void Update()
    {
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
                    if (!IsPointerOverUIObject(mousePosition))
                    {
                        UpdatePlacementPose(mousePosition);
                        inputDetected = true;
                    }
                }
            }
        }
    }

    private void UpdatePlacementPose(Vector2 screenPosition)
    {
        if (arRaycastManager.Raycast(screenPosition, hits, TrackableType.Planes))
        {
            Pose hitPose = hits[0].pose;
            if (!placementIndicator.activeInHierarchy)
            {
                placementIndicator.SetActive(true);
            }

            isPlacementIndicatorActive = true;
            float smoothingSpeed = 10f;
            placementIndicator.transform.position = Vector3.Lerp(placementIndicator.transform.position, hitPose.position, Time.deltaTime * smoothingSpeed);
            placementIndicator.transform.rotation = Quaternion.Lerp(placementIndicator.transform.rotation, hitPose.rotation, Time.deltaTime * smoothingSpeed);
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
        if (isPlacementIndicatorActive)
        {
            confirmedPosition = placementIndicator.transform.position;
            confirmedRotation = placementIndicator.transform.rotation;

            SetupTable(confirmedPosition, confirmedRotation);
            SetupGame();
            placementIndicator.SetActive(false);
            gameSetupStarted = true;
        }
    }

    private void SetupGame()
    {
        SetupDeck();
        SetupPlayers();
        SetupChips();
        StartCoroutine(GameLoop());
    }

    private void SetupPlayers()
    {
        Players = new Player[numNPC+1];
        Players[0] = new Player(false, startingBalance);
        for(int i=1; i<=numNPC; i++)
        {
            Players[i] = new Player(true, NPCstartingBalance);
        }

        PlayerSlots = new GameObject[Players.Length];
        for(int i=0; i<PlayerSlots.Length-1; i++)
        {
            PlayerSlots[i] = gameTable.transform.GetChild(i+1).gameObject;
        }

        DealCards();
        
        List<int> usedAvatarIndices = new List<int>();
        for(int player=0; player < Players.Length; player++)
        {
            int avatarPrefabIndex = UnityEngine.Random.Range(0, AvatarAssets.Avatars.Length);
            while(usedAvatarIndices.Contains(avatarPrefabIndex))
            {
                avatarPrefabIndex = UnityEngine.Random.Range(0, AvatarAssets.Avatars.Length);
            }

            Transform spawnLocation = gameTable.transform.GetChild(player+1).GetChild(3).transform;
            GameObject playerAvatar = Instantiate(AvatarAssets.Avatars[avatarPrefabIndex], spawnLocation.position, spawnLocation.rotation);
            // Make avatar a child of its spawn location
            playerAvatar.transform.SetParent(spawnLocation);
            usedAvatarIndices.Add(avatarPrefabIndex);
        }
    }

    private void SetupTable(Vector3 position, Quaternion rotation)
    {
        gameTable = Instantiate(TableAssets.Tables[0].Prefab, position, rotation, gameObject.transform);

        PotChipSlots = new Transform[4];
        for(int i=0; i<4; i++)
        {
            PotChipSlots[i] = gameTable.transform.GetChild(0).GetChild(5).GetChild(i).transform;
        }
        Debug.Log("Table has been set up.");
    }

    private void SetupChips()
    {
        for(int p=0; p<Players.Length-1; p++)
        {
            int[] numEachChip = new int[3];
            switch(Players[p].Balance)
            {
                case 80:
                    numEachChip[0] = 10; numEachChip[1] = 6; numEachChip[2] = 4;
                    break;
                case 100:
                    numEachChip[0] = 10; numEachChip[1] = 8; numEachChip[2] = 5;
                    break;
                case 120:
                    numEachChip[0] = 10; numEachChip[1] = 10; numEachChip[2] = 6;
                    break;
            }
            
            int stackHeight = 0;
            Transform stackSlot = PlayerSlots[p].transform.GetChild(1).GetChild(0).transform;

            // 1-value chips
            for(int i=0; i<numEachChip[0]; i++)
            {
                GameObject chipGO = Instantiate(ChipAssets.Prefab, stackSlot.position, stackSlot.rotation);
                // Make chip a child of stackSlot
                chipGO.transform.SetParent(stackSlot);
                
                chipGO.transform.GetChild(0).GetComponent<Renderer>().material = ChipAssets.Chips[1].material;
                Players[p].Chips[0].Add(chipGO);
                chipGO.transform.position = stackSlot.position + Vector3.up * (0.00325f * stackHeight);
                chipGO.transform.Rotate(0.0f, 20.0f*i, 0.0f);
                stackHeight = Players[p].Chips[0].Count;
                Players[p].NumChips[0] += 1;
            }

            // 5-value chips
            stackHeight = 0;
            stackSlot = PlayerSlots[p].transform.GetChild(1).GetChild(1).transform;
            for(int i=0; i<numEachChip[1]; i++)
            {
                GameObject chipGO = Instantiate(ChipAssets.Prefab, stackSlot.position, stackSlot.rotation);
                // Make chip a child of stackSlot
                chipGO.transform.SetParent(stackSlot);

                chipGO.transform.GetChild(0).GetComponent<Renderer>().material = ChipAssets.Chips[2].material;
                Players[p].Chips[1].Add(chipGO);
                chipGO.transform.position = stackSlot.position + Vector3.up * (0.00325f * stackHeight);
                chipGO.transform.Rotate(0.0f, 20.0f*i, 0.0f);
                stackHeight = Players[p].Chips[1].Count;
                Players[p].NumChips[1] += 1;
            }

            // 10-value chips
            stackHeight = 0;
            stackSlot = PlayerSlots[p].transform.GetChild(1).GetChild(2).transform;
            for(int i=0; i<numEachChip[2]; i++)
            {
                GameObject chipGO = Instantiate(ChipAssets.Prefab, stackSlot.position, stackSlot.rotation);
                // Make chip a child of stackSlot
                chipGO.transform.SetParent(stackSlot);

                chipGO.transform.GetChild(0).GetComponent<Renderer>().material = ChipAssets.Chips[3].material;
                Players[p].Chips[2].Add(chipGO);
                chipGO.transform.position = stackSlot.position + Vector3.up * (0.00325f * stackHeight);
                chipGO.transform.Rotate(0.0f, 20.0f*i, 0.0f);
                stackHeight = Players[p].Chips[2].Count;
                Players[p].NumChips[2] += 1;
            }
        }
        Debug.Log("Chips have been set up.");
    }

    private void SetupDeck()
    {
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

    private void DealCards()
    {
        // Player (index 0)
        for (int i = 0; i < 2; i++)
        {
            Card card = DrawCard();
            GameObject cardObj = Instantiate(CardAssets.Prefab, gameObject.transform);
            // Make card a child of the card slot
            Transform cardSlot = gameTable.transform.GetChild(1).GetChild(0).GetChild(i);
            cardObj.transform.SetParent(cardSlot);

            cardObj.transform.GetChild(0).GetComponent<Renderer>().material = GetCardMaterial(card);
            cardObj.transform.GetChild(1).GetComponent<Renderer>().material = CardBackMaterial;
            Players[0].Hand[i] = card;
            activeCards.Add(cardObj);

            cardObj.transform.localScale /= 2;
            cardObj.transform.position = cardSlot.position;
            cardObj.transform.rotation = cardSlot.rotation;
        }

        // NPCs
        for (int npc = 1; npc < Players.Length; npc++)
        {
            for (int i = 0; i < 2; i++)
            {
                Card card = DrawCard();
                GameObject cardObj = Instantiate(CardAssets.Prefab, gameObject.transform);
                // Make card a child of the NPC card slot
                Transform cardSlot = gameTable.transform.GetChild(npc+1).GetChild(0).GetChild(i);
                cardObj.transform.SetParent(cardSlot);

                cardObj.transform.GetChild(0).GetComponent<Renderer>().material = GetCardMaterial(card);
                cardObj.transform.GetChild(1).GetComponent<Renderer>().material = CardBackMaterial;
                Players[npc].Hand[i] = card;
                activeCards.Add(cardObj);

                cardObj.transform.localScale /= 2;
                cardObj.transform.position = cardSlot.position;
                cardObj.transform.rotation = cardSlot.rotation;
            }
        }
        Debug.Log("Cards have been dealt.");
    }

    private void DealCommunityCards(int numberOfCards)
    {
        for (int i = 0; i < numberOfCards; i++)
        {
            Card card = DrawCard();
            GameObject cardObj = Instantiate(CardAssets.Prefab, gameObject.transform);
            // Make community card a child of the community card slot
            Transform communityCardSlot = gameTable.transform.GetChild(0).GetChild(communityCardStructs.Count);
            cardObj.transform.SetParent(communityCardSlot);

            cardObj.transform.GetChild(0).GetComponent<Renderer>().material = GetCardMaterial(card);
            cardObj.transform.GetChild(1).GetComponent<Renderer>().material = CardBackMaterial;
            communityCards.Add(cardObj);
            activeCards.Add(cardObj);

            cardObj.transform.localScale /= 2;
            cardObj.transform.position = communityCardSlot.position;
            cardObj.transform.rotation = communityCardSlot.rotation;

            // Store the card struct in communityCardStructs
            communityCardStructs.Add(card);
        }
        Debug.Log("Community cards dealt: " + numberOfCards);
    }

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

    public void FlipCard(GameObject card)
    {
        card.transform.Rotate(180.0f, 0.0f, 0.0f);
    }

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


    // *****************************
    //         Betting Logic
    // *****************************

    private IEnumerator GameLoop()
    {
        int numberOfPlayers = Players.Length;
        foldedPlayers = new bool[numberOfPlayers];
        activePlayers = new bool[numberOfPlayers];
        currentBets = new int[numberOfPlayers];

        for (int i = 0; i < numberOfPlayers; i++)
        {
            activePlayers[i] = true; // all start active
            foldedPlayers[i] = false;
        }

        // Assign blinds
        smallBlindIndex = (dealerIndex + 1) % numberOfPlayers;
        bigBlindIndex = (dealerIndex + 2) % numberOfPlayers;

        // Post blinds
        PlaceBet(smallBlindIndex, smallBlind);
        PlaceBet(bigBlindIndex, bigBlind);

        // Preflop betting
        yield return StartCoroutine(RunBettingRound(GetNextActivePlayer(bigBlindIndex)));

        if (CheckIfOnlyOnePlayerLeft())
        {
            DetermineWinner();
            yield break;
        }

        // Flop
        DealCommunityCards(3);
        ResetBets();
        yield return StartCoroutine(RunBettingRound(GetNextActivePlayer(dealerIndex)));

        if (CheckIfOnlyOnePlayerLeft())
        {
            DetermineWinner();
            yield break;
        }

        // Turn
        DealCommunityCards(1);
        ResetBets();
        yield return StartCoroutine(RunBettingRound(GetNextActivePlayer(dealerIndex)));

        if (CheckIfOnlyOnePlayerLeft())
        {
            DetermineWinner();
            yield break;
        }

        // River
        DealCommunityCards(1);
        ResetBets();
        yield return StartCoroutine(RunBettingRound(GetNextActivePlayer(dealerIndex)));

        if (CheckIfOnlyOnePlayerLeft())
        {
            DetermineWinner();
            yield break;
        }

        // Showdown
        DetermineWinner();
    }

    private void ResetBets()
    {
        currentBet = 0;
        for (int i = 0; i < currentBets.Length; i++)
            currentBets[i] = 0;
    }

    private IEnumerator RunBettingRound(int startIndex)
    {
        bool bettingActive = true;
        while (bettingActive)
        {
            bool actionTaken = false;
            for (int i = 0; i < Players.Length; i++)
            {
                int playerIndex = (startIndex + i) % Players.Length;
                if (!foldedPlayers[playerIndex] && activePlayers[playerIndex] && Players[playerIndex].Balance > 0)
                {
                    yield return StartCoroutine(RunPlayerTurn(playerIndex));
                    actionTaken = true;

                    if (AllBetsEqualOrPlayersFolded())
                    {
                        bettingActive = false;
                        break;
                    }
                }
            }

            if (!actionTaken)
            {
                bettingActive = false;
            }
        }
    }

    private IEnumerator RunPlayerTurn(int playerIndex)
    {
        yield return new WaitForSeconds(0.5f);
        if (Players[playerIndex].isNPC)
        {
            yield return NPCPlayerAction(playerIndex);
        }
        else
        {
            yield return UserPlayerAction(playerIndex);
        }
    }

    private IEnumerator NPCPlayerAction(int playerIndex)
    {
        yield return new WaitForSeconds(0.5f);
        int callAmount = currentBet - currentBets[playerIndex];


        if(playerIndex == 2 && !foldedPlayers[playerIndex])
        {
            foldedPlayers[playerIndex] = true;
                
            //flip folded cards
            FlipCard(PlayerSlots[playerIndex].transform.GetChild(0).GetChild(0).gameObject);
            FlipCard(PlayerSlots[playerIndex].transform.GetChild(0).GetChild(1).gameObject);


            Debug.Log("FOLDING");
            Debug.Log("NPC Player " + playerIndex + " folds.");
            Debug.Log("FOLDING");
        }


        // Very simple NPC logic
        if (callAmount <= 0)
        {
            // Check or minimal bet
            if (Players[playerIndex].Balance > bigBlind && UnityEngine.Random.value > 0.5f)
            {
                PlaceBet(playerIndex, bigBlind);
            }
        }
        else
        {
            if (Players[playerIndex].Balance >= callAmount)
            {
                PlaceBet(playerIndex, callAmount);
            }
            else
            {
                foldedPlayers[playerIndex] = true;
                
                //flip folded cards
                FlipCard(PlayerSlots[playerIndex+1].transform.GetChild(0).GetChild(0).gameObject);
                FlipCard(PlayerSlots[playerIndex+1].transform.GetChild(0).GetChild(1).gameObject);


                Debug.Log("FOLDING");
                Debug.Log("NPC Player " + playerIndex + " folds.");
                Debug.Log("FOLDING");
            }
        }
    }

    // This will be called by external code (e.g., GameManager) once the user chooses an action.
public void HandleCall(int playerIndex)
{
    int callAmount = currentBet - currentBets[playerIndex];
    if (callAmount > 0 && Players[playerIndex].Balance >= callAmount)
    {
        PlaceBet(playerIndex, callAmount);
        Debug.Log("User calls " + callAmount);
    }
    else if (callAmount <= 0)
    {
        // If callAmount is 0 or negative, that means we can just check
        HandleCheck(playerIndex);
        return;
    }
    userActionTaken = true;
}

public void HandleRaise(int playerIndex, int raiseAmount)
{
    int callAmount = currentBet - currentBets[playerIndex];
    int totalBet = callAmount + raiseAmount;

    if (Players[playerIndex].Balance >= totalBet)
    {
        // First call the current amount
        if (callAmount > 0)
            PlaceBet(playerIndex, callAmount);

        // Then raise further
        PlaceBet(playerIndex, raiseAmount);
        Debug.Log("User raises by " + raiseAmount);
    }
    else
    {
        Debug.Log("User tried to raise but doesn't have enough balance!");
        // You could handle this case differently depending on game logic
    }
    userActionTaken = true;
}

public void HandleCheck(int playerIndex)
{
    int callAmount = currentBet - currentBets[playerIndex];
    if (callAmount <= 0)
    {
        Debug.Log("User checks.");
    }
    else
    {
        Debug.Log("User cannot check right now, must call/fold/raise!");
        // This could be handled differently depending on the game logic.
    }
    userActionTaken = true;
}

public void HandleFold(int playerIndex)
{
    foldedPlayers[playerIndex] = true;
    // Flip folded cards (if not already flipped)
    FlipCard(PlayerSlots[playerIndex].transform.GetChild(0).GetChild(0).gameObject);
    FlipCard(PlayerSlots[playerIndex].transform.GetChild(0).GetChild(1).gameObject);

    Debug.Log("User folds.");
    userActionTaken = true;
}

public void HandleView(int playerIndex)
{
    // Display the user's cards in screen space
    // This could involve enabling a UI panel, rendering card images, etc.
    // For demonstration, we'll just log and assume there's a method DisplayCardsOnScreen()

    DisplayCardsOnScreen(Players[playerIndex].Hand);

    // Viewing cards doesn't end the turn, so we do NOT set userActionTaken to true here.
    // The user still needs to choose Call, Check, Fold, or Raise afterward.
}

// Example of a method that displays cards on screen
private void DisplayCardsOnScreen(Card[] cards)
{
    // Implementation detail: Could open a UI canvas, display card sprites, etc.
    // Here we just log for now.
    Debug.Log("Displaying user's cards on screen:");
    foreach (var c in cards)
    {
        Debug.Log(c.Suit + " " + c.Value);
    }
}

// Modified UserPlayerAction method
private IEnumerator UserPlayerAction(int playerIndex)
{
    currentUserPlayerIndex = playerIndex;
    userActionTaken = false;
    Debug.Log("Waiting for user action...");

    // Now just wait until userActionTaken is set to true by one of the Handle* methods.
    while (!userActionTaken)
    {
        // We do nothing here, just wait until some external call triggers an action.
        yield return null;
    }

    Debug.Log("User action completed, proceeding with game.");
}

    private void PlaceBet(int playerIndex, int amount)
{
    if (Players[playerIndex].Balance < amount)
    {
        amount = Players[playerIndex].Balance; // all-in if not enough
    }

    Players[playerIndex].Balance -= amount;
    currentBets[playerIndex] += amount;
    pot += amount;

    if (currentBets[playerIndex] > currentBet)
    {
        currentBet = currentBets[playerIndex];
    }

    Debug.Log("Player " + playerIndex + " bets " + amount + ". Current pot: " + pot);

    // Instantiate pot chips for the bet
    SpawnPotChipsFromBet(playerIndex, amount);
}

private void SpawnPotChipsFromBet(int playerIndex, int amount)
{
    // Define the chip denominations to use (e.g., 10, 5, 1)
    int[] chipValues = { 10, 5, 1 };
    Material[] chipMaterials = { 
        ChipAssets.Chips[3].material, // 10-value chip material
        ChipAssets.Chips[2].material, // 5-value chip material
        ChipAssets.Chips[1].material  // 1-value chip material
    };

    int potSlotIndex = 0; // Choose which pot slot to use. Could be randomized or rotated.
    Transform potSlotTransform = PotChipSlots[potSlotIndex];

    // We can maintain a separate data structure for pot chips if desired.
    // Ensure PotChips is initialized:
    if (PotChips == null || PotChips.Length == 0)
    {
        PotChips = new List<GameObject>[4];
        for (int i=0; i<4; i++)
        {
            PotChips[i] = new List<GameObject>();
        }
    }

    // Break down the amount into chip denominations
    for (int denomIndex = 0; denomIndex < chipValues.Length; denomIndex++)
    {
        int chipValue = chipValues[denomIndex];
        Material chipMat = chipMaterials[denomIndex];
        
        int numChips = amount / chipValue;
        amount = amount % chipValue;

        for (int i = 0; i < numChips; i++)
        {
            // Instantiate a single chip at the pot slot
            GameObject chipGO = Instantiate(ChipAssets.Prefab, potSlotTransform.position, potSlotTransform.rotation);
            chipGO.transform.SetParent(potSlotTransform);

            // Set chip material
            chipGO.transform.GetChild(0).GetComponent<Renderer>().material = chipMat;

            // Positioning logic:
            // We stack them with a slight vertical offset and a small rotation
            // similar to how we did in SetupChips()
            int stackHeight = PotChips[potSlotIndex].Count;
            chipGO.transform.position = potSlotTransform.position + Vector3.up * (0.00325f * stackHeight);
            chipGO.transform.Rotate(0.0f, 20.0f * stackHeight, 0.0f);

            PotChips[potSlotIndex].Add(chipGO);
        }
    }

    // If amount > 0 after this, it means it didn't break down completely 
    // (shouldn't happen if denominations cover all amounts)
}


    private bool AllBetsEqualOrPlayersFolded()
    {
        int matchedBet = -1;
        for (int i = 0; i < Players.Length; i++)
        {
            if (!foldedPlayers[i] && activePlayers[i])
            {
                if (matchedBet == -1) matchedBet = currentBets[i];
                if (currentBets[i] != matchedBet) return false;
            }
        }
        return true;
    }

    private bool CheckIfOnlyOnePlayerLeft()
    {
        int count = 0;
        for (int i = 0; i < Players.Length; i++)
        {
            if (!foldedPlayers[i] && activePlayers[i]) count++;
        }
        return count == 1;
    }

    private int GetNextActivePlayer(int startIndex)
    {
        int idx = (startIndex + 1) % Players.Length;
        while (foldedPlayers[idx] || !activePlayers[idx])
        {
            idx = (idx + 1) % Players.Length;
        }
        return idx;
    }

    private void DetermineWinner()
    {
        Debug.Log("Determining winner...");
        HandEvaluator evaluator = new HandEvaluator();

        int winnerIndex = -1;
        int bestRank = -1;

        for (int p = 0; p < Players.Length; p++)
        {
            if (!foldedPlayers[p])
            {
                List<Card> allCards = new List<Card>(Players[p].Hand);
                allCards.AddRange(communityCardStructs);

                int rank = evaluator.EvaluateHand(allCards);
                Debug.Log("Player " + p + " hand rank: " + rank);
                if (rank > bestRank)
                {
                    bestRank = rank;
                    winnerIndex = p;
                }
            }
        }

        Debug.Log("Player " + winnerIndex + " wins the pot of " + pot);
        Players[winnerIndex].Balance += pot;
        pot = 0;
    }
}

using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.EventSystems;

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
    private int playerIndex = 0; // Start at 0 to ensure it's within range once players are set up

    private List<GameObject> activeCards = new List<GameObject>();
    private GameObject gameTable;
    private List<GameObject> chips = new List<GameObject>();

    private List<GameObject> communityCards = new List<GameObject>();
    private List<Card> communityCardStructs = new List<Card>();

    private Transform[] PotChipSlots;
    private List<GameObject>[] PotChips;

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
        for(int i=0; i<PlayerSlots.Length; i++)
        {
            PlayerSlots[i] = gameTable.transform.GetChild(i+1).gameObject;
        }

        // Set initial playerIndex now that Players and PlayerSlots are set
        playerIndex = 0;

        DealCards();
        
        List<int> usedAvatarIndices = new List<int>();
        for(int player=0; player < Players.Length; player++)
        {
            int avatarPrefabIndex = UnityEngine.Random.Range(0, AvatarAssets.Avatars.Length);
            while(usedAvatarIndices.Contains(avatarPrefabIndex))
            {
                avatarPrefabIndex = UnityEngine.Random.Range(0, AvatarAssets.Avatars.Length);
            }

            Transform spawnLocation = PlayerSlots[player].transform.GetChild(3).transform;
            GameObject playerAvatar = Instantiate(AvatarAssets.Avatars[avatarPrefabIndex], spawnLocation.position, spawnLocation.rotation);
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
            Transform cardSlot = PlayerSlots[0].transform.GetChild(0).GetChild(i);
            cardObj.transform.SetParent(cardSlot);

            cardObj.transform.GetChild(0).GetComponent<Renderer>().material = GetCardMaterial(card);
            cardObj.transform.GetChild(1).GetComponent<Renderer>().material = CardBackMaterial;
            Players[0].Hand[i] = card;
            activeCards.Add(cardObj);

            cardObj.transform.localScale /= 2;
            cardObj.transform.position = cardSlot.position;
            cardObj.transform.rotation = cardSlot.rotation;
            FlipCard(cardObj);
        }

        // NPCs
        for (int npc = 1; npc < Players.Length; npc++)
        {
            for (int i = 0; i < 2; i++)
            {
                Card card = DrawCard();
                GameObject cardObj = Instantiate(CardAssets.Prefab, gameObject.transform);
                Transform cardSlot = PlayerSlots[npc].transform.GetChild(0).GetChild(i);
                cardObj.transform.SetParent(cardSlot);

                cardObj.transform.GetChild(0).GetComponent<Renderer>().material = GetCardMaterial(card);
                cardObj.transform.GetChild(1).GetComponent<Renderer>().material = CardBackMaterial;
                Players[npc].Hand[i] = card;
                activeCards.Add(cardObj);

                cardObj.transform.localScale /= 2;
                cardObj.transform.position = cardSlot.position;
                cardObj.transform.rotation = cardSlot.rotation;
                FlipCard(cardObj);

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
            Transform communityCardSlot = gameTable.transform.GetChild(0).GetChild(communityCardStructs.Count);
            cardObj.transform.SetParent(communityCardSlot);

            cardObj.transform.GetChild(0).GetComponent<Renderer>().material = GetCardMaterial(card);
            cardObj.transform.GetChild(1).GetComponent<Renderer>().material = CardBackMaterial;
            communityCards.Add(cardObj);
            activeCards.Add(cardObj);

            cardObj.transform.localScale /= 2;
            cardObj.transform.position = communityCardSlot.position;
            cardObj.transform.rotation = communityCardSlot.rotation;

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
            activePlayers[i] = true; 
            foldedPlayers[i] = false;
        }

        // Assign blinds
        smallBlindIndex = (dealerIndex + 1) % numberOfPlayers;
        bigBlindIndex = (dealerIndex + 2) % numberOfPlayers;

        // Post blinds
        PlaceBlindBet(smallBlindIndex, smallBlind);
        PlaceBlindBet(bigBlindIndex, bigBlind);

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
                int currentPlayerIndex = (startIndex + i) % Players.Length;
                if (!foldedPlayers[currentPlayerIndex] && activePlayers[currentPlayerIndex] && Players[currentPlayerIndex].Balance > 0)
                {
                    yield return StartCoroutine(RunPlayerTurn(currentPlayerIndex));
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

    private IEnumerator RunPlayerTurn(int currentPlayerIndex)
    {
        this.playerIndex = currentPlayerIndex; // set the global playerIndex to current player's turn
        yield return new WaitForSeconds(0.5f);
        if (Players[currentPlayerIndex].isNPC)
        {
            yield return NPCPlayerAction(currentPlayerIndex);
        }
        else
        {
            yield return UserPlayerAction();
        }
    }

    private IEnumerator NPCPlayerAction(int currentNPCIndex)
    {
        yield return new WaitForSeconds(0.5f);
        int callAmount = currentBet - currentBets[currentNPCIndex];

        // Example simple logic: NPC folds if currentNPCIndex == 2 and not folded already
        if(currentNPCIndex == 2 && !foldedPlayers[currentNPCIndex])
        {
            foldedPlayers[currentNPCIndex] = true;
            FlipCard(PlayerSlots[currentNPCIndex].transform.GetChild(0).GetChild(0).gameObject);
            FlipCard(PlayerSlots[currentNPCIndex].transform.GetChild(0).GetChild(1).gameObject);
            Debug.Log("NPC Player " + currentNPCIndex + " folds.");
            yield break;
        }

        if (callAmount <= 0)
        {
            // Check or minimal bet
            if (Players[currentNPCIndex].Balance > bigBlind && UnityEngine.Random.value > 0.5f)
            {
                PlaceBet(currentNPCIndex, bigBlind);
            }
        }
        else
        {
            if (Players[currentNPCIndex].Balance >= callAmount)
            {
                PlaceBet(currentNPCIndex, callAmount);
            }
            else
            {
                foldedPlayers[currentNPCIndex] = true;
                FlipCard(PlayerSlots[currentNPCIndex].transform.GetChild(0).GetChild(0).gameObject);
                FlipCard(PlayerSlots[currentNPCIndex].transform.GetChild(0).GetChild(1).gameObject);

                Debug.Log("NPC Player " + currentNPCIndex + " folds.");
            }
        }
    }

    public void HandleCall()
    {
        int callAmount = currentBet - currentBets[playerIndex];
        if (callAmount > 0 && Players[playerIndex].Balance >= callAmount)
        {
            PlaceBet(playerIndex, callAmount);
            Debug.Log("User calls " + callAmount);
        }
        else if (callAmount <= 0)
        {
            HandleCheck();
            return;
        }
        userActionTaken = true;
    }

    public void HandleRaise(float raiseFactor)
    {
        int callAmount = currentBet - currentBets[playerIndex];
        int totalBet = callAmount + (int)(Players[playerIndex].Balance * raiseFactor);

        if (Players[playerIndex].Balance >= totalBet)
        {
            if (callAmount > 0)
                PlaceBet(playerIndex, callAmount);

            PlaceBet(playerIndex, totalBet);
            Debug.Log("User raises by " + totalBet);
        }
        else
        {
            Debug.Log("User tried to raise but doesn't have enough balance!");
        }
        userActionTaken = true;
    }

    public void HandleCheck()
    {
        int callAmount = currentBet - currentBets[playerIndex];
        if (callAmount <= 0)
        {
            Debug.Log("User checks.");
        }
        else
        {
            Debug.Log("User cannot check right now, must call/fold/raise!");
        }
        userActionTaken = true;
    }

    public void HandleFold()
    {
        foldedPlayers[playerIndex] = true;
        FlipCard(PlayerSlots[playerIndex].transform.GetChild(0).GetChild(0).gameObject);
        FlipCard(PlayerSlots[playerIndex].transform.GetChild(0).GetChild(1).gameObject);

        Debug.Log("User folds.");
        userActionTaken = true;
    }

    public void HandleView()
    {
        DisplayCardsOnScreen(Players[playerIndex].Hand);
    }

    private void DisplayCardsOnScreen(Card[] cards)
    {
        Debug.Log("Displaying user's cards on screen:");
        foreach (var c in cards)
        {
            Debug.Log(c.Suit + " " + c.Value);
        }
    }

    private IEnumerator UserPlayerAction()
    {
        userActionTaken = false;
        Debug.Log("Waiting for user action...");

        while (!userActionTaken)
        {
            yield return null;
        }

        Debug.Log("User action completed, proceeding with game.");
    }

    private void PlaceBet(int bettingPlayerIndex, int amount)
    {
        if (Players[bettingPlayerIndex].Balance < amount)
        {
            amount = Players[bettingPlayerIndex].Balance; // all-in if not enough
        }

        Players[bettingPlayerIndex].Balance -= amount;
        currentBets[bettingPlayerIndex] += amount;
        pot += amount;

        if (currentBets[bettingPlayerIndex] > currentBet)
        {
            currentBet = currentBets[bettingPlayerIndex];
        }

        Debug.Log("Player " + bettingPlayerIndex + " bets " + amount + ". Current pot: " + pot);

        SpawnPotChipsFromBet(amount);
    }

    private void PlaceBlindBet(int blindPlayerIndex, int amount)
    {
        if (Players[blindPlayerIndex].Balance < amount)
        {
            amount = Players[blindPlayerIndex].Balance; // all-in if not enough
        }

        Players[blindPlayerIndex].Balance -= amount;
        currentBets[blindPlayerIndex] += amount;
        pot += amount;

        if (currentBets[blindPlayerIndex] > currentBet)
        {
            currentBet = currentBets[blindPlayerIndex];
        }

        Debug.Log("Player " + blindPlayerIndex + " posts a blind of " + amount + ". Current pot: " + pot);

        SpawnPotChipsFromBet(amount);
    }

    private void SpawnPotChipsFromBet(int amount)
    {
        int[] chipValues = { 10, 5, 1 };
        Material[] chipMaterials = { 
            ChipAssets.Chips[3].material, // 10-value chip
            ChipAssets.Chips[2].material, // 5-value chip
            ChipAssets.Chips[1].material  // 1-value chip
        };

        int potSlotIndex = 0; 
        Transform potSlotTransform = PotChipSlots[potSlotIndex];

        if (PotChips == null || PotChips.Length == 0)
        {
            PotChips = new List<GameObject>[4];
            for (int i=0; i<4; i++)
            {
                PotChips[i] = new List<GameObject>();
            }
        }

        for (int denomIndex = 0; denomIndex < chipValues.Length; denomIndex++)
        {
            int chipValue = chipValues[denomIndex];
            Material chipMat = chipMaterials[denomIndex];
            
            int numChips = amount / chipValue;
            amount = amount % chipValue;

            for (int i = 0; i < numChips; i++)
            {
                GameObject chipGO = Instantiate(ChipAssets.Prefab, potSlotTransform.position, potSlotTransform.rotation);
                chipGO.transform.SetParent(potSlotTransform);

                chipGO.transform.GetChild(0).GetComponent<Renderer>().material = chipMat;

                int stackHeight = PotChips[potSlotIndex].Count;
                chipGO.transform.position = potSlotTransform.position + Vector3.up * (0.00325f * stackHeight);
                chipGO.transform.Rotate(0.0f, 20.0f * stackHeight, 0.0f);

                PotChips[potSlotIndex].Add(chipGO);
            }
        }
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

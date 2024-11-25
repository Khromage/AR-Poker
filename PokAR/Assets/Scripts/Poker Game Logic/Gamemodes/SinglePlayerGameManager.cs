using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class SinglePlayerGameManager : MonoBehaviour
{
    [SerializeField]
    protected CasinoTables TableAssets;
    [SerializeField]
    protected CasinoChips ChipAssets;
    [SerializeField]
    protected PlayingCards CardAssets;

    [SerializeField]
    private string difficulty;
    [SerializeField]
    private int numNPC;

    private List<GameObject> activeCards = new List<GameObject>();
    private GameObject table;
    private List<GameObject> chips = new List<GameObject>();

    private List<GameObject> communityCards = new List<GameObject>();
    private List<GameObject> playerCards = new List<GameObject>();
    private List<GameObject>[] npcCards;

    private List<Card> deck = new List<Card>();
    private Material CardBackMaterial;

    private ARRaycastManager arRaycastManager;
    private bool gameSetupStarted = false;

    private static List<ARRaycastHit> hits = new List<ARRaycastHit>();
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

    public void Initialize(string gameDifficulty, int npcCount)
    {
        difficulty = gameDifficulty;
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
                    UpdatePlacementPose(touchPosition);
                    inputDetected = true;
                }
            }

            if (!inputDetected && Mouse.current != null)
            {
                var mouse = Mouse.current;

                if (mouse.leftButton.isPressed)
                {
                    Vector2 mousePosition = mouse.position.ReadValue();
                    UpdatePlacementPose(mousePosition);
                    inputDetected = true;
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

    public void ConfirmPlacement()
    {
        if (isPlacementIndicatorActive)
        {
            // Store the confirmed position and rotation
            confirmedPosition = placementIndicator.transform.position;
            confirmedRotation = placementIndicator.transform.rotation;

            // Place the table at the confirmed location and start the game
            SetupTable(confirmedPosition, confirmedRotation);
            SetupGame();

            // Disable further changes to the placement indicator
            placementIndicator.SetActive(false);
            gameSetupStarted = true;
        }
    }

    private void SetupGame()
    {
        // Note: Removed redundant call to SetupTable since it's already called in ConfirmPlacement()
        SetupChips();
        SetupDeck();
        StartCoroutine(GameLoop());
    }

    private void SetupTable(Vector3 position, Quaternion rotation)
    {
        table = Instantiate(TableAssets.Tables[0].Prefab, position, rotation, gameObject.transform);
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
            playerCards.Add(cardObj);
            activeCards.Add(cardObj);
        }

        // Deal two cards to each NPC
        for (int npc = 0; npc < numNPC; npc++)
        {
            for (int i = 0; i < 2; i++)
            {
                Card card = DrawCard();
                GameObject cardObj = Instantiate(CardAssets.Prefab, gameObject.transform);
                cardObj.transform.GetChild(0).GetComponent<Renderer>().material = GetCardMaterial(card);
                cardObj.transform.GetChild(1).GetComponent<Renderer>().material = CardBackMaterial;
                npcCards[npc].Add(cardObj);
                activeCards.Add(cardObj);
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
            if (card.Value >= 1 && card.Value <= CardAssets.Suits[suitIndex].Faces.Length)
            {
                return CardAssets.Suits[suitIndex].Faces[card.Value - 1];
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
            for (int faceIndex = 0; faceIndex < CardAssets.Suits[suitIndex].Faces.Length; faceIndex++)
            {
                if (CardAssets.Suits[suitIndex].Faces[faceIndex] == mat)
                {
                    return new Card { Suit = CardAssets.Suits[suitIndex].Name, Value = faceIndex + 1 };
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
        DealCards();
        yield return new WaitForSeconds(2f);

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
        DetermineWinner();
    }
}

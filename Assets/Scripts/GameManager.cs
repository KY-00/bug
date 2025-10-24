using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum GameState
{
    PlayerTurn,
    EnemyTurn,
    GameOver
}

public enum CardType
{
    Heal,
    Damage
}

public class GameManager : MonoBehaviour
{
    [Header("��Ϸ״̬")]
    public GameState currentState = GameState.PlayerTurn;

    [Header("�������")]
    public float playerHealth = 30;
    public float playerMaxHealth = 30;
    public float playerMana = 3;
    public float playerMaxMana = 10;

    [Header("��������")]
    public float enemyHealth = 30;
    public float enemyMaxHealth = 30;

    [Header("UI����")]
    public Text playerHealthText;
    public Text enemyHealthText;
    public Text manaText;
    public Text turnText;
    public Button nextTurnButton;
    public Transform playerHandArea;
    public Transform enemyHandArea;
    public Slider EnemySlider;
    public Slider PlayerSlider;

    public GameObject ShuZi;

    public GameObject Enemymodle;

    [Header("����Ԥ����")]
    public GameObject cardPrefab;

    [Header("��������")]
    public CardData[] availableCards;

    private List<GameObject> playerHand = new List<GameObject>();
    private List<GameObject> enemyHand = new List<GameObject>();
    private int turnNumber = 1;

    public static GameManager Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        InitializeGame();
        nextTurnButton.onClick.AddListener(OnNextTurnButtonClick);
    }

    void InitializeGame()
    {
        playerHealth = playerMaxHealth;
        enemyHealth = enemyMaxHealth;
        turnNumber = 1;
        currentState = GameState.PlayerTurn;

        UpdateUI();
        StartNewTurn();
    }

    public void StartNewTurn()
    {
        turnNumber++;

        if (currentState == GameState.PlayerTurn)
        {
            // ��һغϿ�ʼ
            playerMana = Mathf.Min(playerMaxMana, playerMana + 1);
            DrawCardsForPlayer(1);
            turnText.text = "��һغ� " + turnNumber;
            nextTurnButton.interactable = true;
        }
        else
        {
            // ���˻غ�
            turnText.text = "���˻غ� " + turnNumber;
            nextTurnButton.interactable = false;
            StartCoroutine(EnemyTurn());
        }

        UpdateUI();
    }

    void DrawCardsForPlayer(int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (playerHand.Count < 10) // ��������
            {
                CardData randomCard = availableCards[Random.Range(0, availableCards.Length)];
                GameObject newCard = Instantiate(cardPrefab, playerHandArea);
                CardDisplay cardDisplay = newCard.GetComponent<CardDisplay>();
                cardDisplay.Initialize(randomCard);
                playerHand.Add(newCard);
            }
        }
    }

    IEnumerator EnemyTurn()
    {
        yield return new WaitForSeconds(1f);

        // ���˼�AI�����ʹ�ÿ��ƻ򹥻�
        if (Random.Range(0, 2) == 0 && enemyHand.Count > 0)
        {
            // ʹ�ÿ���
            GameObject cardToUse = enemyHand[Random.Range(0, enemyHand.Count)];
            CardDisplay cardDisplay = cardToUse.GetComponent<CardDisplay>();

            // ���ѡ��Ŀ��
            bool targetPlayer = Random.Range(0, 2) == 0;
            if (cardDisplay.cardData.cardType == CardType.Heal)
            {
                // ���������Լ�
                enemyHealth = Mathf.Min(enemyMaxHealth, enemyHealth + cardDisplay.cardData.value);
            }
            else
            {
                // ���˹������
                playerHealth -= cardDisplay.cardData.value;
                Enemymodle.GetComponent<Animator>().SetTrigger("attack");
            }

            enemyHand.Remove(cardToUse);
            Destroy(cardToUse);
        }
        else
        {
            // ֱ�ӹ���
            playerHealth -= 2;
            Enemymodle.GetComponent<Animator>().SetTrigger("attack");
        }

        UpdateUI();
        yield return new WaitForSeconds(1f);

        // �л�����һغ�
        currentState = GameState.PlayerTurn;
        StartNewTurn();

        CheckGameOver();
    }

    void OnNextTurnButtonClick()
    {
        if (currentState == GameState.PlayerTurn)
        {
            currentState = GameState.EnemyTurn;
            StartNewTurn();
        }
    }

    public void UpdateUI()
    {
        playerHealthText.text = "���: " + playerHealth + "/" + playerMaxHealth;
        PlayerSlider.value = playerHealth/ playerMaxHealth;
        enemyHealthText.text = enemyHealth.ToString();
        Debug.Log(enemyHealth+enemyMaxHealth);
        EnemySlider.value = enemyHealth/ enemyMaxHealth;
        manaText.text = "����: " + playerMana + "/" + playerMaxMana;
    }

    public bool CanPlayCard(int manaCost)
    {
        return playerMana >= manaCost && currentState == GameState.PlayerTurn;
    }

    public void SpendMana(float amount)
    {
        playerMana -= amount;
        UpdateUI();
    }

    // ��GameManager����ӵ�����Ϣ
    public void PlayCard(GameObject card, bool targetIsPlayer)
    {
        Debug.Log($"ʹ�ÿ���: {card.GetComponent<CardDisplay>().cardData.cardName}, Ŀ��: {(targetIsPlayer ? "���" : "����")}");

        CardDisplay cardDisplay = card.GetComponent<CardDisplay>();

        if (cardDisplay.cardData.cardType == CardType.Heal)
        {
            if (targetIsPlayer)
            {
                playerHealth = Mathf.Min(playerMaxHealth, playerHealth + cardDisplay.cardData.value);
                Debug.Log($"��һָ� {cardDisplay.cardData.value} ������ֵ");
            }
            else
            {
                enemyHealth = Mathf.Min(enemyMaxHealth, enemyHealth + cardDisplay.cardData.value);
                Debug.Log($"���˻ָ� {cardDisplay.cardData.value} ������ֵ");
            }

            // ���ƿ�����ʾ + ��
            ShuZi.SetActive(true);
            ShuZi.GetComponent<Text>().text = "+" + cardDisplay.cardData.value.ToString();
            StartCoroutine(HideNumberAfterDelay(0.5f));
        }
        else // Damage
        {
            if (targetIsPlayer)
            {
                playerHealth -= cardDisplay.cardData.value;
                Debug.Log($"����ܵ� {cardDisplay.cardData.value} ���˺�");
            }
            else
            {
                enemyHealth -= cardDisplay.cardData.value;
                Debug.Log($"�����ܵ� {cardDisplay.cardData.value} ���˺�");
                Enemymodle.GetComponent<Animator>().SetTrigger("hit");
            }

            // �˺�������ʾ - ��
            ShuZi.SetActive(true);
            ShuZi.GetComponent<Text>().text = "-" + cardDisplay.cardData.value.ToString();
            StartCoroutine(HideNumberAfterDelay(0.5f));
        }

        playerHand.Remove(card);
        Destroy(card);
        UpdateUI();
        CheckGameOver();
    }
    private System.Collections.IEnumerator HideNumberAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        ShuZi.SetActive(false);
    }

    void CheckGameOver()
    {
        if (playerHealth <= 0)
        {
            currentState = GameState.GameOver;
            turnText.text = "��Ϸ����! ����ʤ��!";
            nextTurnButton.interactable = false;
        }
        else if (enemyHealth <= 0)
        {
            currentState = GameState.GameOver;
            turnText.text = "��Ϸ����! ���ʤ��!";
            nextTurnButton.interactable = false;
        }
    }

}
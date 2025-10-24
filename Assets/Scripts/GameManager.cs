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
    [Header("游戏状态")]
    public GameState currentState = GameState.PlayerTurn;

    [Header("玩家设置")]
    public float playerHealth = 30;
    public float playerMaxHealth = 30;
    public float playerMana = 3;
    public float playerMaxMana = 10;

    [Header("敌人设置")]
    public float enemyHealth = 30;
    public float enemyMaxHealth = 30;

    [Header("UI引用")]
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

    [Header("卡牌预制体")]
    public GameObject cardPrefab;

    [Header("卡牌数据")]
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
            // 玩家回合开始
            playerMana = Mathf.Min(playerMaxMana, playerMana + 1);
            DrawCardsForPlayer(1);
            turnText.text = "玩家回合 " + turnNumber;
            nextTurnButton.interactable = true;
        }
        else
        {
            // 敌人回合
            turnText.text = "敌人回合 " + turnNumber;
            nextTurnButton.interactable = false;
            StartCoroutine(EnemyTurn());
        }

        UpdateUI();
    }

    void DrawCardsForPlayer(int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (playerHand.Count < 10) // 手牌上限
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

        // 敌人简单AI：随机使用卡牌或攻击
        if (Random.Range(0, 2) == 0 && enemyHand.Count > 0)
        {
            // 使用卡牌
            GameObject cardToUse = enemyHand[Random.Range(0, enemyHand.Count)];
            CardDisplay cardDisplay = cardToUse.GetComponent<CardDisplay>();

            // 随机选择目标
            bool targetPlayer = Random.Range(0, 2) == 0;
            if (cardDisplay.cardData.cardType == CardType.Heal)
            {
                // 敌人治疗自己
                enemyHealth = Mathf.Min(enemyMaxHealth, enemyHealth + cardDisplay.cardData.value);
            }
            else
            {
                // 敌人攻击玩家
                playerHealth -= cardDisplay.cardData.value;
                Enemymodle.GetComponent<Animator>().SetTrigger("attack");
            }

            enemyHand.Remove(cardToUse);
            Destroy(cardToUse);
        }
        else
        {
            // 直接攻击
            playerHealth -= 2;
            Enemymodle.GetComponent<Animator>().SetTrigger("attack");
        }

        UpdateUI();
        yield return new WaitForSeconds(1f);

        // 切换到玩家回合
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
        playerHealthText.text = "玩家: " + playerHealth + "/" + playerMaxHealth;
        PlayerSlider.value = playerHealth/ playerMaxHealth;
        enemyHealthText.text = enemyHealth.ToString();
        Debug.Log(enemyHealth+enemyMaxHealth);
        EnemySlider.value = enemyHealth/ enemyMaxHealth;
        manaText.text = "法力: " + playerMana + "/" + playerMaxMana;
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

    // 在GameManager中添加调试信息
    public void PlayCard(GameObject card, bool targetIsPlayer)
    {
        Debug.Log($"使用卡牌: {card.GetComponent<CardDisplay>().cardData.cardName}, 目标: {(targetIsPlayer ? "玩家" : "敌人")}");

        CardDisplay cardDisplay = card.GetComponent<CardDisplay>();

        if (cardDisplay.cardData.cardType == CardType.Heal)
        {
            if (targetIsPlayer)
            {
                playerHealth = Mathf.Min(playerMaxHealth, playerHealth + cardDisplay.cardData.value);
                Debug.Log($"玩家恢复 {cardDisplay.cardData.value} 点生命值");
            }
            else
            {
                enemyHealth = Mathf.Min(enemyMaxHealth, enemyHealth + cardDisplay.cardData.value);
                Debug.Log($"敌人恢复 {cardDisplay.cardData.value} 点生命值");
            }

            // 治疗卡牌显示 + 号
            ShuZi.SetActive(true);
            ShuZi.GetComponent<Text>().text = "+" + cardDisplay.cardData.value.ToString();
            StartCoroutine(HideNumberAfterDelay(0.5f));
        }
        else // Damage
        {
            if (targetIsPlayer)
            {
                playerHealth -= cardDisplay.cardData.value;
                Debug.Log($"玩家受到 {cardDisplay.cardData.value} 点伤害");
            }
            else
            {
                enemyHealth -= cardDisplay.cardData.value;
                Debug.Log($"敌人受到 {cardDisplay.cardData.value} 点伤害");
                Enemymodle.GetComponent<Animator>().SetTrigger("hit");
            }

            // 伤害卡牌显示 - 号
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
            turnText.text = "游戏结束! 敌人胜利!";
            nextTurnButton.interactable = false;
        }
        else if (enemyHealth <= 0)
        {
            currentState = GameState.GameOver;
            turnText.text = "游戏结束! 玩家胜利!";
            nextTurnButton.interactable = false;
        }
    }

}
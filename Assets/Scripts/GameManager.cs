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
    public Slider EnemySliderNegative;
    public Slider PlayerSlider;

    public GameObject ShuZi;

    public GameObject Enemymodle;

    public Transform NumAppear;

    [Header("卡牌预制体")]
    public GameObject cardPrefab;

    [Header("数字预制体")]
    public GameObject numberPrefab;

    [Header("卡牌数据")]
    public CardData[] availableCards;

    public Animator nowNum;
    public GameObject nextlevel;

    private List<GameObject> playerHand = new List<GameObject>();
    private List<GameObject> enemyHand = new List<GameObject>();
    private int turnNumber = 1;
    private int currentCardIndex = 0; // 当前抽卡索引

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
        currentCardIndex = 0; // 重置抽卡索引

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
            if (playerHand.Count < 10 && currentCardIndex < availableCards.Length) // 手牌上限且还有卡牌可抽
            {
                // 按顺序获取卡牌数据
                CardData nextCard = availableCards[currentCardIndex];
                GameObject newCard = Instantiate(cardPrefab, playerHandArea);
                CardDisplay cardDisplay = newCard.GetComponent<CardDisplay>();
                cardDisplay.Initialize(nextCard);
                playerHand.Add(newCard);

                // 更新索引，循环到卡牌数据库开头
                currentCardIndex++;
                if (currentCardIndex >= availableCards.Length)
                {
                    currentCardIndex = 0; // 循环到第一张卡牌
                }

                Debug.Log($"抽到卡牌: {nextCard.cardName} (索引: {currentCardIndex - 1})");
            }
            else if (currentCardIndex >= availableCards.Length)
            {
                Debug.LogWarning("没有更多卡牌可抽");
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
            playerHealth -= 5;
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
        PlayerSlider.value = playerHealth / playerMaxHealth;
        enemyHealthText.text = enemyHealth.ToString();
        Debug.Log(enemyHealth + enemyMaxHealth);
        EnemySlider.value = enemyHealth / enemyMaxHealth;
        EnemySliderNegative.value = -enemyHealth / enemyMaxHealth;
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
            SplitFloatToDigits(cardDisplay.cardData.value, CardType.Heal); // 明确传递CardType.Heal
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
                nowNum.SetTrigger("attack");
            }

            // 伤害卡牌显示 - 号
            ShuZi.SetActive(true);
            ShuZi.GetComponent<Text>().text = "-" + cardDisplay.cardData.value.ToString();
            SplitFloatToDigits(cardDisplay.cardData.value, CardType.Damage); // 明确传递CardType.Damage
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
        if (enemyHealth == 0)
        {
            currentState = GameState.GameOver;
            turnText.text = "游戏结束! 玩家胜利!";
            nextTurnButton.interactable = false;
            Enemymodle.GetComponent<Animator>().SetTrigger("lose");
            nextlevel.SetActive(true);
        }
    }

    public string[] SplitFloatToDigits(float value, CardType cardType)
    {
        List<string> digits = new List<string>();

        // 根据卡牌类型添加符号
        if (cardType == CardType.Heal)
        {
            digits.Add("+"); // 治疗卡牌显示+号
        }
        else if (cardType == CardType.Damage)
        {
            digits.Add("-"); // 伤害卡牌显示-号
        }

        // 将数值部分转换为字符串（取绝对值）
        string numberString = Mathf.Abs(value).ToString();

        foreach (char c in numberString)
        {
            // 处理小数点
            if (c == '.')
            {
                digits.Add(".");
            }
            // 处理数字
            else if (char.IsDigit(c))
            {
                digits.Add(c.ToString());
            }
        }

        // 在NumAppear位置生成数字预制体
        GenerateNumberPrefabs(digits);

        return digits.ToArray();
    }
    private void GenerateNumberPrefabs(List<string> digits)
    {
        if (NumAppear == null)
        {
            Debug.LogError("NumAppear未分配！");
            return;
        }

        if (numberPrefab == null)
        {
            Debug.LogError("数字预制体未分配！");
            return;
        }

        // 先清空现有的数字
        foreach (Transform child in NumAppear)
        {
            Destroy(child.gameObject);
        }

        // 生成新的数字预制体
        for (int i = 0; i < digits.Count; i++)
        {
            // 实例化预制体
            GameObject numberObj = Instantiate(numberPrefab, NumAppear);
            numberObj.name = $"Number_{i}_{digits[i]}";

            // 获取Text组件并设置文本
            Text textComponent = numberObj.GetComponent<Text>();
            if (textComponent != null)
            {
                textComponent.text = digits[i];

                /*                // 根据字符类型设置颜色
                                if (digits[i] == "-")
                                {
                                    textComponent.color = Color.red;
                                }
                                else if (digits[i] == "+")
                                {
                                    textComponent.color = Color.green;
                                }
                                else if (digits[i] == ".")
                                {
                                    textComponent.color = Color.gray;
                                }
                                else
                                {
                                    textComponent.color = Color.white;
                                }*/
            }

            // 设置位置（水平排列）
            RectTransform rectTransform = numberObj.GetComponent<RectTransform>();
            rectTransform.anchoredPosition = new Vector2(i * 35, 0);

            Debug.Log($"生成数字预制体: {digits[i]} 在位置 {i}");
        }
    }
}
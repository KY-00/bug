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
    public Slider EnemySliderNegative;
    public Slider PlayerSlider;

    public GameObject ShuZi;

    public GameObject Enemymodle;

    public Transform NumAppear;

    [Header("����Ԥ����")]
    public GameObject cardPrefab;

    [Header("����Ԥ����")]
    public GameObject numberPrefab;

    [Header("��������")]
    public CardData[] availableCards;

    public Animator nowNum;
    public GameObject nextlevel;

    private List<GameObject> playerHand = new List<GameObject>();
    private List<GameObject> enemyHand = new List<GameObject>();
    private int turnNumber = 1;
    private int currentCardIndex = 0; // ��ǰ�鿨����

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
        currentCardIndex = 0; // ���ó鿨����

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
            if (playerHand.Count < 10 && currentCardIndex < availableCards.Length) // ���������һ��п��ƿɳ�
            {
                // ��˳���ȡ��������
                CardData nextCard = availableCards[currentCardIndex];
                GameObject newCard = Instantiate(cardPrefab, playerHandArea);
                CardDisplay cardDisplay = newCard.GetComponent<CardDisplay>();
                cardDisplay.Initialize(nextCard);
                playerHand.Add(newCard);

                // ����������ѭ�����������ݿ⿪ͷ
                currentCardIndex++;
                if (currentCardIndex >= availableCards.Length)
                {
                    currentCardIndex = 0; // ѭ������һ�ſ���
                }

                Debug.Log($"�鵽����: {nextCard.cardName} (����: {currentCardIndex - 1})");
            }
            else if (currentCardIndex >= availableCards.Length)
            {
                Debug.LogWarning("û�и��࿨�ƿɳ�");
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
            playerHealth -= 5;
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
        PlayerSlider.value = playerHealth / playerMaxHealth;
        enemyHealthText.text = enemyHealth.ToString();
        Debug.Log(enemyHealth + enemyMaxHealth);
        EnemySlider.value = enemyHealth / enemyMaxHealth;
        EnemySliderNegative.value = -enemyHealth / enemyMaxHealth;
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
            SplitFloatToDigits(cardDisplay.cardData.value, CardType.Heal); // ��ȷ����CardType.Heal
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
                nowNum.SetTrigger("attack");
            }

            // �˺�������ʾ - ��
            ShuZi.SetActive(true);
            ShuZi.GetComponent<Text>().text = "-" + cardDisplay.cardData.value.ToString();
            SplitFloatToDigits(cardDisplay.cardData.value, CardType.Damage); // ��ȷ����CardType.Damage
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
        if (enemyHealth == 0)
        {
            currentState = GameState.GameOver;
            turnText.text = "��Ϸ����! ���ʤ��!";
            nextTurnButton.interactable = false;
            Enemymodle.GetComponent<Animator>().SetTrigger("lose");
            nextlevel.SetActive(true);
        }
    }

    public string[] SplitFloatToDigits(float value, CardType cardType)
    {
        List<string> digits = new List<string>();

        // ���ݿ���������ӷ���
        if (cardType == CardType.Heal)
        {
            digits.Add("+"); // ���ƿ�����ʾ+��
        }
        else if (cardType == CardType.Damage)
        {
            digits.Add("-"); // �˺�������ʾ-��
        }

        // ����ֵ����ת��Ϊ�ַ�����ȡ����ֵ��
        string numberString = Mathf.Abs(value).ToString();

        foreach (char c in numberString)
        {
            // ����С����
            if (c == '.')
            {
                digits.Add(".");
            }
            // ��������
            else if (char.IsDigit(c))
            {
                digits.Add(c.ToString());
            }
        }

        // ��NumAppearλ����������Ԥ����
        GenerateNumberPrefabs(digits);

        return digits.ToArray();
    }
    private void GenerateNumberPrefabs(List<string> digits)
    {
        if (NumAppear == null)
        {
            Debug.LogError("NumAppearδ���䣡");
            return;
        }

        if (numberPrefab == null)
        {
            Debug.LogError("����Ԥ����δ���䣡");
            return;
        }

        // ��������е�����
        foreach (Transform child in NumAppear)
        {
            Destroy(child.gameObject);
        }

        // �����µ�����Ԥ����
        for (int i = 0; i < digits.Count; i++)
        {
            // ʵ����Ԥ����
            GameObject numberObj = Instantiate(numberPrefab, NumAppear);
            numberObj.name = $"Number_{i}_{digits[i]}";

            // ��ȡText����������ı�
            Text textComponent = numberObj.GetComponent<Text>();
            if (textComponent != null)
            {
                textComponent.text = digits[i];

                /*                // �����ַ�����������ɫ
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

            // ����λ�ã�ˮƽ���У�
            RectTransform rectTransform = numberObj.GetComponent<RectTransform>();
            rectTransform.anchoredPosition = new Vector2(i * 35, 0);

            Debug.Log($"��������Ԥ����: {digits[i]} ��λ�� {i}");
        }
    }
}
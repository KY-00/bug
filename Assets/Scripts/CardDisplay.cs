using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
[RequireComponent(typeof(Image))]
public class CardDisplay : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public CardData cardData;

    [Header("UI组件")]
    public Text nameText;
    public Text descriptionText;
    public Text manaCostText;
    public Text valueText;
    public Image artworkImage;

    private LineRenderer lineRenderer;
    private bool isDragging = false;
    private Vector3 startPosition;
    private Transform originalParent;
    private CanvasGroup canvasGroup;

    void Start()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
        canvasGroup.blocksRaycasts = true; // 确保初始可接收射线
        // 确保有CanvasGroup组件
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        // 初始化线条渲染器
        InitializeLineRenderer();

        // 检查UI组件引用
        CheckUIComponents();
    }

    void InitializeLineRenderer()
    {
        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
        }

        lineRenderer.positionCount = 2;
        lineRenderer.startWidth = 0.1f;
        lineRenderer.endWidth = 0.1f;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = Color.yellow;
        lineRenderer.endColor = Color.red;
        lineRenderer.enabled = false;
    }

    void CheckUIComponents()
    {
        if (nameText == null) Debug.LogError("Name Text未分配!");
        if (descriptionText == null) Debug.LogError("Description Text未分配!");
        if (manaCostText == null) Debug.LogError("Mana Cost Text未分配!");
        if (valueText == null) Debug.LogError("Value Text未分配!");
        if (artworkImage == null) Debug.LogError("Artwork Image未分配!");
    }

    public void Initialize(CardData data)
    {
        cardData = data;

        if (nameText != null) nameText.text = data.cardName;
        if (descriptionText != null) descriptionText.text = data.description;
        if (manaCostText != null) manaCostText.text = data.manaCost.ToString();
        if (valueText != null) valueText.text = data.value.ToString();
        if (artworkImage != null) artworkImage.sprite = data.artwork;

        // 根据卡牌类型设置颜色
        if (valueText != null)
        {
            valueText.color = data.cardType == CardType.Heal ? Color.green : Color.red;
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!GameManager.Instance.CanPlayCard(cardData.manaCost))
        {
            Debug.Log("法力值不足或不是玩家回合");
            return;
        }

        isDragging = true;
        startPosition = transform.position;
        originalParent = transform.parent;

        // 设置CanvasGroup属性以便拖动
        if (canvasGroup != null)
        {
           // canvasGroup.alpha = 0.6f;
            canvasGroup.blocksRaycasts = false;
        }

        // 提升卡牌层级
        transform.SetParent(transform.root);
        transform.SetAsLastSibling();

        if (lineRenderer != null)
        {
            lineRenderer.enabled = true;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging) return;
        // 使用RectTransformUtility来处理UI拖动
        RectTransform rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null && RectTransformUtility.ScreenPointToWorldPointInRectangle(
            rectTransform, eventData.position, eventData.pressEventCamera, out Vector3 worldPoint))
        {
            transform.position = worldPoint;
        }
        else
        {
            // 备用方案
            Vector3 currentPosition = Camera.main.ScreenToWorldPoint(eventData.position);
            currentPosition.z = 0;
            transform.position = currentPosition;
        }

        // 更新施法线
        if (lineRenderer != null)
        {
            lineRenderer.SetPosition(0, startPosition);
            lineRenderer.SetPosition(1, transform.position);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging) return;

        isDragging = false;

        // 恢复CanvasGroup属性
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
        }

        if (lineRenderer != null)
        {
            lineRenderer.enabled = false;
        }

        // 检测释放目标
        CheckDropTarget(eventData);
    }

    void CheckDropTarget(PointerEventData eventData)
    {
        // 使用EventSystem检测UI目标
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        bool validTargetFound = false;

        foreach (RaycastResult result in results)
        {
            if (result.gameObject.CompareTag("Player") || result.gameObject.CompareTag("Enemy"))
            {
                bool targetIsPlayer = result.gameObject.CompareTag("Player");

                // 检查目标是否合理
                //if (IsValidTarget(targetIsPlayer))
                //{
                    validTargetFound = true;
                   // GameManager.Instance.SpendMana(cardData.manaCost);
                    GameManager.Instance.PlayCard(gameObject, targetIsPlayer);
                    return;
              //  }
            }
        }

        // 如果没有有效目标，回到手牌
        if (!validTargetFound)
        {
            ReturnToHand();
        }
    }

    bool IsValidTarget(bool targetIsPlayer)
    {
        // 治疗卡只能对玩家或友方使用，伤害卡只能对敌人使用
        if (cardData.cardType == CardType.Heal && !targetIsPlayer)
        {
            Debug.Log("治疗卡不能对敌人使用");
            return false;
        }
        if (cardData.cardType == CardType.Damage && targetIsPlayer)
        {
            Debug.Log("伤害卡不能对自己使用");
            return false;
        }
        return true;
    }

    void ReturnToHand()
    {
        Debug.Log("卡牌回到手牌");
        transform.position = startPosition;
        transform.SetParent(originalParent);
    }
    public string[] SplitFloatToDigits(float value)
    {
        // 将float转换为字符串，保留小数部分
        string numberString = value.ToString();
        List<string> digits = new List<string>();

        foreach (char c in numberString)
        {
            // 如果是负号、小数点或数字，都作为单独元素
            if (c == '-' || c == '.' || char.IsDigit(c))
            {
                digits.Add(c.ToString());
            }
        }

        return digits.ToArray();
    }
}
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class NumberDragController : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private CanvasGroup canvasGroup;
    private Vector3 startPosition;
    private Transform startParent;
    private GameObject placeholder;
    private bool isDragging = false;
    private CardDisplay targetCard; // 记录目标卡牌
    private int targetInsertIndex = -1; // 记录插入位置
    private Rigidbody2D rb; // 添加Rigidbody2D引用
    private float originalGravityScale; // 记录原始重力值

    [Header("占位符设置")]
    public GameObject placeholderPrefab; // 可以在Inspector中分配

    void Start()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        // 获取Rigidbody2D组件
        rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            originalGravityScale = rb.gravityScale; // 保存原始重力值
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        isDragging = true;
        startPosition = transform.position;
        startParent = transform.parent;
        targetCard = null;
        targetInsertIndex = -1;

        // 设置重力为0
        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.velocity = Vector2.zero; // 同时重置速度
        }

        // 创建占位符
        CreatePlaceholder();

        // 设置拖动状态
        canvasGroup.alpha = 0.6f;
        canvasGroup.blocksRaycasts = false;

        // 提升到顶层
        transform.SetParent(transform.root);
        transform.SetAsLastSibling();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging) return;

        // 更新位置
        transform.position = eventData.position;

        // 确保重力为0（防止意外变化）
        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.velocity = Vector2.zero;
        }

        // 更新占位符位置预览
        UpdatePlaceholderPreview(eventData.position);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging) return;

        isDragging = false;

        // 恢复重力
        if (rb != null)
        {
            rb.gravityScale = originalGravityScale; // 恢复原始重力值
        }

        // 恢复状态
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        // 处理插入逻辑
        bool wasInserted = false;

        // 方式1：直接拖到卡牌上（精确插入）
        if (targetCard != null && targetInsertIndex >= 0)
        {
            string content = GetComponent<Text>().text; // 获取文本内容（可能是数字或符号）
            targetCard.InsertNumberFromDrag(content, targetInsertIndex);
            Destroy(gameObject); // 销毁原数字或符号
            wasInserted = true;
        }
        else
        {
            // 方式2：检查是否有卡牌记录了当前数字的碰撞
            wasInserted = CheckTriggeredCards();
        }

        // 如果没有被插入到任何卡牌，停留在当前位置
        if (!wasInserted)
        {
            // 停留在释放的位置，不回到原位
            // 可以设置父对象为Canvas或保持独立
            if (transform.parent == transform.root)
            {
                // 如果还在根层级，可以设置为Canvas或保持独立
                Canvas canvas = FindObjectOfType<Canvas>();
                if (canvas != null)
                {
                    transform.SetParent(canvas.transform);
                }
                // 否则就保持独立，停留在当前位置
            }

            Debug.Log($"数字停留在位置: {transform.position}");
        }

        // 销毁占位符
        Destroy(placeholder);
    }

    // 检查是否有卡牌记录了当前数字的碰撞
    private bool CheckTriggeredCards()
    {
        CardDisplay[] allCards = FindObjectsOfType<CardDisplay>();
        foreach (CardDisplay card in allCards)
        {
            // 如果这张卡牌记录了当前数字的碰撞
            if (card.triggeredNumbers.Contains(GetComponent<Collider2D>()))
            {
                // 让卡牌处理所有触发的数字
                card.ProcessTriggeredNumbersAfterDrop();
                return true; // 表示已经被处理
            }
        }
        return false; // 表示没有被任何卡牌处理
    }

    private void CreatePlaceholder()
    {
        if (placeholderPrefab != null)
        {
            placeholder = Instantiate(placeholderPrefab);
        }
        else
        {
            placeholder = new GameObject("Placeholder");

            Image image = placeholder.AddComponent<Image>();
            image.color = new Color(0.8f, 0.8f, 1f, 0.4f);

            Outline outline = placeholder.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 1f, 0.5f, 0.1f);
            outline.effectDistance = new Vector2(2, 2);
        }

        placeholder.transform.SetParent(startParent);
        placeholder.transform.SetSiblingIndex(transform.GetSiblingIndex());

        RectTransform placeholderRect = placeholder.GetComponent<RectTransform>();
        RectTransform originalRect = GetComponent<RectTransform>();
        placeholderRect.sizeDelta = originalRect.sizeDelta;
    }

    private void UpdatePlaceholderPreview(Vector3 dragPosition)
    {
        if (placeholder == null) return;

        CardDisplay[] allCards = FindObjectsOfType<CardDisplay>();
        CardDisplay closestCard = null;
        float closestDistance = float.MaxValue;
        int insertIndex = -1;

        foreach (CardDisplay card in allCards)
        {
            Transform numArea = card.transform.Find("NumArea");
            if (numArea != null)
            {
                RectTransform cardRect = card.GetComponent<RectTransform>();
                Vector2 localPoint;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    cardRect, dragPosition, null, out localPoint))
                {
                    if (cardRect.rect.Contains(localPoint))
                    {
                        int calculatedIndex = card.FindInsertIndexForDrag(dragPosition);
                        if (calculatedIndex >= 0)
                        {
                            float distance = Vector3.Distance(dragPosition, numArea.position);
                            if (distance < closestDistance)
                            {
                                closestDistance = distance;
                                closestCard = card;
                                insertIndex = calculatedIndex;
                            }
                        }
                    }
                }
            }
        }

        targetCard = closestCard;
        targetInsertIndex = insertIndex;

        if (closestCard != null && insertIndex >= 0)
        {
            Transform targetNumArea = closestCard.transform.Find("NumArea");
            placeholder.transform.SetParent(targetNumArea);
            placeholder.transform.SetSiblingIndex(insertIndex);
        }
        else
        {
            placeholder.transform.SetParent(startParent);
            placeholder.transform.SetSiblingIndex(transform.GetSiblingIndex());
        }
    }

    // 可选：添加一些物理效果让拖动更自然
    void Update()
    {
        // 如果数字没有被插入且不在拖动状态，可以添加一些轻微的物理效果
        if (!isDragging && rb != null && transform.parent != startParent)
        {
            // 可以添加轻微的空气阻力
            rb.drag = 0.5f;
            rb.angularDrag = 0.5f;
        }
    }
}
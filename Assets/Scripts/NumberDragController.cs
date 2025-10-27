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
    private CardDisplay targetCard; // ��¼Ŀ�꿨��
    private int targetInsertIndex = -1; // ��¼����λ��
    private Rigidbody2D rb; // ���Rigidbody2D����
    private float originalGravityScale; // ��¼ԭʼ����ֵ

    [Header("ռλ������")]
    public GameObject placeholderPrefab; // ������Inspector�з���

    void Start()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        // ��ȡRigidbody2D���
        rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            originalGravityScale = rb.gravityScale; // ����ԭʼ����ֵ
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        isDragging = true;
        startPosition = transform.position;
        startParent = transform.parent;
        targetCard = null;
        targetInsertIndex = -1;

        // ��������Ϊ0
        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.velocity = Vector2.zero; // ͬʱ�����ٶ�
        }

        // ����ռλ��
        CreatePlaceholder();

        // �����϶�״̬
        canvasGroup.alpha = 0.6f;
        canvasGroup.blocksRaycasts = false;

        // ����������
        transform.SetParent(transform.root);
        transform.SetAsLastSibling();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging) return;

        // ����λ��
        transform.position = eventData.position;

        // ȷ������Ϊ0����ֹ����仯��
        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.velocity = Vector2.zero;
        }

        // ����ռλ��λ��Ԥ��
        UpdatePlaceholderPreview(eventData.position);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging) return;

        isDragging = false;

        // �ָ�����
        if (rb != null)
        {
            rb.gravityScale = originalGravityScale; // �ָ�ԭʼ����ֵ
        }

        // �ָ�״̬
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        // ��������߼�
        bool wasInserted = false;

        // ��ʽ1��ֱ���ϵ������ϣ���ȷ���룩
        if (targetCard != null && targetInsertIndex >= 0)
        {
            string content = GetComponent<Text>().text; // ��ȡ�ı����ݣ����������ֻ���ţ�
            targetCard.InsertNumberFromDrag(content, targetInsertIndex);
            Destroy(gameObject); // ����ԭ���ֻ����
            wasInserted = true;
        }
        else
        {
            // ��ʽ2������Ƿ��п��Ƽ�¼�˵�ǰ���ֵ���ײ
            wasInserted = CheckTriggeredCards();
        }

        // ���û�б����뵽�κο��ƣ�ͣ���ڵ�ǰλ��
        if (!wasInserted)
        {
            // ͣ�����ͷŵ�λ�ã����ص�ԭλ
            // �������ø�����ΪCanvas�򱣳ֶ���
            if (transform.parent == transform.root)
            {
                // ������ڸ��㼶����������ΪCanvas�򱣳ֶ���
                Canvas canvas = FindObjectOfType<Canvas>();
                if (canvas != null)
                {
                    transform.SetParent(canvas.transform);
                }
                // ����ͱ��ֶ�����ͣ���ڵ�ǰλ��
            }

            Debug.Log($"����ͣ����λ��: {transform.position}");
        }

        // ����ռλ��
        Destroy(placeholder);
    }

    // ����Ƿ��п��Ƽ�¼�˵�ǰ���ֵ���ײ
    private bool CheckTriggeredCards()
    {
        CardDisplay[] allCards = FindObjectsOfType<CardDisplay>();
        foreach (CardDisplay card in allCards)
        {
            // ������ſ��Ƽ�¼�˵�ǰ���ֵ���ײ
            if (card.triggeredNumbers.Contains(GetComponent<Collider2D>()))
            {
                // �ÿ��ƴ������д���������
                card.ProcessTriggeredNumbersAfterDrop();
                return true; // ��ʾ�Ѿ�������
            }
        }
        return false; // ��ʾû�б��κο��ƴ���
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

    // ��ѡ�����һЩ����Ч�����϶�����Ȼ
    void Update()
    {
        // �������û�б������Ҳ����϶�״̬���������һЩ��΢������Ч��
        if (!isDragging && rb != null && transform.parent != startParent)
        {
            // ���������΢�Ŀ�������
            rb.drag = 0.5f;
            rb.angularDrag = 0.5f;
        }
    }
}
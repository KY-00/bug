using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
[RequireComponent(typeof(Image))]
public class CardDisplay : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public CardData cardData;

    [Header("UI���")]
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
        canvasGroup.blocksRaycasts = true; // ȷ����ʼ�ɽ�������
        // ȷ����CanvasGroup���
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        // ��ʼ��������Ⱦ��
        InitializeLineRenderer();

        // ���UI�������
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
        if (nameText == null) Debug.LogError("Name Textδ����!");
        if (descriptionText == null) Debug.LogError("Description Textδ����!");
        if (manaCostText == null) Debug.LogError("Mana Cost Textδ����!");
        if (valueText == null) Debug.LogError("Value Textδ����!");
        if (artworkImage == null) Debug.LogError("Artwork Imageδ����!");
    }

    public void Initialize(CardData data)
    {
        cardData = data;

        if (nameText != null) nameText.text = data.cardName;
        if (descriptionText != null) descriptionText.text = data.description;
        if (manaCostText != null) manaCostText.text = data.manaCost.ToString();
        if (valueText != null) valueText.text = data.value.ToString();
        if (artworkImage != null) artworkImage.sprite = data.artwork;

        // ���ݿ�������������ɫ
        if (valueText != null)
        {
            valueText.color = data.cardType == CardType.Heal ? Color.green : Color.red;
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!GameManager.Instance.CanPlayCard(cardData.manaCost))
        {
            Debug.Log("����ֵ���������һغ�");
            return;
        }

        isDragging = true;
        startPosition = transform.position;
        originalParent = transform.parent;

        // ����CanvasGroup�����Ա��϶�
        if (canvasGroup != null)
        {
           // canvasGroup.alpha = 0.6f;
            canvasGroup.blocksRaycasts = false;
        }

        // �������Ʋ㼶
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
        // ʹ��RectTransformUtility������UI�϶�
        RectTransform rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null && RectTransformUtility.ScreenPointToWorldPointInRectangle(
            rectTransform, eventData.position, eventData.pressEventCamera, out Vector3 worldPoint))
        {
            transform.position = worldPoint;
        }
        else
        {
            // ���÷���
            Vector3 currentPosition = Camera.main.ScreenToWorldPoint(eventData.position);
            currentPosition.z = 0;
            transform.position = currentPosition;
        }

        // ����ʩ����
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

        // �ָ�CanvasGroup����
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
        }

        if (lineRenderer != null)
        {
            lineRenderer.enabled = false;
        }

        // ����ͷ�Ŀ��
        CheckDropTarget(eventData);
    }

    void CheckDropTarget(PointerEventData eventData)
    {
        // ʹ��EventSystem���UIĿ��
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        bool validTargetFound = false;

        foreach (RaycastResult result in results)
        {
            if (result.gameObject.CompareTag("Player") || result.gameObject.CompareTag("Enemy"))
            {
                bool targetIsPlayer = result.gameObject.CompareTag("Player");

                // ���Ŀ���Ƿ����
                //if (IsValidTarget(targetIsPlayer))
                //{
                    validTargetFound = true;
                   // GameManager.Instance.SpendMana(cardData.manaCost);
                    GameManager.Instance.PlayCard(gameObject, targetIsPlayer);
                    return;
              //  }
            }
        }

        // ���û����ЧĿ�꣬�ص�����
        if (!validTargetFound)
        {
            ReturnToHand();
        }
    }

    bool IsValidTarget(bool targetIsPlayer)
    {
        // ���ƿ�ֻ�ܶ���һ��ѷ�ʹ�ã��˺���ֻ�ܶԵ���ʹ��
        if (cardData.cardType == CardType.Heal && !targetIsPlayer)
        {
            Debug.Log("���ƿ����ܶԵ���ʹ��");
            return false;
        }
        if (cardData.cardType == CardType.Damage && targetIsPlayer)
        {
            Debug.Log("�˺������ܶ��Լ�ʹ��");
            return false;
        }
        return true;
    }

    void ReturnToHand()
    {
        Debug.Log("���ƻص�����");
        transform.position = startPosition;
        transform.SetParent(originalParent);
    }
    public string[] SplitFloatToDigits(float value)
    {
        // ��floatת��Ϊ�ַ���������С������
        string numberString = value.ToString();
        List<string> digits = new List<string>();

        foreach (char c in numberString)
        {
            // ����Ǹ��š�С��������֣�����Ϊ����Ԫ��
            if (c == '-' || c == '.' || char.IsDigit(c))
            {
                digits.Add(c.ToString());
            }
        }

        return digits.ToArray();
    }
}
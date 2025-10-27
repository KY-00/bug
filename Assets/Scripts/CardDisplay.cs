using System.Collections.Generic;
using System.Linq;
using System.Text;
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

    [Header("����Ԥ����")]
    public GameObject textNumPrefab; // ��Inspector�з���

    private LineRenderer lineRenderer;
    private bool isDragging = false;
    private Vector3 startPosition;
    private Transform originalParent;
    private CanvasGroup canvasGroup;

    [Header("��ײ���")]
    public List<Collider2D> triggeredNumbers = new List<Collider2D>(); // ��¼����������

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
        data.value = CalculateNumberFromNumArea();
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
    public int CalculateNumberFromNumArea()
    {
        // ��ȡ���ֽ�NumArea��������
        Transform numArea = transform.Find("NumArea");

        // ����Ҳ���NumArea������0
        if (numArea == null)
        {
            Debug.LogWarning("δ�ҵ���ΪNumArea��������");
            return 0;
        }

        // ��ȡNumArea������������
        List<Transform> childDigits = new List<Transform>();
        for (int i = 0; i < numArea.childCount; i++)
        {
            childDigits.Add(numArea.GetChild(i));
        }

        // ������������Hierarchy�е�˳�����򣨴�ǰ����
        childDigits.Sort((a, b) => a.GetSiblingIndex().CompareTo(b.GetSiblingIndex()));

        // ��ȡ�����������Text���ֵ
        List<string> contents = new List<string>();
        foreach (Transform child in childDigits)
        {
            Text textComponent = child.GetComponent<Text>();
            if (textComponent != null && !string.IsNullOrEmpty(textComponent.text))
            {
                contents.Add(textComponent.text);
            }
        }

        // ���û���κ����ݣ�����0
        if (contents.Count == 0)
        {
            Debug.LogWarning("NumArea��û����Ч������");
            return 0;
        }

        // ������ʽ���
        int result = CalculateExpression(contents);
        Debug.Log($"��NumArea�����������: {result} (���ʽ: [{string.Join(" ", contents)}])");
        return result;
    }

    // ������ʽ���
    private int CalculateExpression(List<string> contents)
    {
        // ����Ƿ���������
        bool hasOperator = contents.Any(content => content == "+" || content == "-");

        if (!hasOperator)
        {
            // û���������ֱ��ƴ����������
            string combined = string.Join("", contents);
            if (int.TryParse(combined, out int result))
            {
                return result;
            }
            return 0;
        }
        else
        {
            // �����������Ҫ�������ʽ
            // �Ƚ����������ֺϲ�
            List<string> parsedContents = new List<string>();
            StringBuilder currentNumber = new StringBuilder();

            foreach (string content in contents)
            {
                if (content == "+" || content == "-")
                {
                    // ���֮ǰ���ۻ������֣������
                    if (currentNumber.Length > 0)
                    {
                        parsedContents.Add(currentNumber.ToString());
                        currentNumber.Clear();
                    }
                    // ��������
                    parsedContents.Add(content);
                }
                else
                {
                    // �ۻ�����
                    currentNumber.Append(content);
                }
            }

            // ������һ������
            if (currentNumber.Length > 0)
            {
                parsedContents.Add(currentNumber.ToString());
            }

            // ���м���
            int result = 0;
            string currentOperator = "+";

            foreach (string content in parsedContents)
            {
                if (content == "+" || content == "-")
                {
                    currentOperator = content;
                }
                else if (int.TryParse(content, out int number))
                {
                    if (currentOperator == "+")
                    {
                        result += number;
                    }
                    else if (currentOperator == "-")
                    {
                        result -= number;
                    }
                }
            }

            return result;
        }
    }
    public void OnNumTriggerEnter(Collider2D other)
    {
        // �����ײ�����tag�Ƿ�ΪNum
        if (other.CompareTag("Num"))
        {
            // ��ȡ��ײ�����Text���ֵ
            Text otherText = other.GetComponent<Text>();
            if (otherText != null && !string.IsNullOrEmpty(otherText.text))
            {
                string content = otherText.text;
                // ��NumArea�������µ����ֻ����
                InsertNumberIntoNumArea(content, other.transform.position);
            }
        }
    }
    // ��NumArea�в�������
    // ��NumArea�в�������
    // ��NumArea�в�������
    private void InsertNumberIntoNumArea(string content, Vector3 collisionPosition)
    {
        Transform numArea = transform.Find("NumArea");
        if (numArea == null)
        {
            Debug.LogWarning("δ�ҵ�NumArea");
            return;
        }

        if (textNumPrefab == null)
        {
            textNumPrefab = Resources.Load<GameObject>("Prefabs/TextNum");
            if (textNumPrefab == null)
            {
                Debug.LogError("�޷�����Ԥ����: Assets/Prefabs/TextNum.prefab");
                return;
            }
        }

        // ��ȡNumArea������������
        List<Transform> numChildren = GetAllNumAreaChildren(numArea);

        // �ҵ�����λ��
        int insertIndex = FindInsertIndex(numChildren, collisionPosition);

        // �����µ����ֻ����Ԥ����
        GameObject newObj = Instantiate(textNumPrefab, numArea);
        Text newText = newObj.GetComponent<Text>();
        if (newText != null)
        {
            newText.text = content;

            // ��������������ɫ
            if (content == "-")
            {
                newText.color = Color.red;
            }
            else if (content == "+")
            {
                newText.color = Color.green;
            }
            else
            {
                newText.color = Color.white;
            }
        }

        // ����϶����
        NumberDragController dragController = newObj.AddComponent<NumberDragController>();

        // ����ռλ��Ԥ���壨����еĻ���
        if (GetComponent<NumberDragController>() != null && GetComponent<NumberDragController>().placeholderPrefab != null)
        {
            dragController.placeholderPrefab = GetComponent<NumberDragController>().placeholderPrefab;
        }

        // ����λ��
        newObj.transform.SetSiblingIndex(insertIndex);

        Debug.Log($"�������� '{content}' ��λ�� {insertIndex}");

        // ˢ��NumArea���� - ȷ��ˮƽ����
        RefreshNumAreaLayout();

        // ���¿�����ֵ���������㣩
        UpdateCardValueAfterInsertion();
        GameManager.Instance.UpdateUI();
    }

    // ���϶���������
    public void InsertNumberFromDrag(string content, int insertIndex)
    {
        Transform numArea = transform.Find("NumArea");
        if (numArea == null) return;

        if (textNumPrefab == null)
        {
            textNumPrefab = Resources.Load<GameObject>("Prefabs/TextNum");
            if (textNumPrefab == null) return;
        }

        // �����µ����ֻ����
        GameObject newObj = Instantiate(textNumPrefab, numArea);
        Text newText = newObj.GetComponent<Text>();
        if (newText != null)
        {
            newText.text = content;

            // ��������������ɫ
            if (content == "-")
            {
                newText.color = Color.red;
            }
            else if (content == "+")
            {
                newText.color = Color.green;
            }
            else
            {
                newText.color = Color.white;
            }
        }

        // ����λ��
        newObj.transform.SetSiblingIndex(insertIndex);

        // ����϶��������������Ҳ���Ա��϶�
        newObj.AddComponent<NumberDragController>();

        // ˢ��NumArea���� - ȷ��ˮƽ����
        RefreshNumAreaLayout();

        // ���¿�����ֵ���������㣩
        UpdateCardValueAfterInsertion();
        GameManager.Instance.UpdateUI();

        Debug.Log($"���ֺ�������� '{content}' ��λ�� {insertIndex}");
    }
    // ���϶���������
    
    // ɾ����ע�͵�ԭ���� OnNumTriggerEnter ����
    /*
    public void OnNumTriggerEnter(Collider2D other)
    {
        // ������������Զ�����
    }
    */

    // �������ֺ���¿�����ֵ
    private void UpdateCardValueAfterInsertion()
    {
        // ���¼���NumArea�е�����
        int newValue = CalculateNumberFromNumArea();

        // ���¿�������
        cardData.value = newValue;

        // ����UI��ʾ
        if (valueText != null)
        {
            valueText.text = newValue.ToString();
        }

        Debug.Log($"������ֵ����Ϊ: {newValue}");
    }

    // ��ȡNumArea�����������岢��˳������
    private List<Transform> GetAllNumAreaChildren(Transform numArea)
    {
        List<Transform> children = new List<Transform>();
        for (int i = 0; i < numArea.childCount; i++)
        {
            children.Add(numArea.GetChild(i));
        }
        children.Sort((a, b) => a.GetSiblingIndex().CompareTo(b.GetSiblingIndex()));
        return children;
    }

    // ������ײλ���ҵ�����λ��
    private int FindInsertIndex(List<Transform> numChildren, Vector3 collisionPosition)
    {
        if (numChildren.Count == 0) return 0;

        Vector2 screenCollisionPos = collisionPosition;
      //  Debug.Log($"��Ļ��ײλ��: {screenCollisionPos}");

        // ��ȡ�������ֵ���Ļλ��
        List<float> numScreenPositions = new List<float>();
        foreach (Transform child in numChildren)
        {
            // ����Screen Spaceģʽ��ֱ��ʹ��position��x����
            numScreenPositions.Add(child.position.x);
        }

        // ������λ��
        for (int i = 0; i < numScreenPositions.Count - 1; i++)
        {
            float currentX = numScreenPositions[i];
            float nextX = numScreenPositions[i + 1];
            float midPoint = (currentX + nextX) / 2f;

            Debug.Log($"�Ƚ�: ��ײ{screenCollisionPos.x} �� {currentX} �� {nextX} ֮��, �е�{midPoint}");

            if (screenCollisionPos.x >= currentX && screenCollisionPos.x < nextX)
            {
                // �ж��ǲ����ڵ�ǰλ�û�����һ��λ��
                if (screenCollisionPos.x < midPoint)
                {
                    Debug.Log($"���뵽λ�� {i + 1} (��������{i})");
                    return i + 1;
                }
                else
                {
                    Debug.Log($"���뵽λ�� {i + 1} (��������{i + 1})");
                    return i + 1;
                }
            }
        }

        // �߽����
        if (screenCollisionPos.x < numScreenPositions[0])
        {
            Debug.Log($"���뵽λ�� 0 (��һ��֮ǰ)");
            return 0;
        }
        else if (screenCollisionPos.x >= numScreenPositions[numScreenPositions.Count - 1])
        {
            Debug.Log($"���뵽ĩβ {numChildren.Count}");
            return numChildren.Count;
        }

        Debug.Log($"Ĭ�ϲ��뵽ĩβ {numChildren.Count}");
        return numChildren.Count;
    }
    // ����GridLayout����ʼX���꣨���ǲ�ͬ�Ķ��뷽ʽ��
    private float CalculateStartX(int childCount, GridLayoutGroup gridLayout)
    {
        RectTransform rectTransform = gridLayout.GetComponent<RectTransform>();
        float totalWidth = (gridLayout.cellSize.x + gridLayout.spacing.x) * childCount - gridLayout.spacing.x;

        switch (gridLayout.childAlignment)
        {
            case TextAnchor.UpperLeft:
            case TextAnchor.MiddleLeft:
            case TextAnchor.LowerLeft:
                return -rectTransform.rect.width / 2f + gridLayout.padding.left;

            case TextAnchor.UpperCenter:
            case TextAnchor.MiddleCenter:
            case TextAnchor.LowerCenter:
                return -totalWidth / 2f;

            case TextAnchor.UpperRight:
            case TextAnchor.MiddleRight:
            case TextAnchor.LowerRight:
                return rectTransform.rect.width / 2f - totalWidth - gridLayout.padding.right;

            default:
                return -totalWidth / 2f;
        }
    }

    // ��ײ��⺯������Ҫ��ӵ�����ײ����������

    // �����Ҫ3D��ײ���
    public int FindInsertIndexForDrag(Vector3 dragPosition)
    {
        Transform numArea = transform.Find("NumArea");
        if (numArea == null) return -1;

        // ��ȡ����������
        List<Transform> numChildren = GetAllNumAreaChildren(numArea);
        if (numChildren.Count == 0) return 0;

        // ���϶�λ��ת��ΪNumArea�ľֲ�����
        Vector2 localPos;
        RectTransform numAreaRect = numArea.GetComponent<RectTransform>();
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            numAreaRect, dragPosition, null, out localPos))
        {
            // ���ھֲ�����������λ��
            for (int i = 0; i < numChildren.Count; i++)
            {
                float childX = numChildren[i].localPosition.x;
                if (localPos.x < childX)
                {
                    return i;
                }
            }
            return numChildren.Count;
        }

        return -1;
    }

    // ���϶���������
  
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Num"))
        {
            Text otherText = other.GetComponent<Text>();
            if (otherText != null && !string.IsNullOrEmpty(otherText.text))
            {
                string content = otherText.text;
                InsertNumberIntoNumArea(content, other.transform.position);
            }
        }
    }
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Num"))
        {
            // �Ƴ��뿪������
            if (triggeredNumbers.Contains(other))
            {
                triggeredNumbers.Remove(other);
                Debug.Log($"�Ƴ���������: {other.name}");
            }
        }
    }

    // 3D��ײ���Ҳ��ͬ���޸�

    private void OnTriggerExit(Collider2D other)
    {
        if (other.CompareTag("Num"))
        {
            if (triggeredNumbers.Contains(other))
            {
                triggeredNumbers.Remove(other);
                Debug.Log($"�Ƴ���������: {other.name}");
            }
        }
    }

    // ���������ֺ������д���������
    public void ProcessTriggeredNumbersAfterDrop()
    {
        if (triggeredNumbers.Count == 0) return;

        Debug.Log($"���ֺ��� {triggeredNumbers.Count} ����������");

        foreach (Collider2D collider in triggeredNumbers.ToArray())
        {
            if (collider != null)
            {
                Text otherText = collider.GetComponent<Text>();
                if (otherText != null && !string.IsNullOrEmpty(otherText.text))
                {
                    string content = otherText.text;
                    // ʹ���϶�λ�ý��в���
                    InsertNumberIntoNumArea(content, collider.transform.position);
                }
            }
        }

        // ��ռ�¼
        triggeredNumbers.Clear();
    }
    public bool ContainsTriggeredNumber(Collider2D numberCollider)
    {
        return triggeredNumbers.Contains(numberCollider);
    }
    private void RefreshNumAreaLayout()
    {
        Transform numArea = transform.Find("NumArea");
        if (numArea == null) return;

        // ��ȡGridLayoutGroup���
        GridLayoutGroup gridLayout = numArea.GetComponent<GridLayoutGroup>();
        if (gridLayout != null)
        {
            // ǿ���ؽ�����
            LayoutRebuilder.ForceRebuildLayoutImmediate(numArea.GetComponent<RectTransform>());

            // ȷ��������������ͬһˮƽ����
            ForceHorizontalAlignment(numArea);

            Debug.Log("ˢ��NumArea���� - ˮƽ����");
        }
    }
    private void ForceHorizontalAlignment(Transform numArea)
    {
        // ��ȡ����������
        List<Transform> children = GetAllNumAreaChildren(numArea);

        // ����ˮƽ����λ��
        float totalWidth = (children.Count - 1) * 35f; // ����ÿ�����ּ��35����
        float startX = -totalWidth / 2f;

        for (int i = 0; i < children.Count; i++)
        {
            RectTransform childRect = children[i].GetComponent<RectTransform>();
            if (childRect != null)
            {
                // ǿ������Y����Ϊ0��ȷ����ͬһˮƽ����
                childRect.anchoredPosition = new Vector2(startX + i * 35f, 0f);

                // ȷ��û����ת
                childRect.localRotation = Quaternion.identity;
            }
        }
    }
}
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

    [Header("UI组件")]
    public Text nameText;
    public Text descriptionText;
    public Text manaCostText;
    public Text valueText;
    public Image artworkImage;

    [Header("数字预制体")]
    public GameObject textNumPrefab; // 在Inspector中分配

    private LineRenderer lineRenderer;
    private bool isDragging = false;
    private Vector3 startPosition;
    private Transform originalParent;
    private CanvasGroup canvasGroup;

    [Header("碰撞检测")]
    public List<Collider2D> triggeredNumbers = new List<Collider2D>(); // 记录触发的数字

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
        data.value = CalculateNumberFromNumArea();
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
    public int CalculateNumberFromNumArea()
    {
        // 获取名字叫NumArea的子物体
        Transform numArea = transform.Find("NumArea");

        // 如果找不到NumArea，返回0
        if (numArea == null)
        {
            Debug.LogWarning("未找到名为NumArea的子物体");
            return 0;
        }

        // 获取NumArea的所有子物体
        List<Transform> childDigits = new List<Transform>();
        for (int i = 0; i < numArea.childCount; i++)
        {
            childDigits.Add(numArea.GetChild(i));
        }

        // 按照子物体在Hierarchy中的顺序排序（从前到后）
        childDigits.Sort((a, b) => a.GetSiblingIndex().CompareTo(b.GetSiblingIndex()));

        // 获取所有子物体的Text组件值
        List<string> contents = new List<string>();
        foreach (Transform child in childDigits)
        {
            Text textComponent = child.GetComponent<Text>();
            if (textComponent != null && !string.IsNullOrEmpty(textComponent.text))
            {
                contents.Add(textComponent.text);
            }
        }

        // 如果没有任何内容，返回0
        if (contents.Count == 0)
        {
            Debug.LogWarning("NumArea中没有有效的内容");
            return 0;
        }

        // 计算表达式结果
        int result = CalculateExpression(contents);
        Debug.Log($"从NumArea计算出的数字: {result} (表达式: [{string.Join(" ", contents)}])");
        return result;
    }

    // 计算表达式结果
    private int CalculateExpression(List<string> contents)
    {
        // 检查是否包含运算符
        bool hasOperator = contents.Any(content => content == "+" || content == "-");

        if (!hasOperator)
        {
            // 没有运算符，直接拼接所有数字
            string combined = string.Join("", contents);
            if (int.TryParse(combined, out int result))
            {
                return result;
            }
            return 0;
        }
        else
        {
            // 有运算符，需要解析表达式
            // 先将连续的数字合并
            List<string> parsedContents = new List<string>();
            StringBuilder currentNumber = new StringBuilder();

            foreach (string content in contents)
            {
                if (content == "+" || content == "-")
                {
                    // 如果之前有累积的数字，先添加
                    if (currentNumber.Length > 0)
                    {
                        parsedContents.Add(currentNumber.ToString());
                        currentNumber.Clear();
                    }
                    // 添加运算符
                    parsedContents.Add(content);
                }
                else
                {
                    // 累积数字
                    currentNumber.Append(content);
                }
            }

            // 添加最后一个数字
            if (currentNumber.Length > 0)
            {
                parsedContents.Add(currentNumber.ToString());
            }

            // 进行计算
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
        // 检查碰撞物体的tag是否为Num
        if (other.CompareTag("Num"))
        {
            // 获取碰撞物体的Text组件值
            Text otherText = other.GetComponent<Text>();
            if (otherText != null && !string.IsNullOrEmpty(otherText.text))
            {
                string content = otherText.text;
                // 在NumArea中生成新的数字或符号
                InsertNumberIntoNumArea(content, other.transform.position);
            }
        }
    }
    // 在NumArea中插入数字
    // 在NumArea中插入数字
    // 在NumArea中插入数字
    private void InsertNumberIntoNumArea(string content, Vector3 collisionPosition)
    {
        Transform numArea = transform.Find("NumArea");
        if (numArea == null)
        {
            Debug.LogWarning("未找到NumArea");
            return;
        }

        if (textNumPrefab == null)
        {
            textNumPrefab = Resources.Load<GameObject>("Prefabs/TextNum");
            if (textNumPrefab == null)
            {
                Debug.LogError("无法加载预制体: Assets/Prefabs/TextNum.prefab");
                return;
            }
        }

        // 获取NumArea的所有子物体
        List<Transform> numChildren = GetAllNumAreaChildren(numArea);

        // 找到插入位置
        int insertIndex = FindInsertIndex(numChildren, collisionPosition);

        // 生成新的数字或符号预制体
        GameObject newObj = Instantiate(textNumPrefab, numArea);
        Text newText = newObj.GetComponent<Text>();
        if (newText != null)
        {
            newText.text = content;

            // 根据内容设置颜色
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

        // 添加拖动组件
        NumberDragController dragController = newObj.AddComponent<NumberDragController>();

        // 设置占位符预制体（如果有的话）
        if (GetComponent<NumberDragController>() != null && GetComponent<NumberDragController>().placeholderPrefab != null)
        {
            dragController.placeholderPrefab = GetComponent<NumberDragController>().placeholderPrefab;
        }

        // 设置位置
        newObj.transform.SetSiblingIndex(insertIndex);

        Debug.Log($"插入内容 '{content}' 到位置 {insertIndex}");

        // 刷新NumArea布局 - 确保水平排列
        RefreshNumAreaLayout();

        // 更新卡牌数值（进行运算）
        UpdateCardValueAfterInsertion();
        GameManager.Instance.UpdateUI();
    }

    // 从拖动插入数字
    public void InsertNumberFromDrag(string content, int insertIndex)
    {
        Transform numArea = transform.Find("NumArea");
        if (numArea == null) return;

        if (textNumPrefab == null)
        {
            textNumPrefab = Resources.Load<GameObject>("Prefabs/TextNum");
            if (textNumPrefab == null) return;
        }

        // 生成新的数字或符号
        GameObject newObj = Instantiate(textNumPrefab, numArea);
        Text newText = newObj.GetComponent<Text>();
        if (newText != null)
        {
            newText.text = content;

            // 根据内容设置颜色
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

        // 设置位置
        newObj.transform.SetSiblingIndex(insertIndex);

        // 添加拖动组件，让新内容也可以被拖动
        newObj.AddComponent<NumberDragController>();

        // 刷新NumArea布局 - 确保水平排列
        RefreshNumAreaLayout();

        // 更新卡牌数值（进行运算）
        UpdateCardValueAfterInsertion();
        GameManager.Instance.UpdateUI();

        Debug.Log($"松手后插入内容 '{content}' 到位置 {insertIndex}");
    }
    // 从拖动插入数字
    
    // 删除或注释掉原来的 OnNumTriggerEnter 方法
    /*
    public void OnNumTriggerEnter(Collider2D other)
    {
        // 这个方法不再自动插入
    }
    */

    // 插入数字后更新卡牌数值
    private void UpdateCardValueAfterInsertion()
    {
        // 重新计算NumArea中的数字
        int newValue = CalculateNumberFromNumArea();

        // 更新卡牌数据
        cardData.value = newValue;

        // 更新UI显示
        if (valueText != null)
        {
            valueText.text = newValue.ToString();
        }

        Debug.Log($"卡牌数值更新为: {newValue}");
    }

    // 获取NumArea的所有子物体并按顺序排序
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

    // 根据碰撞位置找到插入位置
    private int FindInsertIndex(List<Transform> numChildren, Vector3 collisionPosition)
    {
        if (numChildren.Count == 0) return 0;

        Vector2 screenCollisionPos = collisionPosition;
      //  Debug.Log($"屏幕碰撞位置: {screenCollisionPos}");

        // 获取所有数字的屏幕位置
        List<float> numScreenPositions = new List<float>();
        foreach (Transform child in numChildren)
        {
            // 对于Screen Space模式，直接使用position的x坐标
            numScreenPositions.Add(child.position.x);
        }

        // 检查插入位置
        for (int i = 0; i < numScreenPositions.Count - 1; i++)
        {
            float currentX = numScreenPositions[i];
            float nextX = numScreenPositions[i + 1];
            float midPoint = (currentX + nextX) / 2f;

            Debug.Log($"比较: 碰撞{screenCollisionPos.x} 在 {currentX} 和 {nextX} 之间, 中点{midPoint}");

            if (screenCollisionPos.x >= currentX && screenCollisionPos.x < nextX)
            {
                // 判断是插入在当前位置还是下一个位置
                if (screenCollisionPos.x < midPoint)
                {
                    Debug.Log($"插入到位置 {i + 1} (靠近数字{i})");
                    return i + 1;
                }
                else
                {
                    Debug.Log($"插入到位置 {i + 1} (靠近数字{i + 1})");
                    return i + 1;
                }
            }
        }

        // 边界情况
        if (screenCollisionPos.x < numScreenPositions[0])
        {
            Debug.Log($"插入到位置 0 (第一个之前)");
            return 0;
        }
        else if (screenCollisionPos.x >= numScreenPositions[numScreenPositions.Count - 1])
        {
            Debug.Log($"插入到末尾 {numChildren.Count}");
            return numChildren.Count;
        }

        Debug.Log($"默认插入到末尾 {numChildren.Count}");
        return numChildren.Count;
    }
    // 计算GridLayout的起始X坐标（考虑不同的对齐方式）
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

    // 碰撞检测函数（需要添加到有碰撞器的物体上

    // 如果需要3D碰撞检测
    public int FindInsertIndexForDrag(Vector3 dragPosition)
    {
        Transform numArea = transform.Find("NumArea");
        if (numArea == null) return -1;

        // 获取所有子物体
        List<Transform> numChildren = GetAllNumAreaChildren(numArea);
        if (numChildren.Count == 0) return 0;

        // 将拖动位置转换为NumArea的局部坐标
        Vector2 localPos;
        RectTransform numAreaRect = numArea.GetComponent<RectTransform>();
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            numAreaRect, dragPosition, null, out localPos))
        {
            // 基于局部坐标计算插入位置
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

    // 从拖动插入数字
  
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
            // 移除离开的数字
            if (triggeredNumbers.Contains(other))
            {
                triggeredNumbers.Remove(other);
                Debug.Log($"移除触发数字: {other.name}");
            }
        }
    }

    // 3D碰撞检测也做同样修改

    private void OnTriggerExit(Collider2D other)
    {
        if (other.CompareTag("Num"))
        {
            if (triggeredNumbers.Contains(other))
            {
                triggeredNumbers.Remove(other);
                Debug.Log($"移除触发数字: {other.name}");
            }
        }
    }

    // 新增：松手后处理所有触发的数字
    public void ProcessTriggeredNumbersAfterDrop()
    {
        if (triggeredNumbers.Count == 0) return;

        Debug.Log($"松手后处理 {triggeredNumbers.Count} 个触发数字");

        foreach (Collider2D collider in triggeredNumbers.ToArray())
        {
            if (collider != null)
            {
                Text otherText = collider.GetComponent<Text>();
                if (otherText != null && !string.IsNullOrEmpty(otherText.text))
                {
                    string content = otherText.text;
                    // 使用拖动位置进行插入
                    InsertNumberIntoNumArea(content, collider.transform.position);
                }
            }
        }

        // 清空记录
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

        // 获取GridLayoutGroup组件
        GridLayoutGroup gridLayout = numArea.GetComponent<GridLayoutGroup>();
        if (gridLayout != null)
        {
            // 强制重建布局
            LayoutRebuilder.ForceRebuildLayoutImmediate(numArea.GetComponent<RectTransform>());

            // 确保所有子物体在同一水平线上
            ForceHorizontalAlignment(numArea);

            Debug.Log("刷新NumArea布局 - 水平排列");
        }
    }
    private void ForceHorizontalAlignment(Transform numArea)
    {
        // 获取所有子物体
        List<Transform> children = GetAllNumAreaChildren(numArea);

        // 计算水平中心位置
        float totalWidth = (children.Count - 1) * 35f; // 假设每个数字间隔35像素
        float startX = -totalWidth / 2f;

        for (int i = 0; i < children.Count; i++)
        {
            RectTransform childRect = children[i].GetComponent<RectTransform>();
            if (childRect != null)
            {
                // 强制设置Y坐标为0，确保在同一水平线上
                childRect.anchoredPosition = new Vector2(startX + i * 35f, 0f);

                // 确保没有旋转
                childRect.localRotation = Quaternion.identity;
            }
        }
    }
}
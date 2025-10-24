using UnityEngine;

public class Targetable : MonoBehaviour
{
    public bool isPlayer;

    void Start()
    {
        // 设置标签以便检测
        gameObject.tag = isPlayer ? "Player" : "Enemy";
    }

    void OnMouseEnter()
    {
        // 鼠标悬停时的高亮效果
        GetComponent<SpriteRenderer>().color = Color.yellow;
    }

    void OnMouseExit()
    {
        // 恢复原色
        GetComponent<SpriteRenderer>().color = Color.white;
    }
}
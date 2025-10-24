using UnityEngine;

public class Targetable : MonoBehaviour
{
    public bool isPlayer;

    void Start()
    {
        // ���ñ�ǩ�Ա���
        gameObject.tag = isPlayer ? "Player" : "Enemy";
    }

    void OnMouseEnter()
    {
        // �����ͣʱ�ĸ���Ч��
        GetComponent<SpriteRenderer>().color = Color.yellow;
    }

    void OnMouseExit()
    {
        // �ָ�ԭɫ
        GetComponent<SpriteRenderer>().color = Color.white;
    }
}
using UnityEngine;
using UnityEngine.UI;

public class SceneButtonClick : MonoBehaviour
{
    void Start()
    {
        // ֱ����Ӽ���
        GetComponent<Button>().onClick.AddListener(() => {
            GameManager.Instance.ReloadCurrentScene();
            YourFunction();
        });
    }

    void YourFunction()
    {
        // ��ĺ����߼�
        Debug.Log("��ť��������ִ��");
    }
}
using UnityEngine;
using UnityEngine.UI;

public class SceneButtonClick : MonoBehaviour
{
    void Start()
    {
        // 直接添加监听
        GetComponent<Button>().onClick.AddListener(() => {
            GameManager.Instance.ReloadCurrentScene();
            YourFunction();
        });
    }

    void YourFunction()
    {
        // 你的函数逻辑
        Debug.Log("按钮触发函数执行");
    }
}
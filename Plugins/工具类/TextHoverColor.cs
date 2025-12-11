using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


public class TextHoverColor : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Color normalColor = Color.white;   // 默认颜色
    public Color hoverColor = Color.yellow;   // 鼠标悬停颜色


    private Text textComp;


    void Awake()
    {
        textComp = GetComponentInChildren<Text>();
        textComp.color = normalColor; // 初始颜色
    }


    public void OnPointerEnter(PointerEventData eventData)
    {
        textComp.color = hoverColor;
    }


    public void OnPointerExit(PointerEventData eventData)
    {
        textComp.color = normalColor;
    }
}
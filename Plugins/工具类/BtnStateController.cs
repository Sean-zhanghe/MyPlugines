using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


public enum BtnState
{
    Normal,
    Hover,
    Select
}

public class BtnStateController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Color normalColor = Color.white;   // 默认颜色
    public Color hoverColor = Color.yellow;   // 鼠标悬停颜色
    public Color selectColor = Color.white;   // 点击颜色


    [SerializeField] private Text textComp;
    [SerializeField] private Transform state;

    private BtnState curState;

    void Awake()
    {
        RefreshBtnState(BtnState.Normal);
    }

    public void RefreshBtnState(BtnState _state)
    {
        switch (_state)
        {
            case BtnState.Normal:
                textComp.color = normalColor;
                break;
            case BtnState.Hover:
                textComp.color = hoverColor;
                break;
            case BtnState.Select:
                textComp.color = selectColor;
                break;
            default:
                textComp.color = normalColor;
                break;
        }
        for (int i = 0; i < state.childCount; i++)
        {
            GameObject item = state.GetChild(i).gameObject;
            item.SetActive(item.name == _state.ToString());
        }
        curState = _state;
    }


    public void OnPointerEnter(PointerEventData eventData)
    {
        if (curState == BtnState.Select) return;
        RefreshBtnState(BtnState.Hover);
    }


    public void OnPointerExit(PointerEventData eventData)
    {
        if (curState == BtnState.Select) return;
        RefreshBtnState(BtnState.Normal);
    }
}
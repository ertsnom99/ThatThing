using UnityEngine;
using UnityEngine.EventSystems;

public class DebugWindowLevelRenderer : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public delegate void DebugWindowImageDelegate();
    public DebugWindowImageDelegate _onBeginDragDelegate;
    public DebugWindowImageDelegate _onEndDragDelegate;
    public DebugWindowImageDelegate _onPointerEnterDelegate;
    public DebugWindowImageDelegate _onPointerExitDelegate;
    public DebugWindowImageDelegate _onPointerLeftClickDelegate;

    public void OnDrag(PointerEventData eventData) { }

    public void OnBeginDrag(PointerEventData eventData)
    {
        _onBeginDragDelegate();
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        _onEndDragDelegate();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _onPointerEnterDelegate();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _onPointerExitDelegate();
    }

    public void OnPointerClick(PointerEventData pointerEventData)
    {
        if (pointerEventData.button == PointerEventData.InputButton.Left)
        {
            _onPointerLeftClickDelegate();
        }
    }
}

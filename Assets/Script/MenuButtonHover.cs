using UnityEngine;
using UnityEngine.EventSystems;

public class MenuButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    [SerializeField] private GameObject hoverArrow;

    private void Awake()
    {
        if (hoverArrow != null)
        {
            hoverArrow.SetActive(false);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        SetArrowVisible(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        SetArrowVisible(false);
    }

    public void OnSelect(BaseEventData eventData)
    {
        SetArrowVisible(true);
    }

    public void OnDeselect(BaseEventData eventData)
    {
        SetArrowVisible(false);
    }

    private void SetArrowVisible(bool visible)
    {
        if (hoverArrow != null)
        {
            hoverArrow.SetActive(visible);
        }
    }
}



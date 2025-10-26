using UnityEngine;
using UnityEngine.EventSystems;

public class GridCellUIControl : MonoBehaviour, IPointerClickHandler
{
    public int x;
    public int y;

    public void Init(int gridX, int gridY)
    {
        x = gridX;
        y = gridY;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
       // GomokuManager.Instance.OnGridClicked(x, y);
    }
}

using UnityEngine;

public class BackgroundZoom : MonoBehaviour
{
    public RectTransform background;

    public void ZoomCenter()
    {
        background.localScale = new Vector3(1.5f, 1.5f, 1f);
        background.anchoredPosition = Vector2.zero;
    }

    public void ZoomRight()
    {
        background.localScale = new Vector3(1.5f, 1.5f, 1f);
        background.anchoredPosition = new Vector2(-300f, 0f);
    }

    public void ZoomLeft()
    {
        background.localScale = new Vector3(1.5f, 1.5f, 1f);
        background.anchoredPosition = new Vector2(300f, 0f);
    }

    public void ResetZoom()
    {
        background.localScale = Vector3.one;
        background.anchoredPosition = Vector2.zero;
    }
}
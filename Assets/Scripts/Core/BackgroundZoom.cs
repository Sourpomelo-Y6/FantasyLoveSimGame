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

    public void CaptureState(out Vector3 localScale, out Vector2 anchoredPosition)
    {
        if (background == null)
        {
            localScale = Vector3.one;
            anchoredPosition = Vector2.zero;
            return;
        }

        localScale = background.localScale;
        anchoredPosition = background.anchoredPosition;
    }

    public void RestoreState(Vector3 localScale, Vector2 anchoredPosition)
    {
        if (background == null)
        {
            return;
        }

        background.localScale = localScale;
        background.anchoredPosition = anchoredPosition;
    }
}

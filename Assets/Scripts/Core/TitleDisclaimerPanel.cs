using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// タイトルへ最初に入ったときだけ注意文を表示し、クリックで閉じる。
/// 表示済み状態はゲームセーブへ保存せず、起動中のセッションだけ保持する。
/// </summary>
public class TitleDisclaimerPanel : MonoBehaviour, IPointerClickHandler
{
    private static bool hasDismissedThisSession;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ResetSessionState()
    {
        hasDismissedThisSession = false;
    }

    private void Awake()
    {
        if (hasDismissedThisSession)
        {
            gameObject.SetActive(false);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData != null && eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        Dismiss();
    }

    public void Dismiss()
    {
        hasDismissedThisSession = true;
        gameObject.SetActive(false);
    }
}

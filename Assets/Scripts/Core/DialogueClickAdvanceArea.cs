using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
public class DialogueClickAdvanceArea : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private GameManager gameManager;

    public void Initialize(GameManager owner)
    {
        gameManager = owner;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (gameManager == null)
        {
            return;
        }

        gameManager.OnDialogueWindowClicked();
    }
}

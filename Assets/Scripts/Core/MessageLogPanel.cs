using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MessageLogPanel : MonoBehaviour
{
    public struct MessageLogEntry
    {
        public readonly string SpeakerName;
        public readonly string Message;
        public readonly Color SpeakerColor;
        public readonly Color MessageColor;

        public MessageLogEntry(string speakerName, string message, Color speakerColor, Color messageColor)
        {
            SpeakerName = speakerName;
            Message = message;
            SpeakerColor = speakerColor;
            MessageColor = messageColor;
        }
    }

    [SerializeField] private GameManager gameManager;
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Button closeButton;
    [SerializeField] private Transform listParent;
    [SerializeField] private GameObject rowPrefab;
    [SerializeField] private TextMeshProUGUI emptyText;
    [SerializeField] private string emptyMessage = "ログはまだありません。";
    [SerializeField] private string speakerTextName = "SpeakerNameText";
    [SerializeField] private string dialogueTextName = "DialogueText";

    private readonly List<GameObject> spawnedRows = new List<GameObject>();

    private void Awake()
    {
        EnsureReferences();

        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(Close);
            closeButton.onClick.AddListener(Close);
        }
    }

    public void Initialize(GameManager manager)
    {
        gameManager = manager;
        EnsureReferences();
    }

    public void Open(IReadOnlyList<MessageLogEntry> entries)
    {
        EnsureReferences();
        Refresh(entries);

        if (panelRoot != null)
        {
            panelRoot.SetActive(true);
        }
    }

    public void Close()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }
    }

    private void Refresh(IReadOnlyList<MessageLogEntry> entries)
    {
        ClearRows();

        bool hasEntries = entries != null && entries.Count > 0;
        if (emptyText != null)
        {
            emptyText.gameObject.SetActive(!hasEntries);
            emptyText.text = emptyMessage;
        }

        if (!hasEntries || listParent == null || rowPrefab == null)
        {
            return;
        }

        for (int i = 0; i < entries.Count; i++)
        {
            MessageLogEntry entry = entries[i];
            GameObject row = Instantiate(rowPrefab, listParent);
            row.SetActive(true);
            spawnedRows.Add(row);

            TextMeshProUGUI speakerText = FindText(row.transform, speakerTextName);
            if (speakerText != null)
            {
                speakerText.text = entry.SpeakerName;
                speakerText.color = entry.SpeakerColor;
            }

            TextMeshProUGUI dialogueText = FindText(row.transform, dialogueTextName);
            if (dialogueText != null)
            {
                dialogueText.text = entry.Message;
                dialogueText.color = entry.MessageColor;
            }
        }
    }

    private void ClearRows()
    {
        for (int i = 0; i < spawnedRows.Count; i++)
        {
            if (spawnedRows[i] != null)
            {
                Destroy(spawnedRows[i]);
            }
        }

        spawnedRows.Clear();
    }

    private void EnsureReferences()
    {
        if (panelRoot == null)
        {
            panelRoot = gameObject;
        }

        if (closeButton == null)
        {
            Transform closeTransform = transform.Find("CloseButton");
            if (closeTransform != null)
            {
                closeButton = closeTransform.GetComponent<Button>();
            }
        }

        if (listParent == null)
        {
            Transform listTransform = FindChildRecursive(transform, "MessageLogList");
            if (listTransform != null)
            {
                listParent = listTransform;
            }
        }
    }

    private TextMeshProUGUI FindText(Transform root, string objectName)
    {
        Transform textTransform = FindChildRecursive(root, objectName);
        return textTransform != null ? textTransform.GetComponent<TextMeshProUGUI>() : null;
    }

    private Transform FindChildRecursive(Transform root, string objectName)
    {
        if (root == null)
        {
            return null;
        }

        if (root.name == objectName)
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform result = FindChildRecursive(root.GetChild(i), objectName);
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }
}

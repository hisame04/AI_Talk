using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class OptionSelectorItem : MonoBehaviour
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text valueText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Button statusButton;
    [SerializeField] private Button previousButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private RectTransform progressContainer;
    [SerializeField] private Image progressSegmentTemplate;
    [SerializeField] private Color selectedSegmentColor = new Color(0f, 0.75f, 1f, 1f);
    [SerializeField] private Color unselectedSegmentColor = new Color(0.45f, 0.52f, 0.57f, 1f);
    [SerializeField] private float segmentSpacing = 4f;

    private string[] options = Array.Empty<string>();
    private int selectedIndex;
    private readonly List<Image> progressSegments = new List<Image>();

    public event Action<int> SelectionRequested;
    public event Action StatusClicked;

    private void Awake()
    {
        previousButton.onClick.AddListener(OnPreviousClicked);
        nextButton.onClick.AddListener(OnNextClicked);
        statusButton.onClick.AddListener(OnStatusClicked);
    }

    private void OnDestroy()
    {
        previousButton.onClick.RemoveListener(OnPreviousClicked);
        nextButton.onClick.RemoveListener(OnNextClicked);
        statusButton.onClick.RemoveListener(OnStatusClicked);
    }

    public void Configure(string title, string[] values, int index)
    {
        options = values ?? Array.Empty<string>();
        SetTitle(title);
        BuildProgressSegments();
        SetSelectedIndex(index);
        SetStatus(null);
    }

    public void SetTitle(string title)
    {
        titleText.text = title;
    }

    public void SetStatus(string status, bool canClick = false)
    {
        if (statusText == null) return;
        bool visible = !string.IsNullOrEmpty(status);
        statusText.text = status ?? string.Empty;
        statusText.raycastTarget = visible && canClick;
        statusButton.enabled = visible && canClick;
        statusText.gameObject.SetActive(visible);
    }

    public void SetNavigationVisible(bool visible)
    {
        previousButton.gameObject.SetActive(visible);
        nextButton.gameObject.SetActive(visible);
    }

    public void SetSelectedIndex(int index)
    {
        if (options.Length == 0) return;
        selectedIndex = Mathf.Clamp(index, 0, options.Length - 1);
        valueText.text = options[selectedIndex];
        for (int i = 0; i < progressSegments.Count; i++)
        {
            if (i < options.Length)
            {
                progressSegments[i].color = i == selectedIndex ? selectedSegmentColor : unselectedSegmentColor;
            }
        }
    }

    public void SetInteractable(bool interactable)
    {
        bool enabled = interactable && options.Length > 1;
        previousButton.interactable = enabled;
        nextButton.interactable = enabled;
    }

    private void RequestSelection(int direction)
    {
        if (options.Length < 2) return;
        int nextIndex = (selectedIndex + direction + options.Length) % options.Length;
        SelectionRequested?.Invoke(nextIndex);
    }

    private void BuildProgressSegments()
    {
        if (progressContainer == null || progressSegmentTemplate == null) return;

        while (progressSegments.Count < options.Length)
        {
            Image segment = Instantiate(progressSegmentTemplate, progressContainer);
            segment.name = $"Segment {progressSegments.Count + 1}";
            segment.sprite = null;
            segment.type = Image.Type.Simple;
            segment.raycastTarget = false;
            progressSegments.Add(segment);
        }

        for (int i = 0; i < progressSegments.Count; i++)
        {
            bool visible = i < options.Length;
            progressSegments[i].gameObject.SetActive(visible);
            if (!visible) continue;

            RectTransform rect = progressSegments[i].rectTransform;
            rect.anchorMin = new Vector2((float)i / options.Length, 0f);
            rect.anchorMax = new Vector2((float)(i + 1) / options.Length, 1f);
            rect.offsetMin = new Vector2(i == 0 ? 0f : segmentSpacing / 2f, 0f);
            rect.offsetMax = new Vector2(i == options.Length - 1 ? 0f : -segmentSpacing / 2f, 0f);
        }
    }

    private void OnPreviousClicked() => RequestSelection(-1);
    private void OnNextClicked() => RequestSelection(1);
    private void OnStatusClicked() => StatusClicked?.Invoke();
}

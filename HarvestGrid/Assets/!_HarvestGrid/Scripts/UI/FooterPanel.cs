using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class FooterPanel : MonoBehaviour
{
    [SerializeField]
    private Dictionary<Button, GameObject> buttonAndTabs;

    [SerializeField]
    private Button defaultButtonToOpenOnStart;

    [Header("Button Visual")]
    [SerializeField]
    private Sprite normalStateSprite;

    [SerializeField]
    private Sprite pressedStateSprite;

    [SerializeField]
    private float pressedScale = 1.1f;

    [SerializeField]
    private float animationTime = 0.2f;

    private Button currentButton;

    private void Start()
    {
        Initialize();

        if (defaultButtonToOpenOnStart != null)
        {
            ShowTab(defaultButtonToOpenOnStart);
        }
    }

    private void Initialize()
    {
        foreach (var pair in buttonAndTabs)
        {
            Button button = pair.Key;

            if (button == null)
                continue;

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => ShowTab(button));

            SetButtonNormal(button);
        }

        HideAllTab();
    }

    public void ShowTab(Button button)
    {
        if (button == null || !buttonAndTabs.TryGetValue(button, out GameObject tab))
            return;

        if (currentButton == button)
            return;

        foreach (var pair in buttonAndTabs)
        {
            if (pair.Value != null)
                pair.Value.SetActive(pair.Key == button);
        }

        if (currentButton != null)
            SetButtonNormal(currentButton);

        SetButtonPressed(button);

        currentButton = button;
    }

    public void HideAllTab()
    {
        foreach (var pair in buttonAndTabs)
        {
            if (pair.Value != null)
                pair.Value.SetActive(false);

            if (pair.Key != null)
                SetButtonNormal(pair.Key);
        }

        currentButton = null;
    }

    private void SetButtonNormal(Button button)
    {
        if (button == null)
            return;

        Image image = button.targetGraphic as Image;

        if (image != null && normalStateSprite != null)
            image.sprite = normalStateSprite;

        button.transform.DOKill();
        button.transform.DOScale(1f, animationTime);
    }

    private void SetButtonPressed(Button button)
    {
        if (button == null)
            return;

        // Move selected button to the top of its parent's hierarchy
        button.transform.SetAsLastSibling();

        Image image = button.targetGraphic as Image;

        if (image != null && pressedStateSprite != null)
            image.sprite = pressedStateSprite;

        button.transform.DOKill();
        button.transform.DOScale(pressedScale, animationTime);
    }
}
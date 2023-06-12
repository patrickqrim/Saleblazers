using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;
using TMPro;

public class HRMainMenuStartJoinButton : Button
{
    public TextMeshProUGUI ButtonText;
    public RectTransform ButtonTextRect;

    public Image Shine;
    public RectTransform ShineRect;

    public RectTransform Rect;

    private bool bTweenText = true;
    private bool bTweenFill = true;
    private bool bStartExpanded = false;

    Sequence ShineSequence;

    private static List<HRMainMenuStartJoinButton> ExpandedButtons = new List<HRMainMenuStartJoinButton>();


    protected override void Awake()
    {
        bTweenText = !this.name.Contains("[NOTEXT]");
        bTweenFill = !this.name.Contains("[NOFILL]");
        bStartExpanded = this.name.Contains("[EXPAND]");

        base.Awake();

        Rect = this.transform as RectTransform;

        ButtonText = GetComponentInChildren<TextMeshProUGUI>();
        ButtonTextRect = ButtonText.rectTransform;
        ButtonTextRect.DOAnchorPosX(22, 0.2f).SetEase(Ease.OutQuad);

        Shine = transform.Find("Shine Mask").GetChild(0).GetComponent<Image>();
        ShineRect = Shine.transform as RectTransform;
    }


    protected override void Start()
    {
        base.Start();

        if (bStartExpanded)
        {
            ButtonTextRect.DOAnchorPosY(6, 0.2f).SetEase(Ease.OutQuad);
            //ForceHighlight();
        }
    }


    public void ForceHighlight()
    {
        (targetGraphic as Image).sprite = spriteState.highlightedSprite;

        if (bTweenText)
        {
            ButtonTextRect.DOAnchorPosX(22, 0.2f).SetEase(Ease.OutQuad);
        }

        if (bTweenFill)
        {
            (targetGraphic as Image).DOFillAmount(1, 0.2f).SetEase(Ease.OutQuad);
        }

        if (ShineSequence != null)
        {
            ShineSequence.Kill();
        }

        Shine.gameObject.SetActive(true);

        ShineSequence = DOTween.Sequence();
        ShineSequence.Append(ShineRect.DOAnchorPos(new Vector2((Rect.rect.width / 2) + 10.0f, ShineRect.anchoredPosition.y), 1.0f).
            From(new Vector2(-(Rect.rect.width / 2) - 100, ShineRect.anchoredPosition.y)).SetEase(Ease.Linear)).AppendInterval(2);
        ShineSequence.SetLoops(-1);
        ShineSequence.Play();

        ExpandedButtons.Add(this);
    }


    public override void OnPointerEnter(PointerEventData eventData)
    {
        foreach (var item in ExpandedButtons)
        {
            if(item != this)
            {
                item.OnPointerExit(eventData);
            }
        }

        ExpandedButtons.Clear();

        base.OnPointerEnter(eventData);

        if (bTweenText)
        {
            ButtonTextRect.DOAnchorPosX(47, 0.2f).SetEase(Ease.OutQuad);
        }

        if (bTweenFill)
        {
            (targetGraphic as Image).DOFillAmount(1, 0.2f).SetEase(Ease.OutQuad);
        }

        if (ShineSequence != null)
        {
            ShineSequence.Kill();
        }

        Shine.gameObject.SetActive(true);

        ShineSequence = DOTween.Sequence();
        ShineSequence.Append(ShineRect.DOAnchorPos(new Vector2((Rect.rect.width / 2) + 10.0f, ShineRect.anchoredPosition.y), 1.0f).
            From(new Vector2(-(Rect.rect.width / 2) - 100, ShineRect.anchoredPosition.y)).SetEase(Ease.Linear)).AppendInterval(2);
        ShineSequence.SetLoops(-1);
        ShineSequence.Play();
    }

    public override void OnPointerExit(PointerEventData eventData)
    {
        base.OnPointerExit(eventData);

        if (bTweenText)
        {
            ButtonTextRect.DOAnchorPosX(22, 0.2f).SetEase(Ease.OutQuad);
        }

        //ButtonTextRect.DOAnchorPosY(6, 0.2f).SetEase(Ease.OutQuad);

        if (bTweenFill)
        {
            (targetGraphic as Image).DOFillAmount(0, 0.2f).SetEase(Ease.InQuad);
        }

        Shine.gameObject.SetActive(false);

        if(ShineSequence != null)
        {
            ShineSequence.Kill();
        }

        if (EventSystem.current.currentSelectedGameObject == this.gameObject)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }


    public override void OnPointerDown(PointerEventData eventData)
    {
        base.OnPointerDown(eventData);

        if (bStartExpanded)
        {
            ButtonTextRect.DOAnchorPosY(0, 0.2f).SetEase(Ease.OutQuad);
        }
        else
        {
            ButtonTextRect.DOAnchorPosY(-2, 0.2f).SetEase(Ease.OutQuad);
        }

        ShineRect.DOKill();
        Shine.gameObject.SetActive(false);
    }


    public override void OnPointerUp(PointerEventData eventData)
    {
        base.OnPointerUp(eventData);

        if (bStartExpanded)
        {
            ButtonTextRect.DOAnchorPosY(6, 0.2f).SetEase(Ease.OutQuad);
        }
        else
        {
            ButtonTextRect.DOAnchorPosY(0, 0.2f).SetEase(Ease.OutQuad);
        }
    }

    protected override void OnDisable()
    {
        base.OnDisable();

        ButtonText.color = Color.gray;
        //ButtonTextRect.DOAnchorPosY(0, 0.2f).SetEase(Ease.OutQuad);
    }

    protected override void OnEnable()
    {
        base.OnEnable();

        ButtonText.color = Color.white;
    }

    protected override void OnDestroy()
    {
        if (bStartExpanded && ExpandedButtons.Contains(this))
        {
            ExpandedButtons.Remove(this);
        }
    }
}

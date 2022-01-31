using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class UIButtonVisual : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    [SerializeField] private float fontSizeMultiplier = 1.2f;
    [SerializeField] private Color highLightColor = Color.white;

    float defaultFontSize = 0;
    Color defaultColor = Color.white;
    TextMeshProUGUI buttonText = null;

    void Start()
    {
        buttonText = GetComponentInChildren<TextMeshProUGUI>();
        defaultFontSize = buttonText.fontSize;
        defaultColor = buttonText.color;
    }

    void OnDisable()
    {
        if(buttonText != null)
        {
            buttonText.fontSize = defaultFontSize;
            buttonText.color = defaultColor;
        }  
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if(buttonText.fontSize == defaultFontSize)
        {
            buttonText.fontSize *= fontSizeMultiplier;
            buttonText.color = highLightColor;
            EventSystem.current.SetSelectedGameObject(this.gameObject);
            UIManager.Instance.ButtonSound();
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        buttonText.fontSize = defaultFontSize;
    }

    // Keyboard input needs updating as well
    void Update()
    {
        
    }

    public void OnSelect(BaseEventData eventData)
    {
        if(buttonText.fontSize == defaultFontSize)
        {
            buttonText.fontSize *= fontSizeMultiplier;
            buttonText.color = highLightColor;
        }    
    }

    public void OnDeselect(BaseEventData eventData)
    {
        buttonText.fontSize = defaultFontSize;
        buttonText.color = defaultColor;
    }
}

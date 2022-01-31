using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ProgressBar : MonoBehaviour
{
    [Header("Progress Bar Values")]
    [SerializeField] private int maximum = 0;
    [SerializeField] private int minimum = 0;
    [SerializeField] private int current = 0;

    [Header("Optional Assignables")]
    [SerializeField] private Image mask = null;
    [SerializeField] private Image fill = null;
    [SerializeField] private Color fullColor;

    [Header("Optional Functionality")]
    [SerializeField] private bool useLerp = false;
    [SerializeField] private bool changeColorOnFull = false;

    // Default color
    private Color defaultColor;

    private void Start()
    {
        if (fill != null)
            defaultColor = fill.color;

        else
            defaultColor = mask.color;
    }

    void SetCurrentFill()
    {
        if(useLerp)
        {
            StartCoroutine(FillWithLerp());
        }

        else
        {
            float currentOffset = current - minimum;
            float maximumOffset = maximum - minimum;
            float fillAmount = currentOffset / maximumOffset;
            mask.fillAmount = fillAmount;
            SetColor();
        }
    }

    IEnumerator FillWithLerp()
    {
        float currentOffset = current - minimum;
        float maximumOffset = maximum - minimum;

        float fillCurrent = mask.fillAmount;
        float fillTarget = currentOffset / maximumOffset;

        float timeElapsed = 0f;
        float lerpDuration = .5f;

        while (timeElapsed < lerpDuration)
        {
            mask.fillAmount = Mathf.Lerp(fillCurrent, fillTarget, timeElapsed / lerpDuration);
            timeElapsed += Time.deltaTime;
            yield return null;
        }

        mask.fillAmount = fillTarget;
        SetColor();
    }

    void SetColor()
    {
        if (!changeColorOnFull)
            return;

        if (current >= maximum)
            fill.color = fullColor;

        else
            fill.color = defaultColor;
    }

    public void SetCurrentValue(int value)
    {
        if (value > maximum)
            value = maximum;

        current = value;
        SetCurrentFill();
    }

    public void SetMaxValue(int value)
    {
        maximum = value;
    }

    public void SetMinValue(int value)
    {
        minimum = value;
    }
}

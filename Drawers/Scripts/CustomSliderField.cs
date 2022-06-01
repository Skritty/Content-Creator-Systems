using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using RuntimeInspectorNamespace;

public class CustomSliderField : InspectorField
{
	private static readonly HashSet<Type> supportedTypes = new HashSet<Type>()
		{
			typeof( float ), typeof( double ), typeof( decimal )
		};

#pragma warning disable 0649
	[SerializeField]
	protected BoundInputField input;
	[SerializeField]
	private BoundSlider slider;
#pragma warning restore 0649

	protected INumberHandler numberHandler;
	bool useRange = false;
	public System.Action<float> onStartInputChange;
	public void SetSlider(float min, float max)
    {
		useRange = true;
		slider.SetRange(min, max);
	}

	public override void Initialize()
	{
		base.Initialize();

		input.Initialize();
		input.OnValueChanged += OnValueChanged;
		input.OnValueSubmitted += OnValueSubmitted;
		input.DefaultEmptyValue = "0";
		slider.OnValueChanged += OnSliderValueChanged;
		slider.onBeginDrag += (v) => onStartInputChange?.Invoke(v);
	}

	public override bool SupportsType(Type type)
	{
		return supportedTypes.Contains(type);
	}

	protected override void OnBound(MemberInfo variable)
	{
		base.OnBound(variable);

		if (BoundVariableType == typeof(float) || BoundVariableType == typeof(double) || BoundVariableType == typeof(decimal))
			input.BackingField.contentType = InputField.ContentType.DecimalNumber;
		else
			input.BackingField.contentType = InputField.ContentType.IntegerNumber;

		numberHandler = NumberHandlers.Get(BoundVariableType);
		input.Text = numberHandler.ToString(Value);

		slider.BackingField.wholeNumbers = BoundVariableType != typeof(float) && BoundVariableType != typeof(double) && BoundVariableType != typeof(decimal);
		useRange = false;
	}

	protected virtual bool OnValueChanged(BoundInputField source, string input)
	{
		object value;
		if (numberHandler.TryParse(input, out value))
		{
            if (useRange)
            {
				float fvalue = numberHandler.ConvertToFloat(value);
				if (fvalue >= slider.BackingField.minValue && fvalue <= slider.BackingField.maxValue)
				{
					Value = value;
					return true;
				}
			}
            else
            {
				Value = value;
				return true;
			}
		}

		return false;
	}

	private void OnSliderValueChanged(BoundSlider source, float value)
	{
		if (input.BackingField.isFocused)
			return;

		Value = numberHandler.ConvertFromFloat(value);
		input.Text = numberHandler.ToString(Value);
		Inspector.RefreshDelayed();
	}

	private bool OnValueSubmitted(BoundInputField source, string input)
	{
		Inspector.RefreshDelayed();
		return OnValueChanged(source, input);
	}

	protected override void OnSkinChanged()
	{
		base.OnSkinChanged();
		input.Skin = Skin;

		Vector2 rightSideAnchorMin = new Vector2(Skin.LabelWidthPercentage, 0f);
		variableNameMask.rectTransform.anchorMin = rightSideAnchorMin;
		((RectTransform)input.transform).anchorMin = rightSideAnchorMin;

		float inputFieldWidth = (1f - Skin.LabelWidthPercentage) / 3f;
		variableNameMask.rectTransform.anchorMin = rightSideAnchorMin;
		((RectTransform)slider.transform).anchorMin = rightSideAnchorMin;
		((RectTransform)slider.transform).anchorMax = new Vector2(1f - inputFieldWidth, 1f);
		((RectTransform)input.transform).anchorMin = new Vector2(1f - inputFieldWidth, 0f);
	}

	public override void Refresh()
	{
		object prevVal = Value;
		base.Refresh();

        if (useRange)
        {
			//input.gameObject.SetActive(false);
			slider.gameObject.SetActive(true);
        }
        else
        {
			//input.gameObject.SetActive(true);
			slider.gameObject.SetActive(false);
		}

		if (!numberHandler.ValuesAreEqual(Value, prevVal))
			input.Text = numberHandler.ToString(Value);
		slider.Value = numberHandler.ConvertToFloat(Value);
	}
}
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class RadioButtonGroup : MonoBehaviour
{
    [SerializeField]
    private Button _defaultButton;
    public Button DefaultButton
    {
        get => _defaultButton;
        set
        {
            _defaultButton = value;
            if (!actions.ContainsKey(_defaultButton))
                Add(_defaultButton);
            _defaultButton.onClick.Invoke();
        }
    }
    [SerializeField]
    private List<Button> radioButtons = new List<Button>();
    private Dictionary<Button, List<UnityAction>> actions = new Dictionary<Button, List<UnityAction>>();

    [Space]
    [SerializeField]
    private UnityEvent OnClickOn;
    [SerializeField]
    private UnityEvent OnClickOff;

    private Button active = null;
    private bool clicking = false;

    private void Start()
    {
        foreach(Button button in radioButtons)
        {
            Add(button);
        }
        if(_defaultButton != null)
        {
            if (!actions.ContainsKey(_defaultButton))
                Add(_defaultButton);
            _defaultButton.onClick.Invoke();
        }
    }

    public void Add(Button button)
    {
        if (!radioButtons.Contains(button))
            radioButtons.Add(button);

        if (!actions.ContainsKey(button))
            actions.Add(button, new List<UnityAction>());
        else return;

        Button temp = button;

        UnityAction action = () => OnClick(temp);
        button.onClick.AddListener(action);
        actions[button].Add(action);

        AddActionSet(temp,
            () => temp.interactable = false,
            () => temp.interactable = true);

        AddActionSet(temp, 
            () => OnClickOn?.Invoke(), 
            () => OnClickOff?.Invoke());
    }

    public void Remove(Button button)
    {
        if (!radioButtons.Contains(button)) return;

        int index = radioButtons.IndexOf(button);

        button.onClick.RemoveListener(actions[button][0]);

        actions[button].RemoveAt(index);
        radioButtons.RemoveAt(index);
    }

    public void Clear()
    {
        foreach (Button button in radioButtons.ToArray())
            Remove(button);
    }

    public void AddActionSet(Button button, UnityAction OnClickOn, UnityAction OnClickOff)
    {
        if (!radioButtons.Contains(button))
            Add(button);

        actions[button].Add(OnClickOn);
        actions[button].Add(OnClickOff);
    }

    public void SelectWithoutInvoke(Button button)
    {
        if (!radioButtons.Contains(button))
            return;

        actions[active][2].Invoke();
        actions[button][1].Invoke();
    }

    private void OnClick(Button self)
    {
        if (clicking) return;
        clicking = true;

        if(active != null)
            for (int i = 2; i < actions[active].Count; i+=2)
            {
                actions[active][i]?.Invoke();
            }

        for (int i = 1; i < actions[self].Count; i += 2)
        {
            actions[self][i]?.Invoke();
        }

        active = self;
        clicking = false;
    }
}

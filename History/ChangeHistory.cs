using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using Object = UnityEngine.Object;

public class ChangeHistory : MonoBehaviour
{
    private static Stack<Change> past = new Stack<Change>();
    private static Stack<Change> future = new Stack<Change>();
    public class Change
    {
        public object obj;
        public Type type;
        public Action OnRevert;
        public Action Undo;
        public Action Redo;
        public List<RefelectionVariable> variables = new List<RefelectionVariable>();
        public enum ChangeType { Bad, All, Single, UserDefined, Custom }
        public ChangeType changeType = ChangeType.Bad; 
        public struct RefelectionVariable
        {
            public string variable;
            public Func<dynamic> get;
            public Action<dynamic> set;
            public dynamic value;
        }
        public Change(Action undo, Action redo)
        {
            obj = null;
            type = null;
            OnRevert = Undo = undo;
            Redo = redo;
            changeType = ChangeType.Custom;
        }
        public Change(object changed, Action OnSet = null)
        {
            obj = changed;
            type = obj.GetType();
            OnRevert = OnSet;

            foreach (PropertyInfo p in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!p.CanWrite || !p.CanRead) continue;
                try
                {
                    RefelectionVariable variable;
                    variable.variable = p.Name;
                    variable.value = p.GetValue(changed);
                    variable.get = null;
                    variable.set = null;
                    variables.Add(variable);
                }
                catch { }
            }
            changeType = ChangeType.All;
        }
        public Change(object changed, string property, Action OnSet = null)
        {
            obj = changed;
            type = obj.GetType();
            OnRevert = OnSet;

            PropertyInfo p = type.GetProperty(property);
            if (!p.CanWrite || !p.CanRead) return;
            try
            {
                RefelectionVariable variable;
                variable.variable = p.Name;
                variable.value = p.GetValue(changed);
                variable.get = null;
                variable.set = null;
                variables.Add(variable);
            }
            catch { }
            changeType = ChangeType.Single;
        }
        public Change(object changed, Func<dynamic> getter, Action<dynamic> setter, Action OnSet = null)
        {
            if (getter == null || setter == null) return;
            obj = changed;
            type = obj.GetType();
            OnRevert = OnSet;

            RefelectionVariable variable;
            variable.variable = "";
            variable.get = getter;
            variable.set = setter;
            variable.value = getter.Invoke();
            variables.Add(variable);
            changeType = ChangeType.UserDefined;
        }
        public void Revert()
        {
            OnRevert?.Invoke();
            foreach (RefelectionVariable v in variables)
            {
                if (v.set != null)
                    v.set.Invoke(v.value);
                else type.GetProperty(v.variable).SetValue(obj, v.value);
            }
        }

        public bool Valid()
        {
            bool valid = true;//false;
            //foreach (RefelectionVariable v in variables)
            //{
            //    Debug.Log($"{v.variable}: {type.GetProperty(v.variable).GetValue(obj)} == {v.value}");
            //    if (!type.GetProperty(v.variable).GetValue(obj).Equals(v.value))
            //    {
            //        valid = true;
            //        break;
            //    }
            //}
            return valid;
        }

        public override bool Equals(object obj)
        {
            Change c = obj as Change;
            if(c == null)
                return base.Equals(obj);

            if (c.variables.Count != variables.Count)
                return false;

            bool allMatch = changeType == ChangeType.Custom ? false : true;
            for (int i = 0; i < variables.Count; i++)
                if (!c.variables[i].variable.Equals(variables[i].variable)
                    || !c.variables[i].value.Equals(variables[i].value))
                {
                    allMatch = false;
                    break;
                }
            return allMatch;
        }
    }

    void Update()
    {
        if (/*(Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) &&*/ Input.GetKeyDown(KeyCode.Z)) Undo();
        if (/*(Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && */Input.GetKeyDown(KeyCode.Y)) Redo();
    }

    public static void LogState(Action undo, Action redo)
    {
        Change c = new Change(undo, redo);
        if (!c.Valid() || past.Count > 0 && past.Peek().Equals(c)) return;
        future.Clear();
        past.Push(c);
    }

    public static void LogState(object changed, Action OnSet = null)
    {
        Change c = new Change(changed, OnSet);
        if (!c.Valid() || past.Count > 0 && past.Peek().Equals(c)) return;
        future.Clear();
        past.Push(c);
    }

    public static void LogState(object changed, string property, Action OnSet = null)
    {
        Change c = new Change(changed, property, OnSet);
        if (!c.Valid() || past.Count > 0 && past.Peek().Equals(c)) return;
        future.Clear();
        past.Push(c);
    }

    public static void LogState(object changed, Func<dynamic> getter, Action<dynamic> setter, Action OnSet = null)
    {
        Change c = new Change(changed, getter, setter, OnSet);
        if (!c.Valid() || past.Count > 0 && past.Peek().Equals(c)) return;
        future.Clear();
        past.Push(c);
    }

    private void Undo()
    {
        if (past.Count == 0) return;
        Change c = past.Pop();
        //Debug.Log(c.changeType);
        if (c.changeType == Change.ChangeType.Bad) Undo();
        switch (c.changeType)
        {
            case Change.ChangeType.All:
                future.Push(new Change(c.obj, c.OnRevert));
                break;
            case Change.ChangeType.Single:
                future.Push(new Change(c.obj, c.variables[0].variable, c.OnRevert));
                break;
            case Change.ChangeType.UserDefined:
                future.Push(new Change(c.obj, c.variables[0].get, c.variables[0].set, c.OnRevert));
                break;
            case Change.ChangeType.Custom:
                future.Push(new Change(c.Redo, c.Undo));
                break;
        }

        ActionLog.Log($"Undo");
        Debug.Log($"Undo");

        c.Revert();
    }

    private void Redo()
    {
        if (future.Count == 0) return;
        Change c = future.Pop();
        if (c.changeType == Change.ChangeType.Bad) Redo();
        switch (c.changeType)
        {
            case Change.ChangeType.All:
                past.Push(new Change(c.obj, c.OnRevert));
                break;
            case Change.ChangeType.Single:
                past.Push(new Change(c.obj, c.variables[0].variable, c.OnRevert));
                break;
            case Change.ChangeType.UserDefined:
                past.Push(new Change(c.obj, c.variables[0].get, c.variables[0].set, c.OnRevert));
                break;
            case Change.ChangeType.Custom:
                past.Push(new Change(c.Redo, c.Undo));
                break;
        }

        ActionLog.Log($"Redo");
        Debug.Log($"Redo");

        c.Revert();
    }
}

public static class ChangeHistoryExtensions
{
    public static void LogState(this object c, Action OnSet = null)
    {
        ChangeHistory.LogState(c, OnSet);
    }

    public static void LogState(this object c, string property, Action OnSet = null)
    {
        ChangeHistory.LogState(c, property, OnSet);
    }

    public static void LogState(this object c, Func<dynamic> getter, Action<dynamic> setter, Action OnSet = null)
    {
        ChangeHistory.LogState(c, getter, setter, OnSet);
    }
}
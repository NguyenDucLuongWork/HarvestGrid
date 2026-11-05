using System;
using UnityEngine;

namespace UIGen
{
    public enum ControlType { Auto, Label, TextField, Slider, Toggle, ProgressBar }

    // Đánh dấu field nào hiện lên UI và hiện bằng control gì
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class UIFieldAttribute : Attribute
    {
        public string Label;                 // optional override; default = nicified field name
        public ControlType Type;             // optional; default = guessed from C# type
        public float Min = 0, Max = 100;
        public UIFieldAttribute() { }
        public UIFieldAttribute(ControlType type) { Type = type; }
    }

    // Ẩn field khỏi UI (dùng bên trong nhóm nested, nơi mặc định hiện hết field public)
    [AttributeUsage(AttributeTargets.Field)]
    public class UIIgnoreAttribute : Attribute { }

    // Đánh dấu method sẽ gắn vào button "btn-<Name>"
    [AttributeUsage(AttributeTargets.Method)]
    public class UIActionAttribute : Attribute
    {
        public string Name;
        public UIActionAttribute(string name) { Name = name; }
    }
}
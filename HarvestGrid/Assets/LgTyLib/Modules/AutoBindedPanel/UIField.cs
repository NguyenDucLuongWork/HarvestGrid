using System;

namespace LgTyLib.Modules.AutoBindedPanel
{
    public enum ControlType { Auto, Label, TextField, Slider, Toggle, ProgressBar, Image }

    // Đánh dấu field nào hiện lên UI và hiện bằng control gì
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class UIFieldAttribute : Attribute
    {
        public string Label;
        public ControlType Type;
        public float Min = 0, Max = 100;
        public UIFieldAttribute() { }
        public UIFieldAttribute(ControlType type) { Type = type; }
    }

    // [UIField.Sync(nameof(PlayerView.name))] on a ViewSO field
    public static class UIField
    {
        [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
        public sealed class SyncAttribute : UIFieldAttribute   // inherits Label/Type/Min/Max
        {
            public string SourceMember { get; }
            public string Format { get; set; }
            public Type Converter { get; set; }

            public SyncAttribute(params string[] path) => SourceMember = string.Join(".", path);
        }


        [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
        public sealed class ListAttribute : Attribute
        {
            public string Template;
            public string ListName;
            public Type View;          // optional: a BindedView subclass for rows with logic
        }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class UIIgnoreAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Method)]
    public class UIActionAttribute : Attribute
    {
        public string Name;
        public UIActionAttribute(string name) { Name = name; }
    }

}
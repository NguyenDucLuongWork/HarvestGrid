using System;

namespace LgTyLib.Modules.AutoBindedPanel
{
    public interface IUIConverter
    {
        object Convert(object sourceValue, Type targetType);
    }

    // Typed base so converters are easier to write
    public abstract class UIConverter<TIn, TOut> : IUIConverter
    {
        public object Convert(object sourceValue, Type targetType) => Convert((TIn)sourceValue);
        protected abstract TOut Convert(TIn value);
    }
}
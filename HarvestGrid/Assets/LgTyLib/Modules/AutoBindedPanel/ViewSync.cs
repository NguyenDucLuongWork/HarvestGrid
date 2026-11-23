using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace LgTyLib.Modules.AutoBindedPanel
{
    public static class ViewSync
    {
        const BindingFlags F = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        static readonly Dictionary<Type, IUIConverter> converterCache = new();

        // view: ViewSO or any plain class with [UIField.Sync] fields. data: the source object.
        public static void Sync(object view, object data, UnityEngine.Object ctx = null)
        {
            if (data == null) return;

            foreach (var target in view.GetType().GetFields(F))
            {
                var attr = target.GetCustomAttribute<UIField.SyncAttribute>();
                if (attr == null) continue;
                try
                {
                    if (!TryRead(view, data, attr.SourceMember, out var value)) continue;
                    target.SetValue(view, Resolve(target.GetValue(view), value, target.FieldType, attr));
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Sync '{target.Name}' failed: {e.Message}", ctx);
                }
            }
        }

        static object Resolve(object existing, object value, Type targetType, UIField.SyncAttribute attr)
        {
            if (value == null) return null;

            if (attr.Converter != null)
            {
                if (!converterCache.TryGetValue(attr.Converter, out var conv))
                    converterCache[attr.Converter] = conv = (IUIConverter)Activator.CreateInstance(attr.Converter);
                return conv.Convert(value, targetType);
            }

            // List<ItemView> <- IEnumerable<Data>
            if (value is IEnumerable src && value is not string && IsList(targetType, out var itemType)
                && !itemType.IsAssignableFrom(ElementType(value.GetType())))
                return SyncList(existing, src, itemType);

            if (targetType == typeof(string))
                return string.IsNullOrEmpty(attr.Format) ? value.ToString() : string.Format(attr.Format, value);

            if (targetType.IsAssignableFrom(value.GetType())) return value;
            return Convert.ChangeType(value, targetType);
        }

        // Reuses existing item instances so references held by bindings stay stable.
        static object SyncList(object existing, IEnumerable src, Type itemType)
        {
            var list = existing as IList
                       ?? (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(itemType));
            int i = 0;
            foreach (var element in src)
            {
                object item;
                if (i < list.Count && list[i] != null) item = list[i];
                else
                {
                    item = Activator.CreateInstance(itemType);
                    if (i < list.Count) list[i] = item; else list.Add(item);
                }
                Sync(item, element);
                i++;
            }
            while (list.Count > i) list.RemoveAt(list.Count - 1);
            return list;
        }

        static bool IsList(Type t, out Type item)
        {
            item = null;
            if (!t.IsGenericType || t.GetGenericTypeDefinition() != typeof(List<>)) return false;
            item = t.GetGenericArguments()[0];
            return true;
        }

        static Type ElementType(Type t)
        {
            if (t.IsArray) return t.GetElementType();
            foreach (var i in t.GetInterfaces())
                if (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                    return i.GetGenericArguments()[0];
            return typeof(object);
        }

        // 1) property/method on the view, 2) field/property path on data
        static bool TryRead(object view, object data, string member, out object value)
        {
            var type = view.GetType();
            var prop = type.GetProperty(member, F);
            if (prop != null) { value = prop.GetValue(view); return true; }
            var method = type.GetMethod(member, F, null, Type.EmptyTypes, null);
            if (method != null) { value = method.Invoke(view, null); return true; }

            value = data;
            foreach (var part in member.Split('.'))
            {
                if (value == null) return false;
                var vt = value.GetType();
                var f = vt.GetField(part, F);
                if (f != null) { value = f.GetValue(value); continue; }
                var p = vt.GetProperty(part, F);   // new: properties in paths
                if (p != null) { value = p.GetValue(value); continue; }
                Debug.LogWarning($"Member '{part}' not found in {vt.Name} (path: {member})");
                return false;
            }
            return true;
        }
    }
}
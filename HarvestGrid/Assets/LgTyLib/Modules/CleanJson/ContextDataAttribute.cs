using System;

namespace GameJson
{
    /// <summary>
    /// Đánh dấu field mà AI cần thấy. CleanJson chỉ giữ các field có dấu này.
    ///
    /// Quy tắc (áp dụng cho từng class, đệ quy):
    ///   - Class có ít nhất một field [ContextData]  -> chỉ giữ field có dấu (+ field "id").
    ///   - Class không có dấu nào                    -> giữ tất cả field (Vector3, struct nhỏ... không phải đánh dấu).
    /// Đánh dấu một field class nghĩa là "lấy field này"; bên trong nó lại do chính class đó quyết định.
    ///
    ///   [Serializable] public class Plant : StateEntity {
    ///       public string plantName;                  // không dấu: AI không thấy
    ///       [ContextData] public int stageIndex;
    ///       [ContextData] public string cropID;
    ///   }
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
    public sealed class ContextDataAttribute : Attribute { }
}
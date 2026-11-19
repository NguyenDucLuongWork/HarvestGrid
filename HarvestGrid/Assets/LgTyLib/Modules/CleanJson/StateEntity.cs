using System;

namespace GameJson
{
    /// <summary>
    /// Class cơ sở cho mọi dữ liệu game mà AI cần nhắc tới (cây, ô đất, chó, quốc gia...).
    /// Chỉ làm một việc: bảo đảm có field "id" và tự cấp id ổn định, dễ đọc, có kèm tên loại.
    /// CleanJson dùng field này để thay {"rid":N} bằng id.
    ///
    ///   [Serializable] public class Dog : StateEntity { public string name; }
    ///   [SerializeReference] public Dog dog;      // reference thật, không cần id thủ công
    ///
    /// Id có dạng "dog_7f3a2b1c9d4e": tên loại + 12 ký tự hex ngẫu nhiên (48 bit; 100.000 entity cùng loại vẫn
    /// chỉ có xác suất trùng khoảng 2 phần 100.000). Vì có tên loại nên id không trùng giữa các loại, và LLM nhìn vào
    /// là biết nó là gì. Với entity số lượng lớn (tỉnh, ô đất...) nên tự đặt id có nghĩa: base("province_102").
    /// </summary>
    [Serializable]
    public abstract class StateEntity
    {
        /// <summary>Tên field trong JSON. JsonUtility dùng đúng tên field C#, nên không đổi tên field "id" bên dưới.</summary>
        public const string IdFieldName = "id";

        public string id;   // public để JsonUtility chắc chắn serialize; đừng sửa tay sau khi tạo.

        protected StateEntity() { id = NewId(GetType()); }

        protected StateEntity(string customId)
        {
            id = string.IsNullOrEmpty(customId) ? NewId(GetType()) : customId;
        }

        static string NewId(Type type)
        {
            return type.Name.ToLowerInvariant() + "_" + Guid.NewGuid().ToString("N").Substring(0, 12);
        }
    }
}
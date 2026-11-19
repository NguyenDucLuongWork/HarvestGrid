using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace GameJson
{
    /// <summary>
    /// Đổi JSON từ JsonUtility (có [SerializeReference]: {"rid":N} + khối "references") thành JSON sạch cho AI đọc:
    ///   - {"rid":N}  ->  giá trị field "id" của object được trỏ tới ("dog_001")
    ///   - bỏ khối "references"
    /// Không cần attribute hay schema. Entity kế thừa StateEntity (có field "id"); với JSON từ nơi khác có thể đổi Options.IdField.
    /// Có kiểu gốc (Simplify(json, typeof(GameData)) hoặc SimplifyObject) thì chỉ giữ các field đánh dấu [ContextData].
    /// Tuỳ chọn dọn nhiễu cho AI (Options.ForAI()): bỏ instanceID, Dictionary -> object, bỏ giá trị rỗng/object chưa gán,
    /// làm tròn số, đổi enum số thành tên, gom dữ liệu lặp. Mặc định tắt hết để JSON giữ nguyên.
    /// Một file (cùng StateEntity.cs), không phụ thuộc thư viện nào. Dùng được ở mọi project.
    /// </summary>
    public static class CleanJson
    {
        public sealed class Options
        {
            public string IdField = StateEntity.IdFieldName;
            /// <summary>true: tham chiếu viết "Dog#dog_001" thay vì "dog_001" (hữu ích khi id chỉ là số, dễ trùng giữa các loại).</summary>
            public bool TypePrefix = false;
            /// <summary>false = gọn, tiết kiệm token. true = dễ đọc.</summary>
            public bool Indent = true;
            /// <summary>Tên mục chứa các object chỉ được trỏ tới mà không nằm ở đâu khác trong dữ liệu.</summary>
            public string OrphanKey = "_refs";
            /// <summary>Điền cảnh báo (rid không tìm thấy...) nếu khác null.</summary>
            public List<string> Warnings;

            // ── Dọn nhiễu cho AI (mặc định TẮT; dùng Options.ForAI() để bật hết) ──

            /// <summary>Bỏ mọi {"instanceID":N} (sprite, icon, gameObject...): số này vô nghĩa với AI và đổi mỗi phiên chạy.</summary>
            public bool DropUnityObjectRefs;
            /// <summary>Đổi Dictionary dạng [{"key":..,"value":..}] thành {"key": value}.</summary>
            public bool PairsToObject;
            /// <summary>Bỏ giá trị rỗng ("", null, []) và các object "chưa gán" (id/tên rỗng, mọi số = 0). Vị trí trong mảng được giữ bằng null.</summary>
            public bool OmitEmpty;
            /// <summary>Làm tròn số thực tới n chữ số thập phân; -1 = giữ nguyên.</summary>
            public int Decimals = -1;
            /// <summary>&gt;0: object/mảng giống hệt nhau (dài từ n ký tự trở lên) xuất hiện nhiều lần được đưa vào SharedKey, chỗ dùng ghi "@tên".</summary>
            public int DedupeMinChars;
            public string SharedKey = "_shared";
            /// <summary>Kiểu C# của object gốc (vd. typeof(GameData)). Có kiểu thì CleanJson áp [ContextData]: chỉ giữ field được đánh dấu.
            /// SimplifyObject tự điền. Để null thì không lọc theo attribute.</summary>
            public Type RootType;
            /// <summary>Tên key bị bỏ ở mọi nơi.</summary>
            public HashSet<string> DropKeys = new HashSet<string>();
            /// <summary>Field (theo tên JSON) mà số nguyên là enum: giá trị và key của dictionary được đổi thành tên enum.</summary>
            public Dictionary<string, Type> EnumFields = new Dictionary<string, Type>();

            public Options MapEnum<T>(params string[] fields) where T : Enum
            {
                foreach (var f in fields) EnumFields[f] = typeof(T);
                return this;
            }

            /// <summary>Cấu hình khuyến nghị khi đưa cho LLM: gọn, không nhiễu, ít token.</summary>
            public static Options ForAI()
            {
                return new Options
                {
                    Indent = false,
                    DropUnityObjectRefs = true,
                    PairsToObject = true,
                    OmitEmpty = true,
                    Decimals = 2,
                    DedupeMinChars = 150
                };
            }

            internal bool NeedsTidy
            {
                get
                {
                    return DropUnityObjectRefs || PairsToObject || OmitEmpty || Decimals >= 0 || DedupeMinChars > 0
                             || DropKeys.Count > 0 || EnumFields.Count > 0;
                }
            }
        }

#if UNITY_5_3_OR_NEWER
        public static string SimplifyObject(object obj, Options options = null)
        {
            var opt = options ?? new Options();
            if (opt.RootType == null) opt.RootType = obj.GetType();
            return Simplify(UnityEngine.JsonUtility.ToJson(obj), opt);
        }
#endif

        /// <summary>Như Simplify(json) nhưng biết kiểu gốc, nên áp [ContextData].</summary>
        public static string Simplify(string unityJson, Type rootType, Options options = null)
        {
            var opt = options ?? new Options();
            opt.RootType = rootType;
            return Simplify(unityJson, opt);
        }

        public static string Simplify(string unityJson, Options options = null)
        {
            var opt = options ?? new Options();
            var root = Parser.Parse(unityJson) as Obj;
            if (root == null) throw new FormatException("JSON gốc phải là một object.");

            // 1. Bảng rid -> entry, lấy từ khối "references" (chỉ khi đúng dạng của Unity), rồi bỏ khối đó.
            var table = new Dictionary<long, Obj>();
            if (root.TryGet("references", out var refsNode) && refsNode is Obj refs
                && refs.TryGet("RefIds", out var listNode) && listNode is List<object> entries)
            {
                foreach (var e in entries)
                {
                    var entry = e as Obj;
                    if (entry != null && entry.TryGet("rid", out var r) && r is Num n && n.TryLong(out long rid))
                        table[rid] = entry;
                }
                root.Remove("references");
            }

            // 2. Thay {"rid":N} trong toàn bộ cây.
            var cleaner = new Cleaner(table, opt);
            var result = (Obj)cleaner.Clean(root, false, opt.RootType);
            cleaner.AddOrphans(result);
            if (opt.NeedsTidy) result = new Tidier(opt).Run(result);
            return Writer.Write(result, opt.Indent);
        }

        // ── Cây JSON giữ thứ tự key, số giữ nguyên chữ gốc (không đổi độ chính xác) ──

        sealed class Num
        {
            public string Text;
            public bool TryLong(out long v) { return long.TryParse(Text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out v); }
        }

        sealed class Obj
        {
            public readonly List<string> Keys = new List<string>();
            readonly Dictionary<string, object> map = new Dictionary<string, object>();
            public int Count { get { return Keys.Count; } }
            public object this[string k] { get { return map[k]; } }
            public bool TryGet(string k, out object v) { return map.TryGetValue(k, out v); }
            public void Set(string k, object v) { if (!map.ContainsKey(k)) Keys.Add(k); map[k] = v; }
            public void Remove(string k) { if (map.Remove(k)) Keys.Remove(k); }
        }

        sealed class Cleaner
        {
            readonly Dictionary<long, Obj> table;
            readonly Options opt;
            readonly HashSet<long> expanded = new HashSet<long>();
            readonly HashSet<long> inProgress = new HashSet<long>();
            readonly List<long> referenced = new List<long>();
            readonly HashSet<long> referencedSet = new HashSet<long>();

            readonly Dictionary<Type, TInfo> infos = new Dictionary<Type, TInfo>();
            readonly Dictionary<long, Type> refTypes = new Dictionary<long, Type>();

            public Cleaner(Dictionary<long, Obj> table, Options opt) { this.table = table; this.opt = opt; }

            sealed class TInfo
            {
                public bool HasMarks;
                public readonly Dictionary<string, FieldInfo> Fields = new Dictionary<string, FieldInfo>();
            }

            TInfo Info(Type t)
            {
                TInfo i;
                if (infos.TryGetValue(t, out i)) return i;
                i = new TInfo();
                const BindingFlags F = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
                for (var c = t; c != null && c != typeof(object); c = c.BaseType)
                    foreach (var f in c.GetFields(F))
                    {
                        if (!i.Fields.ContainsKey(f.Name)) i.Fields[f.Name] = f;
                        if (f.IsDefined(typeof(ContextDataAttribute), false)) i.HasMarks = true;
                    }
                infos[t] = i;
                return i;
            }

            /// <summary>Field "key" của JSON có được giữ không, và kiểu khai báo của nó là gì.</summary>
            bool Include(Type t, string key, out Type fieldType)
            {
                fieldType = null;
                if (t == null) return true;
                var info = Info(t);
                FieldInfo f;
                info.Fields.TryGetValue(key, out f);
                if (f != null) fieldType = f.FieldType;
                if (!info.HasMarks) return true;
                if (key == opt.IdField) return true;
                return f != null && f.IsDefined(typeof(ContextDataAttribute), false);
            }

            static bool IsDictionary(Type t, out Type k, out Type v)
            {
                k = v = null;
                if (t == null || !t.IsGenericType || t.GetGenericTypeDefinition() != typeof(Dictionary<,>)) return false;
                var a = t.GetGenericArguments();
                k = a[0]; v = a[1];
                return true;
            }

            static Type ElementTypeOf(Type t)
            {
                if (t == null) return null;
                if (t.IsArray) return t.GetElementType();
                if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IList<>)) return t.GetGenericArguments()[0];
                foreach (var i in t.GetInterfaces())
                    if (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IList<>)) return i.GetGenericArguments()[0];
                return null;
            }

            /// <summary>Kiểu thật của object được tham chiếu, lấy từ khối references (class/ns/asm). null nếu không tìm được.</summary>
            static Type ResolveType(Obj entry)
            {
                object t;
                var to = entry.TryGet("type", out t) ? t as Obj : null;
                if (to == null) return null;
                string cls = Str(to, "class"), ns = Str(to, "ns"), asm = Str(to, "asm");
                if (string.IsNullOrEmpty(cls)) return null;
                string full = (string.IsNullOrEmpty(ns) ? "" : ns + ".") + cls.Replace('/', '+');
                try
                {
                    if (!string.IsNullOrEmpty(asm))
                    {
                        var ty = Assembly.Load(asm).GetType(full, false);
                        if (ty != null) return ty;
                    }
                    return Type.GetType(full, false);
                }
                catch { return null; }
            }

            static string Str(Obj o, string key)
            {
                object v;
                return o.TryGet(key, out v) ? v as string : null;
            }

            public object Clean(object node, bool inArray, Type type)
            {
                var o = node as Obj;
                if (o != null)
                {
                    long rid;
                    if (IsRef(o, out rid)) return Resolve(o, rid, inArray, type);
                    var res = new Obj();
                    foreach (var k in o.Keys)
                    {
                        Type ft;
                        if (!Include(type, k, out ft)) continue;       // field không có [ContextData]
                        res.Set(k, Clean(o[k], false, ft));
                    }
                    return res;
                }
                var list = node as List<object>;
                if (list != null)
                {
                    Type kt, vt;
                    bool isDict = IsDictionary(type, out kt, out vt);
                    Type et = isDict ? null : ElementTypeOf(type);
                    var res = new List<object>(list.Count);
                    foreach (var e in list)
                    {
                        var p = e as Obj;
                        object k, v;
                        if (isDict && p != null && p.Count == 2 && p.TryGet("key", out k) && p.TryGet("value", out v))
                        {
                            var np = new Obj();
                            np.Set("key", Clean(k, false, kt));
                            np.Set("value", Clean(v, false, vt));
                            res.Add(np);
                        }
                        else res.Add(Clean(e, true, et));
                    }
                    return res;
                }
                return node;
            }

            static bool IsRef(Obj o, out long rid)
            {
                rid = 0;
                object v;
                return o.Count == 1 && o.TryGet("rid", out v) && v is Num && ((Num)v).TryLong(out rid);
            }

            object Resolve(Obj original, long rid, bool inArray, Type declared)
            {
                if (rid < 0) return null;                                  // reference null của Unity
                Obj entry; object d;
                if (!table.TryGetValue(rid, out entry) || !entry.TryGet("data", out d) || !(d is Obj))
                {
                    if (opt.Warnings != null) opt.Warnings.Add("rid " + rid + " không có trong khối references; giữ nguyên.");
                    return original;                                       // có thể chỉ là field tên "rid" của game
                }
                var data = (Obj)d;
                if (referencedSet.Add(rid)) referenced.Add(rid);
                Type actual = ResolveType(entry) ?? declared;               // kiểu thật (class con) quyết định field nào được giữ
                if (!refTypes.ContainsKey(rid)) refTypes[rid] = actual;

                object id = IdOf(data);
                bool owner;
                if (id == null) owner = true;                              // không có id thì chỉ còn cách viết thẳng nội dung
                else owner = inArray && expanded.Add(rid);                 // phần tử mảng đầu tiên = nơi "sở hữu", viết đủ nội dung
                if (!owner) return FormatRef(entry, id);

                if (id == null)
                {
                    if (!inProgress.Add(rid)) return "<cycle " + TypeName(entry) + "#" + rid + ">";
                    var inline = Clean(data, false, actual);
                    inProgress.Remove(rid);
                    return inline;
                }
                return Clean(data, false, actual);
            }

            object IdOf(Obj data)
            {
                object v;
                if (!data.TryGet(opt.IdField, out v)) return null;
                var s = v as string;
                if (s != null) return s.Length > 0 ? v : null;
                return v is Num ? v : null;
            }

            object FormatRef(Obj entry, object id)
            {
                if (!opt.TypePrefix) return id;
                return TypeName(entry) + "#" + (id is Num ? ((Num)id).Text : (string)id);
            }

            static string TypeName(Obj entry)
            {
                object t, c;
                if (entry.TryGet("type", out t) && t is Obj && ((Obj)t).TryGet("class", out c) && c is string) return (string)c;
                return "?";
            }

            /// <summary>Object chỉ được trỏ tới mà chưa xuất hiện ở đâu khác (vd. field duy nhất giữ nó) sẽ được đưa vào mục
            /// OrphanKey để id không bị "treo". Nếu dữ liệu đã có object cùng id (ví dụ nằm trong list by-value) thì bỏ qua.</summary>
            public void AddOrphans(Obj root)
            {
                var orphans = new List<object>();
                int done = 0;
                while (true)
                {
                    var ids = new HashSet<string>();
                    CollectIds(root, ids);
                    CollectIds(orphans, ids);

                    bool added = false;
                    for (; done < referenced.Count; done++)
                    {
                        long rid = referenced[done];
                        if (expanded.Contains(rid)) continue;
                        var data = (Obj)table[rid]["data"];
                        object id = IdOf(data);
                        if (id == null) continue;
                        string key = id is Num ? ((Num)id).Text : (string)id;
                        if (ids.Contains(key)) continue;
                        expanded.Add(rid);
                        Type rt;
                        refTypes.TryGetValue(rid, out rt);
                        var cleaned = (Obj)Clean(data, false, rt);
                        // thêm "type" để biết object này là gì khi nó không nằm trong collection nào
                        var wrapped = new Obj();
                        wrapped.Set("_type", TypeName(table[rid]));
                        foreach (var k in cleaned.Keys) wrapped.Set(k, cleaned[k]);
                        orphans.Add(wrapped);
                        ids.Add(key);
                        added = true;
                    }
                    if (!added) break;
                }
                if (orphans.Count > 0) root.Set(opt.OrphanKey, orphans);
            }

            void CollectIds(object node, HashSet<string> ids)
            {
                var o = node as Obj;
                if (o != null)
                {
                    object v;
                    if (o.TryGet(opt.IdField, out v))
                    {
                        if (v is string) ids.Add((string)v);
                        else if (v is Num) ids.Add(((Num)v).Text);
                    }
                    foreach (var k in o.Keys) CollectIds(o[k], ids);
                    return;
                }
                var l = node as List<object>;
                if (l != null) foreach (var e in l) CollectIds(e, ids);
            }
        }

        // ── Dọn nhiễu: instanceID, dictionary, giá trị rỗng, số thực, enum, trùng lặp ──

        sealed class Tidier
        {
            static readonly object Removed = new object();
            readonly Options opt;
            public Tidier(Options opt) { this.opt = opt; }

            public Obj Run(Obj root)
            {
                var t = Tidy(root, null) as Obj ?? new Obj();
                if (opt.DedupeMinChars > 0) t = new Deduper(opt).Run(t);
                return t;
            }

            object Tidy(object node, string field)
            {
                var o = node as Obj;
                if (o != null) return TidyObj(o, field);
                var l = node as List<object>;
                if (l != null) return TidyList(l, field);
                return TidyScalar(node, field);
            }

            object TidyObj(Obj o, string field)
            {
                if (opt.DropUnityObjectRefs && IsUnityRef(o)) return Removed;

                var res = new Obj();
                bool marker = false;       // có thứ gì đó bị bỏ vì rỗng / là reference Unity
                bool allDefault = true;    // mọi giá trị còn lại đều là 0 / false
                foreach (var k in o.Keys)
                {
                    if (opt.DropKeys.Contains(k)) continue;
                    var v = Tidy(o[k], k);
                    if (v == Removed) { marker = true; continue; }
                    res.Set(k, v);
                    if (!IsDefaultLeaf(v)) allDefault = false;
                }
                if (opt.OmitEmpty)
                {
                    if (res.Count == 0) return Removed;
                    // Object "chưa gán": id/tên rỗng + toàn số 0. Chỉ gộp khi có dấu hiệu rỗng thật,
                    // nên một Vector3 (0,0,0) hợp lệ vẫn được giữ.
                    if (marker && allDefault) return Removed;
                }
                return res;
            }

            object TidyList(List<object> l, string field)
            {
                if (opt.PairsToObject)
                {
                    Obj map;
                    if (TryPairs(l, field, out map)) return TidyObj(map, field);
                }
                var res = new List<object>(l.Count);
                bool anyReal = false;
                foreach (var e in l)
                {
                    var v = Tidy(e, field);
                    if (v == Removed) { if (opt.OmitEmpty) res.Add(null); }   // giữ nguyên vị trí các phần tử còn lại
                    else { res.Add(v); anyReal = true; }
                }
                if (opt.OmitEmpty && !anyReal) return Removed;
                return res;
            }

            object TidyScalar(object v, string field)
            {
                if (v == null) return opt.OmitEmpty ? Removed : null;
                var s = v as string;
                if (s != null) return (opt.OmitEmpty && s.Length == 0) ? Removed : v;
                var n = v as Num;
                if (n != null)
                {
                    Type et; long l;
                    if (field != null && opt.EnumFields.TryGetValue(field, out et) && n.TryLong(out l))
                    {
                        string name = EnumName(et, l);
                        if (name != null) return name;
                    }
                    if (opt.Decimals >= 0 && n.Text.IndexOfAny(new[] { '.', 'e', 'E' }) >= 0) return Round(n);
                }
                return v;
            }

            Num Round(Num n)
            {
                double d;
                if (!double.TryParse(n.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out d)) return n;
                d = Math.Round(d, Math.Min(opt.Decimals, 15));
                string text = d.ToString("0.###############", CultureInfo.InvariantCulture);
                return new Num { Text = text == "-0" ? "0" : text };
            }

            bool TryPairs(List<object> l, string field, out Obj map)
            {
                map = null;
                if (l.Count == 0) return false;
                var seen = new HashSet<string>();
                var m = new Obj();
                Type et = null;
                if (field != null) opt.EnumFields.TryGetValue(field, out et);
                foreach (var e in l)
                {
                    var p = e as Obj;
                    object k, v;
                    if (p == null || p.Count != 2 || !p.TryGet("key", out k) || !p.TryGet("value", out v)) return false;
                    string key;
                    if (k is string) key = (string)k;
                    else if (k is bool) key = (bool)k ? "true" : "false";
                    else if (k is Num)
                    {
                        key = ((Num)k).Text;
                        long lv;
                        if (et != null && ((Num)k).TryLong(out lv)) key = EnumName(et, lv) ?? key;
                    }
                    else return false;
                    if (!seen.Add(key)) return false;                      // key trùng: không phải dictionary, giữ nguyên danh sách
                    m.Set(key, v);
                }
                map = m;
                return true;
            }

            static string EnumName(Type t, long v)
            {
                try
                {
                    var e = Enum.ToObject(t, v);
                    return Enum.IsDefined(t, e) ? e.ToString() : null;
                }
                catch { return null; }
            }

            static bool IsUnityRef(Obj o)
            {
                object v;
                return o.Count == 1 && o.TryGet("instanceID", out v) && v is Num;
            }

            /// <summary>0, false, hoặc object mà mọi giá trị bên trong đều như vậy (vd. crop còn {"baseSellPrice":0} sau khi lọc).</summary>
            static bool IsDefaultLeaf(object v)
            {
                if (v is bool) return !(bool)v;
                var o = v as Obj;
                if (o != null)
                {
                    if (o.Count == 0) return false;
                    foreach (var k in o.Keys) if (!IsDefaultLeaf(o[k])) return false;
                    return true;
                }
                var n = v as Num;
                double d;
                return n != null && double.TryParse(n.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out d) && d == 0;
            }
        }

        /// <summary>Gom các object/mảng giống hệt nhau (ví dụ danh sách stages lặp lại ở mọi cây) vào một chỗ.</summary>
        sealed class Deduper
        {
            readonly Options opt;
            readonly Dictionary<string, int> counts = new Dictionary<string, int>();
            readonly Dictionary<string, string> names = new Dictionary<string, string>();
            readonly Dictionary<string, int> seq = new Dictionary<string, int>();
            readonly Obj shared = new Obj();

            public Deduper(Options opt) { this.opt = opt; }

            public Obj Run(Obj root)
            {
                Count(root, true);
                var res = (Obj)RewriteChildren(root, null);
                if (shared.Count > 0) res.Set(opt.SharedKey, shared);
                return res;
            }

            static bool IsContainer(object n) { return n is Obj || n is List<object>; }

            // Chỉ đi sâu vào lần xuất hiện đầu tiên, để bản sao thứ hai không làm đếm lố các phần con.
            void Count(object node, bool isRoot)
            {
                if (!IsContainer(node)) return;
                if (!isRoot)
                {
                    string s = Writer.Write(node, false);
                    if (s.Length >= opt.DedupeMinChars)
                    {
                        int c;
                        counts.TryGetValue(s, out c);
                        counts[s] = c + 1;
                        if (c > 0) return;
                    }
                }
                var o = node as Obj;
                if (o != null) foreach (var k in o.Keys) Count(o[k], false);
                else foreach (var e in (List<object>)node) Count(e, false);
            }

            object Rewrite(object node, string field)
            {
                if (!IsContainer(node)) return node;
                string s = Writer.Write(node, false);
                int c;
                if (counts.TryGetValue(s, out c) && c >= 2)
                {
                    string name;
                    if (!names.TryGetValue(s, out name))
                    {
                        string f = field ?? "item";
                        int n;
                        seq.TryGetValue(f, out n);
                        seq[f] = ++n;
                        name = f + "_" + n;
                        names[s] = name;
                        shared.Set(name, null);                            // giữ chỗ để thứ tự ổn định
                        shared.Set(name, RewriteChildren(node, field));
                    }
                    return "@" + name;
                }
                return RewriteChildren(node, field);
            }

            object RewriteChildren(object node, string field)
            {
                var o = node as Obj;
                if (o != null)
                {
                    var res = new Obj();
                    foreach (var k in o.Keys) res.Set(k, Rewrite(o[k], k));
                    return res;
                }
                var l = (List<object>)node;
                var rl = new List<object>(l.Count);
                foreach (var e in l) rl.Add(Rewrite(e, field));
                return rl;
            }
        }

        // ── Parser / Writer ──

        static class Parser
        {
            public static object Parse(string t)
            {
                int i = 0;
                var v = Value(t, ref i);
                Ws(t, ref i);
                if (i != t.Length) throw new FormatException("Ký tự thừa ở vị trí " + i);
                return v;
            }

            static void Ws(string t, ref int i) { while (i < t.Length && char.IsWhiteSpace(t[i])) i++; }

            static object Value(string t, ref int i)
            {
                Ws(t, ref i);
                if (i >= t.Length) throw new FormatException("JSON kết thúc sớm.");
                char c = t[i];
                if (c == '{')
                {
                    i++;
                    var o = new Obj();
                    Ws(t, ref i);
                    if (t[i] == '}') { i++; return o; }
                    while (true)
                    {
                        Ws(t, ref i);
                        string k = Str(t, ref i);
                        Ws(t, ref i);
                        if (t[i++] != ':') throw new FormatException("Thiếu ':' ở " + i);
                        o.Set(k, Value(t, ref i));
                        Ws(t, ref i);
                        if (t[i] == ',') { i++; continue; }
                        if (t[i] == '}') { i++; return o; }
                        throw new FormatException("Cần ',' hoặc '}' ở " + i);
                    }
                }
                if (c == '[')
                {
                    i++;
                    var l = new List<object>();
                    Ws(t, ref i);
                    if (t[i] == ']') { i++; return l; }
                    while (true)
                    {
                        l.Add(Value(t, ref i));
                        Ws(t, ref i);
                        if (t[i] == ',') { i++; continue; }
                        if (t[i] == ']') { i++; return l; }
                        throw new FormatException("Cần ',' hoặc ']' ở " + i);
                    }
                }
                if (c == '"') return Str(t, ref i);
                if (Word(t, ref i, "true")) return true;
                if (Word(t, ref i, "false")) return false;
                if (Word(t, ref i, "null")) return null;
                int s = i;
                while (i < t.Length && "+-0123456789.eE".IndexOf(t[i]) >= 0) i++;
                if (s == i) throw new FormatException("Ký tự lạ '" + c + "' ở " + i);
                return new Num { Text = t.Substring(s, i - s) };
            }

            static bool Word(string t, ref int i, string w)
            {
                if (string.CompareOrdinal(t, i, w, 0, w.Length) != 0) return false;
                i += w.Length;
                return true;
            }

            static string Str(string t, ref int i)
            {
                if (t[i] != '"') throw new FormatException("Cần '\"' ở " + i);
                i++;
                var sb = new StringBuilder();
                while (true)
                {
                    if (i >= t.Length) throw new FormatException("String không đóng.");
                    char c = t[i++];
                    if (c == '"') return sb.ToString();
                    if (c != '\\') { sb.Append(c); continue; }
                    char e = t[i++];
                    switch (e)
                    {
                        case '"': sb.Append('"'); break;
                        case '\\': sb.Append('\\'); break;
                        case '/': sb.Append('/'); break;
                        case 'b': sb.Append('\b'); break;
                        case 'f': sb.Append('\f'); break;
                        case 'n': sb.Append('\n'); break;
                        case 'r': sb.Append('\r'); break;
                        case 't': sb.Append('\t'); break;
                        case 'u': sb.Append((char)System.Convert.ToInt32(t.Substring(i, 4), 16)); i += 4; break;
                        default: throw new FormatException("Escape lạ '\\" + e + "'");
                    }
                }
            }
        }

        static class Writer
        {
            public static string Write(object v, bool indent)
            {
                var sb = new StringBuilder();
                W(sb, v, indent, 0);
                return sb.ToString();
            }

            static void NewLine(StringBuilder sb, bool indent, int level)
            {
                if (!indent) return;
                sb.Append('\n').Append(' ', level * 2);
            }

            static void W(StringBuilder sb, object v, bool indent, int level)
            {
                if (v == null) { sb.Append("null"); return; }
                if (v is string) { Str(sb, (string)v); return; }
                if (v is bool) { sb.Append((bool)v ? "true" : "false"); return; }
                if (v is Num) { sb.Append(((Num)v).Text); return; }
                var o = v as Obj;
                if (o != null)
                {
                    if (o.Count == 0) { sb.Append("{}"); return; }
                    sb.Append('{');
                    for (int k = 0; k < o.Keys.Count; k++)
                    {
                        if (k > 0) sb.Append(',');
                        NewLine(sb, indent, level + 1);
                        Str(sb, o.Keys[k]);
                        sb.Append(indent ? ": " : ":");
                        W(sb, o[o.Keys[k]], indent, level + 1);
                    }
                    NewLine(sb, indent, level);
                    sb.Append('}');
                    return;
                }
                var l = v as List<object>;
                if (l != null)
                {
                    if (l.Count == 0) { sb.Append("[]"); return; }
                    sb.Append('[');
                    for (int k = 0; k < l.Count; k++)
                    {
                        if (k > 0) sb.Append(',');
                        NewLine(sb, indent, level + 1);
                        W(sb, l[k], indent, level + 1);
                    }
                    NewLine(sb, indent, level);
                    sb.Append(']');
                    return;
                }
                Str(sb, v.ToString());
            }

            static void Str(StringBuilder sb, string s)
            {
                sb.Append('"');
                foreach (char c in s)
                {
                    switch (c)
                    {
                        case '"': sb.Append("\\\""); break;
                        case '\\': sb.Append("\\\\"); break;
                        case '\n': sb.Append("\\n"); break;
                        case '\r': sb.Append("\\r"); break;
                        case '\t': sb.Append("\\t"); break;
                        default:
                            if (c < 0x20) sb.Append("\\u").Append(((int)c).ToString("x4"));
                            else sb.Append(c);
                            break;
                    }
                }
                sb.Append('"');
            }
        }
    }
}
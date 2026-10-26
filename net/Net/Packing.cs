using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Reflection;
using System.Text;

namespace Stillwake.Net;

/// <summary>
/// Plain records as compact bytes for the wire (2026-10-26-compact-wire): every value by its declared type, no names,
/// so both ends must run one build. A record is its public constructor's parameters in order, then its other settable
/// properties by name; a reference value carries whether it is there. Hashes and saves stay JSON.
/// </summary>
public static class Packing
{
    /// <summary>The concrete types an abstract base is sent as, each written as its index here, then its fields.</summary>
    public sealed class Union
    {
        private readonly Type[] _cases;
        private readonly Dictionary<Type, int> _tags = new();

        public Union(IEnumerable<Type> cases)
        {
            _cases = cases.ToArray();
            for (int i = 0; i < _cases.Length; i++)
            {
                _tags.Add(_cases[i], i);
                For(_cases[i]);
            }
        }

        internal int TagOf(Type type) =>
            _tags.TryGetValue(type, out int tag) ? tag : throw new ArgumentException($"{type.Name} is none of the union's types.");

        internal Type CaseOf(uint tag) =>
            tag < _cases.Length ? _cases[tag] : throw new InvalidDataException($"No type has tag {tag}.");

        /// <summary>
        /// A short hash of how every case is laid out: its name, then each field's name and kind, in order. Two peers whose
        /// fingerprints agree read each other's bytes alike.
        /// </summary>
        public string Fingerprint()
        {
            ulong hash = 14695981039346656037;
            foreach (byte b in Encoding.UTF8.GetBytes(string.Join(";", _cases.Select(c => c.FullName + For(c).Shape))))
                hash = unchecked((hash ^ b) * 1099511628211);
            return hash.ToString("x16");
        }
    }

    public static byte[] Pack<T>(T value)
    {
        var w = new Writer();
        For(typeof(T)).Alone.Write(w, value);
        return w.ToArray();
    }

    public static byte[] Pack(object value, Union union)
    {
        var w = new Writer();
        var type = value.GetType();
        w.Varint((uint)union.TagOf(type));
        For(type).Alone.Write(w, value);
        return w.ToArray();
    }

    /// <summary>The value <see cref="Pack{T}"/> wrote; garbled or cut bytes throw <see cref="InvalidDataException"/>.</summary>
    public static T Unpack<T>(ReadOnlySpan<byte> bytes) => (T)Read(bytes, r => For(typeof(T)).Alone.Read(r))!;

    public static object Unpack(ReadOnlySpan<byte> bytes, Union union) =>
        Read(bytes, r => For(union.CaseOf(r.Varint())).Alone.Read(r))!;

    private static object? Read(ReadOnlySpan<byte> bytes, Func<Reader, object?> read)
    {
        var r = new Reader(bytes.ToArray());
        try
        {
            var value = read(r);
            if (!r.Done) throw new InvalidDataException("Bytes left over.");
            return value;
        }
        catch (Exception e) when (e is not InvalidDataException)
        {
            // A constructor refusing what it was read with, a value of the wrong kind: not from a peer of this build.
            throw new InvalidDataException("A garbled value.", e);
        }
    }

    /// <summary>
    /// How a type is written and read, its <c>Shape</c> told in a few letters; <c>Whole</c>, for a record class, without
    /// whether it is there, as one sent on its own always is.
    /// </summary>
    private sealed record Codec(string Shape, Action<Writer, object?> Write, Func<Reader, object?> Read, Codec? Whole = null)
    {
        public Codec Alone => Whole ?? this;
    }

    private static readonly ConcurrentDictionary<Type, Codec> Codecs = new();

    private static Codec For(Type type) => Codecs.TryGetValue(type, out var codec) ? codec : Codecs.GetOrAdd(type, Build(type));

    private static Codec Build(Type type)
    {
        if (type == typeof(int)) return new("i", (w, v) => w.Varint(Zig((int)v!)), r => Zag(r.Varint()));
        if (type == typeof(bool)) return new("b", (w, v) => w.Byte((bool)v! ? (byte)1 : (byte)0), r => r.Byte() switch
        {
            0 => false,
            1 => true,
            _ => throw new InvalidDataException("Not a bool."),
        });
        if (type == typeof(double)) return new("d", (w, v) => w.Fixed(BitConverter.DoubleToUInt64Bits((double)v!)), r => BitConverter.UInt64BitsToDouble(r.Fixed()));
        if (type == typeof(ulong)) return new("u", (w, v) => w.Fixed((ulong)v!), r => r.Fixed());
        if (type == typeof(string))
            return new("s", (w, v) => w.Bytes(v is string s ? Encoding.UTF8.GetBytes(s) : null), r => r.Bytes() is { } b ? Encoding.UTF8.GetString(b) : null);
        if (type == typeof(byte[])) return new("y", (w, v) => w.Bytes((byte[]?)v), r => r.Bytes());
        if (type.IsEnum)
        {
            if (Enum.GetUnderlyingType(type) != typeof(int)) throw new NotSupportedException($"{type.Name} is not an int enum.");
            return new($"e:{type.Name}", (w, v) => w.Varint(Zig((int)v!)), r =>
            {
                var value = Enum.ToObject(type, Zag(r.Varint()));
                return Enum.IsDefined(type, value) ? value : throw new InvalidDataException($"No {type.Name} {value}.");
            });
        }
        if (Nullable.GetUnderlyingType(type) is { } inner)
        {
            var codec = For(inner);
            return new("?" + codec.Shape, (w, v) =>
            {
                w.Byte(v == null ? (byte)0 : (byte)1);
                if (v != null) codec.Write(w, v);
            }, r => r.Present() ? codec.Read(r) : null);
        }
        if (ItemOf(type) is { } item) return List(item);
        if (type.IsAbstract || type.IsInterface || type.IsPrimitive || type.IsArray)
            throw new NotSupportedException($"{type.Name} has no packing: only plain records, ints, bools, doubles, strings and lists of them.");
        return Record(type);
    }

    private static Type? ItemOf(Type type)
    {
        if (!type.IsGenericType) return null;
        var open = type.GetGenericTypeDefinition();
        return open == typeof(List<>) || open == typeof(IReadOnlyList<>) ? type.GetGenericArguments()[0] : null;
    }

    private static Codec List(Type item)
    {
        var codec = For(item);
        var list = typeof(List<>).MakeGenericType(item);
        return new($"[{codec.Shape}]", (w, v) =>
        {
            if (v is not System.Collections.IList items)
            {
                w.Varint(0);
                return;
            }
            w.Varint((uint)items.Count + 1);
            foreach (var x in items) codec.Write(w, x);
        }, r =>
        {
            int count = r.Count();
            if (count < 0) return null;
            var items = (System.Collections.IList)Activator.CreateInstance(list, count)!;
            for (int i = 0; i < count; i++) items.Add(codec.Read(r));
            return items;
        });
    }

    private static Codec Record(Type type)
    {
        var ctors = type.GetConstructors().OrderByDescending(c => c.GetParameters().Length).ToArray();
        if (ctors.Length == 0) throw new NotSupportedException($"{type.Name} has no public constructor.");
        // Which of two alike would be read through would hang on reflection's order.
        if (ctors.Length > 1 && ctors[1].GetParameters().Length == ctors[0].GetParameters().Length)
            throw new NotSupportedException($"{type.Name} has two public constructors of {ctors[0].GetParameters().Length} parameters.");
        var ctor = ctors[0];
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.CanRead && p.GetIndexParameters().Length == 0).ToList();
        var given = ctor.GetParameters().Select(a =>
            properties.FirstOrDefault(p => string.Equals(p.Name, a.Name, StringComparison.OrdinalIgnoreCase) && p.PropertyType == a.ParameterType)
            ?? throw new NotSupportedException($"{type.Name}'s parameter {a.Name} is no property of it.")).ToArray();
        // Properties set after the constructor, such as a command's From and Surface; by name, a total order.
        var set = properties.Where(p => !given.Contains(p) && p.SetMethod is { IsPublic: true })
            .OrderBy(p => p.Name, StringComparer.Ordinal).ToArray();
        // State the wire cannot carry: an auto-property neither given nor publicly set would arrive at its default.
        if (properties.FirstOrDefault(p => !given.Contains(p) && !set.Contains(p) && IsAuto(p)) is { } hidden)
            throw new NotSupportedException($"{type.Name}.{hidden.Name} is neither a constructor parameter nor publicly settable.");
        var fields = given.Concat(set).Select(p => (Property: p, Codec: For(p.PropertyType))).ToArray();
        string shape = "{" + string.Join(",", fields.Select(f => f.Property.Name + ":" + f.Codec.Shape)) + "}";
        var whole = new Codec(shape, (w, v) =>
        {
            foreach (var (p, codec) in fields) codec.Write(w, p.GetValue(v));
        }, r =>
        {
            var args = new object?[given.Length];
            for (int i = 0; i < args.Length; i++) args[i] = fields[i].Codec.Read(r);
            var value = ctor.Invoke(args);
            for (int i = given.Length; i < fields.Length; i++) fields[i].Property.SetValue(value, fields[i].Codec.Read(r));
            return value;
        });
        if (type.IsValueType) return whole;
        return new("?" + shape, (w, v) =>
        {
            w.Byte(v == null ? (byte)0 : (byte)1);
            if (v != null) whole.Write(w, v);
        }, r => r.Present() ? whole.Read(r) : null, whole);
    }

    private static bool IsAuto(PropertyInfo p) =>
        p.DeclaringType!.GetField($"<{p.Name}>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance) != null;

    private static uint Zig(int n) => (uint)((n << 1) ^ (n >> 31));
    private static int Zag(uint n) => (int)(n >> 1) ^ -(int)(n & 1);

    private sealed class Writer
    {
        private byte[] _bytes = new byte[64];
        private int _count;

        public void Byte(byte b)
        {
            if (_count == _bytes.Length) Array.Resize(ref _bytes, _bytes.Length * 2);
            _bytes[_count++] = b;
        }

        public void Varint(uint n)
        {
            for (; n >= 0x80; n >>= 7) Byte((byte)(n | 0x80));
            Byte((byte)n);
        }

        public void Fixed(ulong n)
        {
            for (int i = 0; i < 8; i++) Byte((byte)(n >> (8 * i)));
        }

        /// <summary>Its length plus one first: zero is none.</summary>
        public void Bytes(byte[]? bytes)
        {
            if (bytes == null)
            {
                Varint(0);
                return;
            }
            Varint((uint)bytes.Length + 1);
            foreach (byte b in bytes) Byte(b);
        }

        public byte[] ToArray() => _bytes[.._count];
    }

    private sealed class Reader(byte[] bytes)
    {
        private int _at;

        public bool Done => _at == bytes.Length;

        public byte Byte() => _at < bytes.Length ? bytes[_at++] : throw new InvalidDataException("Cut short.");

        public bool Present() => Byte() switch
        {
            0 => false,
            1 => true,
            _ => throw new InvalidDataException("Neither there nor missing."),
        };

        public uint Varint()
        {
            uint n = 0;
            for (int shift = 0; shift < 35; shift += 7)
            {
                byte b = Byte();
                // A fifth byte holds only the top four bits.
                if (shift == 28 && b > 0x0F) throw new InvalidDataException("A number too big.");
                n |= (uint)(b & 0x7F) << shift;
                if (b < 0x80) return n;
            }
            throw new InvalidDataException("A number too long.");
        }

        public ulong Fixed() => BinaryPrimitives.ReadUInt64LittleEndian(Take(8));

        /// <summary>A count written as itself plus one, so zero is none (-1); never more than the bytes left.</summary>
        public int Count()
        {
            uint n = Varint();
            if (n > bytes.Length - _at + 1) throw new InvalidDataException("More than the bytes left.");
            return (int)n - 1;
        }

        public byte[]? Bytes() => Count() is var n and >= 0 ? Take(n).ToArray() : null;

        private ReadOnlySpan<byte> Take(int n)
        {
            if (n > bytes.Length - _at) throw new InvalidDataException("Cut short.");
            var span = bytes.AsSpan(_at, n);
            _at += n;
            return span;
        }
    }
}

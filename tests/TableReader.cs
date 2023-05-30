namespace NetCash.Tests;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;


/// <summary>
///   Data reader for Org-mode tables.
/// </summary>
public static class TableReader
{
    static IEnumerable<string[]> ReadAsTuples(string table) =>
        NetCash.Extensions.SystemExtensions
            .Lines(table)
            .Skip(2)
            .Where(line => !(line.Contains("-+-") || string.IsNullOrWhiteSpace(line)))
            .Select(line => line.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

    public static T TupleToObject<T>(string[] tuple)
    {
        var fields =
                from mem in typeof(T).GetFields()
                    // I'm too lazy to define a custom attribute, just reusing DataMember seems to make sense.
                let attr = mem.GetCustomAttribute<DataMemberAttribute>()
                where attr != null
                orderby attr.Order ascending
                select (mem, attr.Order);

        var inst = Activator.CreateInstance<T>();

        foreach (var (field, column) in fields)
        {
            object value;
            var rawValue = tuple[column];

            if (field.FieldType == typeof(string))
            {
                value = rawValue;
            }
            else
            {
                var parse = field.FieldType.GetMethod("Parse", BindingFlags.Static | BindingFlags.Public, new[] { typeof(string) });
                if (parse == null)
                    throw new NotSupportedException($"No static Parse method on {field.FieldType.FullName}");

                value = parse.Invoke(null, new[] { rawValue });
            }

            field.SetValue(inst, value);
        }

        return inst;
    }

    public static IEnumerable<T> Read<T>(string table) => ReadAsTuples(table).Select(TupleToObject<T>);
}

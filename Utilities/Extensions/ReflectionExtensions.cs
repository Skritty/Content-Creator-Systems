using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

public static class ReflectionExtensions
{
    public static object Cast(this Type Type, object data)
    {
        var DataParam = Expression.Parameter(typeof(object), "data");
        var Body = Expression.Block(Expression.Convert(Expression.Convert(DataParam, data.GetType()), Type));

        var Run = Expression.Lambda(Body, DataParam).Compile();
        var ret = Run.DynamicInvoke(data);
        return ret;
    }

    public static Type GetEnumerableType(this Type type)
    {
        if (type == null)
            throw new ArgumentNullException("type");

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            return type.GetGenericArguments()[0];

        var iface = (from i in type.GetInterfaces()
                     where i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>)
                     select i).FirstOrDefault();

        if (iface == null)
            throw new ArgumentException("Does not represent an enumerable type.", "type");

        return GetEnumerableType(iface);
    }

    public static bool IsGenericList(this Type oType)
    {
        return (oType.IsGenericType && (oType.GetGenericTypeDefinition() == typeof(List<>)));
    }

    public static object GetValue(this MemberInfo info, object obj)
    {
        switch (info.MemberType)
        {
            case MemberTypes.Field:
                {
                    return (info as FieldInfo).GetValue(obj);
                }
            case MemberTypes.Property:
                {
                    return (info as PropertyInfo).GetValue(obj, null);
                }
            default:
                {
                    return null;
                }
        }
    }

    public static void SetValue(this MemberInfo info, object obj, object value)
    {
        switch (info.MemberType)
        {
            case MemberTypes.Field:
                {
                    (info as FieldInfo).SetValue(obj, value);
                    break;
                }
            case MemberTypes.Property:
                {
                    (info as PropertyInfo).SetValue(obj, value);
                    break;
                }
            default:
                {
                    break;
                }
        }
    }

    public static bool CanWrite(this MemberInfo info)
    {
        switch (info.MemberType)
        {
            case MemberTypes.Field:
                {
                    return (info as FieldInfo).IsPublic;
                }
            case MemberTypes.Property:
                {
                    return (info as PropertyInfo).CanWrite;
                }
            default:
                {
                    return false;
                }
        }
    }

    public static bool CanRead(this MemberInfo info)
    {
        switch (info.MemberType)
        {
            case MemberTypes.Field:
                {
                    return (info as FieldInfo).IsPublic;
                }
            case MemberTypes.Property:
                {
                    return (info as PropertyInfo).CanRead;
                }
            default:
                {
                    return false;
                }
        }
    }
}

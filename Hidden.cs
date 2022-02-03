using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Utils;
using System.Reflection.Extensions;
using System.Runtime.CompilerServices;

[AttributeUsage(AttributeTargets.All ^ (AttributeTargets.Assembly | AttributeTargets.Module | AttributeTargets.Parameter | AttributeTargets.GenericParameter | AttributeTargets.ReturnValue), Inherited = false)]
public class Hidden : Attribute
{
    public static bool IsHid { get; private set; }
    [ModuleInitializer]
    public static void Hide()
        => Hide(Assembly.GetCallingAssembly());
    public static void Hide(Assembly assembly)
    {
        if (IsHid) return;
        foreach (Type type in assembly.GetTypes())
        {
            if (type.GetCustomAttribute(typeof(Hidden)) != null)
            {
                if (typeof(Attribute).IsAssignableFrom(type))
                    HiddenAttrs.Add(type);
                else
                    HiddenMembers.Add(type);
            }
            HiddenMembers.AddRange(type.GetMembers(AccessUtils.all).Where(m => m.GetCustomAttribute(typeof(Hidden)) != null));
        }
        foreach (Type patchTo in RuntimeTypes)
        {
            foreach (MethodInfo method in patchTo.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
            {
                try
                {
                    if (method.Name.Contains("Get"))
                    {
                        if (typeof(MemberInfo).IsAssignableFrom(method.ReturnType))
                        {
                            if (!method.HasMethodBody())
                                continue;
                            if (method.ReturnType.IsArray)
                            {
                                MethodUtils.Runtime.Patch(method, postfix: new RuntimeMethod(getsFix));
                                HidePatches.Add((method, getsFix));
                            }
                            else
                            {
                                MethodUtils.Runtime.Patch(method, postfix: new RuntimeMethod(getFix));
                                HidePatches.Add((method, getsFix));
                            }
                        }
                        else if (method.Name.Contains("Attribute"))
                        {
                            if (!method.HasMethodBody())
                                continue;
                            if (method.ReturnType.IsArray)
                            {
                                MethodUtils.Runtime.Patch(method, postfix: new RuntimeMethod(getAttrsFix));
                                HidePatches.Add((method, getsFix));
                            }
                            else
                            {
                                MethodUtils.Runtime.Patch(method, postfix: new RuntimeMethod(getAttrFix));
                                HidePatches.Add((method, getsFix));
                            }
                        }
                    }

                }
                catch
                {
                    MethodInfo method2 = AccessUtils.GetDeclaredMember<MethodBase>(method) as MethodInfo;
                    try
                    {
                        if (method2.Name.Contains("Get"))
                        {
                            if (typeof(MemberInfo).IsAssignableFrom(method2.ReturnType))
                            {
                                if (!method2.HasMethodBody())
                                    continue;
                                if (method2.ReturnType.IsArray)
                                {
                                    MethodUtils.Runtime.Patch(method2, postfix: new RuntimeMethod(getsFix));
                                    HidePatches.Add((method2, getsFix));
                                }
                                else
                                {
                                    MethodUtils.Runtime.Patch(method2, postfix: new RuntimeMethod(getFix));
                                    HidePatches.Add((method2, getsFix));
                                }
                            }
                            else if (method2.Name.Contains("Attribute"))
                            {
                                if (!method2.HasMethodBody())
                                    continue;
                                if (method2.ReturnType.IsArray)
                                {
                                    MethodUtils.Runtime.Patch(method2, postfix: new RuntimeMethod(getAttrsFix));
                                    HidePatches.Add((method2, getsFix));
                                }
                                else
                                {
                                    MethodUtils.Runtime.Patch(method2, postfix: new RuntimeMethod(getAttrFix));
                                    HidePatches.Add((method2, getsFix));
                                }
                            }
                        }
                        continue;
                    }
                    catch { }
                }
            }
        }
        IsHid = true;
    }
    public static void Show()
    {
        if (!IsHid) return;
        foreach (var tuple in HidePatches)
            MethodUtils.Runtime.Unpatch(tuple.Item1, tuple.Item2);
        HiddenMembers.Clear();
        HidePatches.Clear();
        IsHid = false;
    }
#pragma warning disable IDE0051
    private static void GetFix(ref MemberInfo __result)
    {
        if (!IsHid) return;
        if (ChkContainsManual(HiddenMembers, __result))
            __result = null;
    }
    private static void GetsFix(ref MemberInfo[] __result)
    {
        if (!IsHid) return;
        foreach (MemberInfo member in HiddenMembers)
            if (ChkContainsManual(__result, member))
            {
                var list = __result.ToList();
                list.Remove(member);
                __result = list.ToArray();
            }
    }
    private static void GetAttrFix(ref object __result)
    {
        if (!IsHid) return;
        if (ChkContainsManual(HiddenAttrs.ToArray(), __result.GetType()))
            __result = null;
    }
    private static void GetAttrsFix(ref object[] __result)
    {
        if (!IsHid) return;
        var arr = __result.Select(o => o.GetType()).ToArray();
        foreach (Type attr in HiddenAttrs)
            if (ChkContainsManual(arr, attr))
            {
                var dict = __result.ToDictionary(o => o.GetType());
                dict.Remove(attr);
                __result = dict.Values.ToArray();
                return;
            }
    }
#pragma warning restore IDE0051
    private static readonly MethodInfo getFix = typeof(Hidden).GetMethod("GetFix", AccessUtils.all);
    private static readonly MethodInfo getsFix = typeof(Hidden).GetMethod("GetsFix", AccessUtils.all);
    private static readonly MethodInfo getAttrFix = typeof(Hidden).GetMethod("GetAttrFix", AccessUtils.all);
    private static readonly MethodInfo getAttrsFix = typeof(Hidden).GetMethod("GetAttrsFix", AccessUtils.all);
    private static readonly Type[] RuntimeTypes = new Type[]
    {
            Type.GetType("System.Reflection.RuntimeMethodInfo"),
            Type.GetType("System.RuntimeType"),
            Type.GetType("System.Reflection.RuntimeEventInfo"),
            Type.GetType("System.Reflection.RtFieldInfo"),
            Type.GetType("System.Reflection.RuntimeConstructorInfo"),
            Type.GetType("System.Reflection.RuntimePropertyInfo")
    };
    private static bool ChkContainsManual<T>(List<T> list, T item)
    {
        for (int i = 0; i < list.Count; i++)
            if (item?.Equals(list[i]) ?? false)
                return true;
        return false;
    }
    private static bool ChkContainsManual<T>(T[] list, T item)
    {
        for (int i = 0; i < list.Length; i++)
            if (item.Equals(list[i]))
                return true;
        return false;
    }
    private static readonly List<MemberInfo> HiddenMembers = new List<MemberInfo>();
    private static readonly List<Type> HiddenAttrs = new List<Type>();
    private static readonly List<(MethodInfo, MethodInfo)> HidePatches = new List<(MethodInfo, MethodInfo)>();
}
namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public sealed class ModuleInitializerAttribute : Attribute
    {
    }
}

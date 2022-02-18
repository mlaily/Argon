﻿#region License
// Copyright (c) 2007 James Newton-King
//
// Permission is hereby granted, free of charge, to any person
// obtaining a copy of this software and associated documentation
// files (the "Software"), to deal in the Software without
// restriction, including without limitation the rights to use,
// copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following
// conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.
#endregion

#if !NETSTANDARD2_0
using System.Reflection.Emit;

class DynamicReflectionDelegateFactory : ReflectionDelegateFactory
{
    internal static DynamicReflectionDelegateFactory Instance { get; } = new();

    static DynamicMethod CreateDynamicMethod(string name, Type? returnType, Type[] parameterTypes, Type owner)
    {
        var dynamicMethod = !owner.IsInterface
            ? new DynamicMethod(name, returnType, parameterTypes, owner, true)
            : new DynamicMethod(name, returnType, parameterTypes, owner.Module, true);

        return dynamicMethod;
    }

    public override ObjectConstructor<object> CreateParameterizedConstructor(MethodBase method)
    {
        var dynamicMethod = CreateDynamicMethod(method.ToString(), typeof(object), new[] { typeof(object[]) }, method.DeclaringType);
        var generator = dynamicMethod.GetILGenerator();

        GenerateCreateMethodCallIL(method, generator, 0);

        return (ObjectConstructor<object>)dynamicMethod.CreateDelegate(typeof(ObjectConstructor<object>));
    }

    public override MethodCall<T, object?> CreateMethodCall<T>(MethodBase method)
    {
        var dynamicMethod = CreateDynamicMethod(method.ToString(), typeof(object), new[] { typeof(object), typeof(object[]) }, method.DeclaringType);
        var generator = dynamicMethod.GetILGenerator();

        GenerateCreateMethodCallIL(method, generator, 1);

        return (MethodCall<T, object?>)dynamicMethod.CreateDelegate(typeof(MethodCall<T, object?>));
    }

    void GenerateCreateMethodCallIL(MethodBase method, ILGenerator generator, int argsIndex)
    {
        ParameterInfo[] args = method.GetParameters();

        var argsOk = generator.DefineLabel();

        // throw an error if the number of argument values doesn't match method parameters
        generator.Emit(OpCodes.Ldarg, argsIndex);
        generator.Emit(OpCodes.Ldlen);
        generator.Emit(OpCodes.Ldc_I4, args.Length);
        generator.Emit(OpCodes.Beq, argsOk);
        generator.Emit(OpCodes.Newobj, typeof(TargetParameterCountException).GetConstructor(Type.EmptyTypes));
        generator.Emit(OpCodes.Throw);

        generator.MarkLabel(argsOk);

        if (!method.IsConstructor && !method.IsStatic)
        {
            generator.PushInstance(method.DeclaringType);
        }

        var localConvertible = generator.DeclareLocal(typeof(IConvertible));
        var localObject = generator.DeclareLocal(typeof(object));

        var variableAddressOpCode = args.Length < 256 ? OpCodes.Ldloca_S : OpCodes.Ldloca;
        var variableLoadOpCode = args.Length < 256 ? OpCodes.Ldloc_S : OpCodes.Ldloc;

        for (var i = 0; i < args.Length; i++)
        {
            var parameter = args[i];
            var parameterType = parameter.ParameterType;

            if (parameterType.IsByRef)
            {
                parameterType = parameterType.GetElementType();

                var localVariable = generator.DeclareLocal(parameterType);

                // don't need to set variable for 'out' parameter
                if (!parameter.IsOut)
                {
                    generator.PushArrayInstance(argsIndex, i);

                    if (parameterType.IsValueType)
                    {
                        var skipSettingDefault = generator.DefineLabel();
                        var finishedProcessingParameter = generator.DefineLabel();

                        // check if parameter is not null
                        generator.Emit(OpCodes.Brtrue_S, skipSettingDefault);

                        // parameter has no value, initialize to default
                        generator.Emit(variableAddressOpCode, localVariable);
                        generator.Emit(OpCodes.Initobj, parameterType);
                        generator.Emit(OpCodes.Br_S, finishedProcessingParameter);

                        // parameter has value, get value from array again and unbox and set to variable
                        generator.MarkLabel(skipSettingDefault);
                        generator.PushArrayInstance(argsIndex, i);
                        generator.UnboxIfNeeded(parameterType);
                        generator.Emit(OpCodes.Stloc_S, localVariable);

                        // parameter finished, we out!
                        generator.MarkLabel(finishedProcessingParameter);
                    }
                    else
                    {
                        generator.UnboxIfNeeded(parameterType);
                        generator.Emit(OpCodes.Stloc_S, localVariable);
                    }
                }

                generator.Emit(variableAddressOpCode, localVariable);
            }
            else if (parameterType.IsValueType)
            {
                generator.PushArrayInstance(argsIndex, i);
                generator.Emit(OpCodes.Stloc_S, localObject);

                // have to check that value type parameters aren't null
                // otherwise they will error when unboxed
                var skipSettingDefault = generator.DefineLabel();
                var finishedProcessingParameter = generator.DefineLabel();

                // check if parameter is not null
                generator.Emit(OpCodes.Ldloc_S, localObject);
                generator.Emit(OpCodes.Brtrue_S, skipSettingDefault);

                // parameter has no value, initialize to default
                var localVariable = generator.DeclareLocal(parameterType);
                generator.Emit(variableAddressOpCode, localVariable);
                generator.Emit(OpCodes.Initobj, parameterType);
                generator.Emit(variableLoadOpCode, localVariable);
                generator.Emit(OpCodes.Br_S, finishedProcessingParameter);

                // argument has value, try to convert it to parameter type
                generator.MarkLabel(skipSettingDefault);

                if (parameterType.IsPrimitive)
                {
                    // for primitive types we need to handle type widening (e.g. short -> int)
                    var toParameterTypeMethod = typeof(IConvertible)
                        .GetMethod($"To{parameterType.Name}", new[] { typeof(IFormatProvider) });

                    if (toParameterTypeMethod != null)
                    {
                        var skipConvertible = generator.DefineLabel();

                        // check if argument type is an exact match for parameter type
                        // in this case we may use cheap unboxing instead
                        generator.Emit(OpCodes.Ldloc_S, localObject);
                        generator.Emit(OpCodes.Isinst, parameterType);
                        generator.Emit(OpCodes.Brtrue_S, skipConvertible);

                        // types don't match, check if argument implements IConvertible
                        generator.Emit(OpCodes.Ldloc_S, localObject);
                        generator.Emit(OpCodes.Isinst, typeof(IConvertible));
                        generator.Emit(OpCodes.Stloc_S, localConvertible);
                        generator.Emit(OpCodes.Ldloc_S, localConvertible);
                        generator.Emit(OpCodes.Brfalse_S, skipConvertible);

                        // convert argument to parameter type
                        generator.Emit(OpCodes.Ldloc_S, localConvertible);
                        generator.Emit(OpCodes.Ldnull);
                        generator.Emit(OpCodes.Callvirt, toParameterTypeMethod);
                        generator.Emit(OpCodes.Br_S, finishedProcessingParameter);

                        generator.MarkLabel(skipConvertible);
                    }
                }

                // we got here because either argument type matches parameter (conversion will succeed),
                // or argument type doesn't match parameter, but we're out of options (conversion will fail)
                generator.Emit(OpCodes.Ldloc_S, localObject);

                generator.UnboxIfNeeded(parameterType);

                // parameter finished, we out!
                generator.MarkLabel(finishedProcessingParameter);
            }
            else
            {
                generator.PushArrayInstance(argsIndex, i);

                generator.UnboxIfNeeded(parameterType);
            }
        }

        if (method.IsConstructor)
        {
            generator.Emit(OpCodes.Newobj, (ConstructorInfo)method);
        }
        else
        {
            generator.CallMethod((MethodInfo)method);
        }

        var returnType = method.IsConstructor
            ? method.DeclaringType
            : ((MethodInfo)method).ReturnType;

        if (returnType != typeof(void))
        {
            generator.BoxIfNeeded(returnType);
        }
        else
        {
            generator.Emit(OpCodes.Ldnull);
        }

        generator.Return();
    }

    public override Func<T> CreateDefaultConstructor<T>(Type type)
    {
        var dynamicMethod = CreateDynamicMethod($"Create{type.FullName}", typeof(T), Type.EmptyTypes, type);
        dynamicMethod.InitLocals = true;
        var generator = dynamicMethod.GetILGenerator();

        GenerateCreateDefaultConstructorIL(type, generator, typeof(T));

        return (Func<T>)dynamicMethod.CreateDelegate(typeof(Func<T>));
    }

    void GenerateCreateDefaultConstructorIL(Type type, ILGenerator generator, Type delegateType)
    {
        if (type.IsValueType)
        {
            generator.DeclareLocal(type);
            generator.Emit(OpCodes.Ldloc_0);

            // only need to box if the delegate isn't returning the value type
            if (type != delegateType)
            {
                generator.Emit(OpCodes.Box, type);
            }
        }
        else
        {
            var constructorInfo =
                type.GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, Type.EmptyTypes, null);

            if (constructorInfo == null)
            {
                throw new ArgumentException($"Could not get constructor for {type}.");
            }

            generator.Emit(OpCodes.Newobj, constructorInfo);
        }

        generator.Return();
    }

    public override Func<T, object?> CreateGet<T>(PropertyInfo propertyInfo)
    {
        var dynamicMethod = CreateDynamicMethod($"Get{propertyInfo.Name}", typeof(object), new[] { typeof(T) }, propertyInfo.DeclaringType);
        var generator = dynamicMethod.GetILGenerator();

        GenerateCreateGetPropertyIL(propertyInfo, generator);

        return (Func<T, object?>)dynamicMethod.CreateDelegate(typeof(Func<T, object?>));
    }

    void GenerateCreateGetPropertyIL(PropertyInfo propertyInfo, ILGenerator generator)
    {
        var getMethod = propertyInfo.GetGetMethod(true);
        if (getMethod == null)
        {
            throw new ArgumentException($"Property '{propertyInfo.Name}' does not have a getter.");
        }

        if (!getMethod.IsStatic)
        {
            generator.PushInstance(propertyInfo.DeclaringType);
        }

        generator.CallMethod(getMethod);
        generator.BoxIfNeeded(propertyInfo.PropertyType);
        generator.Return();
    }

    public override Func<T, object?> CreateGet<T>(FieldInfo fieldInfo)
    {
        if (fieldInfo.IsLiteral)
        {
            var constantValue = fieldInfo.GetValue(null);
            Func<T, object?> getter = _ => constantValue;
            return getter;
        }

        var dynamicMethod = CreateDynamicMethod($"Get{fieldInfo.Name}", typeof(T), new[] { typeof(object) }, fieldInfo.DeclaringType);
        var generator = dynamicMethod.GetILGenerator();

        GenerateCreateGetFieldIL(fieldInfo, generator);

        return (Func<T, object?>)dynamicMethod.CreateDelegate(typeof(Func<T, object?>));
    }

    void GenerateCreateGetFieldIL(FieldInfo fieldInfo, ILGenerator generator)
    {
        if (!fieldInfo.IsStatic)
        {
            generator.PushInstance(fieldInfo.DeclaringType);
            generator.Emit(OpCodes.Ldfld, fieldInfo);
        }
        else
        {
            generator.Emit(OpCodes.Ldsfld, fieldInfo);
        }

        generator.BoxIfNeeded(fieldInfo.FieldType);
        generator.Return();
    }

    public override Action<T, object?> CreateSet<T>(FieldInfo fieldInfo)
    {
        var dynamicMethod = CreateDynamicMethod($"Set{fieldInfo.Name}", null, new[] { typeof(T), typeof(object) }, fieldInfo.DeclaringType);
        var generator = dynamicMethod.GetILGenerator();

        GenerateCreateSetFieldIL(fieldInfo, generator);

        return (Action<T, object?>)dynamicMethod.CreateDelegate(typeof(Action<T, object?>));
    }

    internal static void GenerateCreateSetFieldIL(FieldInfo fieldInfo, ILGenerator generator)
    {
        if (!fieldInfo.IsStatic)
        {
            generator.PushInstance(fieldInfo.DeclaringType);
        }

        generator.Emit(OpCodes.Ldarg_1);
        generator.UnboxIfNeeded(fieldInfo.FieldType);

        if (!fieldInfo.IsStatic)
        {
            generator.Emit(OpCodes.Stfld, fieldInfo);
        }
        else
        {
            generator.Emit(OpCodes.Stsfld, fieldInfo);
        }

        generator.Return();
    }

    public override Action<T, object?> CreateSet<T>(PropertyInfo propertyInfo)
    {
        var dynamicMethod = CreateDynamicMethod($"Set{propertyInfo.Name}", null, new[] { typeof(T), typeof(object) }, propertyInfo.DeclaringType);
        var generator = dynamicMethod.GetILGenerator();

        GenerateCreateSetPropertyIL(propertyInfo, generator);

        return (Action<T, object?>)dynamicMethod.CreateDelegate(typeof(Action<T, object>));
    }

    internal static void GenerateCreateSetPropertyIL(PropertyInfo propertyInfo, ILGenerator generator)
    {
        var setMethod = propertyInfo.GetSetMethod(true);
        if (!setMethod.IsStatic)
        {
            generator.PushInstance(propertyInfo.DeclaringType);
        }

        generator.Emit(OpCodes.Ldarg_1);
        generator.UnboxIfNeeded(propertyInfo.PropertyType);
        generator.CallMethod(setMethod);
        generator.Return();
    }
}

#endif
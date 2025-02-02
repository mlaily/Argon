﻿// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

#if !NET5_0_OR_GREATER
using TestObjects;

public class DynamicReflectionDelegateFactoryTests : TestFixtureBase
{
    [Fact]
    public void ConstructorWithInString()
    {
        var constructor = typeof(InTestClass).GetConstructors().Single(c => c.GetParameters().Length == 1);

        var creator = DynamicReflectionDelegateFactory.Instance.CreateParameterizedConstructor(constructor);

        var args = new object[]
        {
            "Value"
        };
        var o = (InTestClass) creator(args);
        Assert.NotNull(o);
        Assert.Equal("Value", o.Value);
    }

    [Fact]
    public void ConstructorWithInStringAndBool()
    {
        var constructor = typeof(InTestClass).GetConstructors().Single(c => c.GetParameters().Length == 2);

        var creator = DynamicReflectionDelegateFactory.Instance.CreateParameterizedConstructor(constructor);

        var args = new object[]
        {
            "Value",
            true
        };
        var o = (InTestClass) creator(args);
        Assert.NotNull(o);
        Assert.Equal("Value", o.Value);
        XUnitAssert.True(o.B1);
    }

    [Fact]
    public void ConstructorWithRefString()
    {
        var constructor = typeof(OutAndRefTestClass).GetConstructors().Single(c => c.GetParameters().Length == 1);

        var creator = DynamicReflectionDelegateFactory.Instance.CreateParameterizedConstructor(constructor);

        var args = new object[]
        {
            "Input"
        };
        var o = (OutAndRefTestClass) creator(args);
        Assert.NotNull(o);
        Assert.Equal("Input", o.Input);
    }

    [Fact]
    public void ConstructorWithRefStringAndOutBool()
    {
        var constructor = typeof(OutAndRefTestClass).GetConstructors().Single(c => c.GetParameters().Length == 2);

        var creator = DynamicReflectionDelegateFactory.Instance.CreateParameterizedConstructor(constructor);

        var args = new object[]
        {
            "Input",
            false
        };
        var o = (OutAndRefTestClass) creator(args);
        Assert.NotNull(o);
        Assert.Equal("Input", o.Input);
        XUnitAssert.True(o.B1);
    }

    [Fact]
    public void ConstructorWithRefStringAndRefBoolAndRefBool()
    {
        var constructor = typeof(OutAndRefTestClass).GetConstructors().Single(c => c.GetParameters().Length == 3);

        var creator = DynamicReflectionDelegateFactory.Instance.CreateParameterizedConstructor(constructor);

        var args = new object[]
        {
            "Input",
            true,
            null
        };
        var o = (OutAndRefTestClass) creator(args);
        Assert.NotNull(o);
        Assert.Equal("Input", o.Input);
        XUnitAssert.True(o.B1);
        XUnitAssert.False(o.B2);
    }

    [Fact]
    public void CreateGetWithBadObjectTarget()
    {
        var p = new Person
        {
            Name = "Hi"
        };
        var setter = DynamicReflectionDelegateFactory.Instance.CreateGet<object>(typeof(Movie).GetProperty("Name"));

        XUnitAssert.Throws<InvalidCastException>(
            () => setter(p),
            "Unable to cast object of type 'TestObjects.Person' to type 'TestObjects.Movie'.");
    }

    [Fact]
    public void CreateSetWithBadObjectTarget() =>
        XUnitAssert.Throws<InvalidCastException>(
            () =>
            {
                var p = new Person();
                var m = new Movie();

                var setter = DynamicReflectionDelegateFactory.Instance.CreateSet<object>(typeof(Movie).GetProperty("Name"));

                setter(m, "Hi");

                Assert.Equal(m.Name, "Hi");

                setter(p, "Hi");

                Assert.Equal(p.Name, "Hi");
            },
            "Unable to cast object of type 'TestObjects.Person' to type 'TestObjects.Movie'.");

    [Fact]
    public void CreateSetWithBadTarget() =>
        XUnitAssert.Throws<InvalidCastException>(
            () =>
            {
                object structTest = new StructTest();

                var setter = DynamicReflectionDelegateFactory.Instance.CreateSet<object>(typeof(StructTest).GetProperty("StringProperty"));

                setter(structTest, "Hi");

                Assert.Equal("Hi", ((StructTest) structTest).StringProperty);

                setter(new TimeSpan(), "Hi");
            },
            "Specified cast is not valid.");

    [Fact]
    public void CreateSetWithBadObjectValue() =>
        XUnitAssert.Throws<InvalidCastException>(
            () =>
            {
                var m = new Movie();

                var setter = DynamicReflectionDelegateFactory.Instance.CreateSet<object>(typeof(Movie).GetProperty("Name"));

                setter(m, new Version("1.1.1.1"));
            },
            "Unable to cast object of type 'System.Version' to type 'System.String'.");

    [Fact]
    public void CreateStaticMethodCall()
    {
        var castMethodInfo = typeof(DictionaryKey).GetMethod(
            "op_Implicit",
            new[]
            {
                typeof(string)
            });

        Assert.NotNull(castMethodInfo);

        var call = DynamicReflectionDelegateFactory.Instance.CreateMethodCall<object>(castMethodInfo);

        var result = call(null, "First!");
        Assert.NotNull(result);

        var key = (DictionaryKey) result;
        Assert.Equal("First!", key.Value);
    }

    [Fact]
    public void CreatePropertyGetter()
    {
        var namePropertyInfo = typeof(Person).GetProperty(nameof(Person.Name));

        Assert.NotNull(namePropertyInfo);

        var call = DynamicReflectionDelegateFactory.Instance.CreateGet<Person>(namePropertyInfo);

        var p = new Person
        {
            Name = "Name!"
        };

        var result = call(p);
        Assert.NotNull(result);

        Assert.Equal("Name!", (string) result);
    }

    [Fact]
    public void ConstructorStruct()
    {
        var creator1 = DynamicReflectionDelegateFactory.Instance.CreateDefaultConstructor<object>(typeof(MyStruct));
        var myStruct1 = (MyStruct) creator1.Invoke();
        Assert.Equal(0, myStruct1.IntProperty);

        var creator2 = DynamicReflectionDelegateFactory.Instance.CreateDefaultConstructor<MyStruct>(typeof(MyStruct));
        var myStruct2 = creator2.Invoke();
        Assert.Equal(0, myStruct2.IntProperty);
    }

    public struct TestStruct
    {
        public TestStruct(int i) =>
            Value = i;

        public int Value { get; }
    }

    public static TestStruct StructMethod(TestStruct s) =>
        new(s.Value + s.Value);

    [Fact]
    public void CreateStructMethodCall()
    {
        var methodInfo = typeof(DynamicReflectionDelegateFactoryTests).GetMethod(
            nameof(StructMethod),
            new[]
            {
                typeof(TestStruct)
            });

        Assert.NotNull(methodInfo);

        var call = DynamicReflectionDelegateFactory.Instance.CreateMethodCall<object>(methodInfo);

        var result = call(null, new TestStruct(123));
        Assert.NotNull(result);

        var s = (TestStruct) result;
        Assert.Equal(246, s.Value);
    }
}

#endif
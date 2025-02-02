﻿// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

using Autofac;
using Autofac.Core;
using Autofac.Core.Activators.Reflection;
using TestObjects;

public class DependencyInjectionTests : TestFixtureBase
{
    [Fact]
    public void ResolveContractFromAutofac()
    {
        var builder = new ContainerBuilder();
        builder.RegisterType<Company>().As<ICompany>();
        var container = builder.Build();

        var resolver = new AutofacContractResolver(container);

        var user = JsonConvert.DeserializeObject<User>(
            "{'company':{'company_name':'Company name!'}}",
            new JsonSerializerSettings
            {
                ContractResolver = resolver
            });

        Assert.Equal("Company name!", user.Company.CompanyName);
    }

    [Fact]
    public void CreateObjectWithParameters()
    {
        var count = 0;

        var builder = new ContainerBuilder();
        builder.RegisterType<TaskRepository>().As<ITaskRepository>();
        builder.RegisterType<TaskController>();
        builder.Register(_ =>
        {
            count++;
            return new LogManager(new(2000, 12, 12));
        }).As<ILogger>();

        var container = builder.Build();

        var contractResolver = new AutofacContractResolver(container);

        var controller = JsonConvert.DeserializeObject<TaskController>(
            """
                {
                    'Logger': {
                        'Level':'Debug'
                    }
                }
                """,
            new JsonSerializerSettings
            {
                ContractResolver = contractResolver
            });

        Assert.NotNull(controller);
        Assert.NotNull(controller.Logger);

        Assert.Equal(1, count);

        Assert.Equal(new(2000, 12, 12), controller.Logger.DateTime);
        Assert.Equal("Debug", controller.Logger.Level);
    }

    [Fact]
    public void CreateObjectWithSettableParameter()
    {
        var count = 0;

        var builder = new ContainerBuilder();
        builder.Register(_ =>
        {
            count++;
            return new TaskRepository();
        }).As<ITaskRepository>();
        builder.RegisterType<HasSettableProperty>();
        builder.Register(_ =>
        {
            count++;
            return new LogManager(new(2000, 12, 12));
        }).As<ILogger>();

        var container = builder.Build();

        var contractResolver = new AutofacContractResolver(container);

        var o = JsonConvert.DeserializeObject<HasSettableProperty>(
            """
                {
                    'Logger': {
                        'Level': 'Debug'
                    },
                    'Repository': {
                        'ConnectionString': 'server=.',
                        'CreatedOn': '2015-04-01 20:00'
                    },
                    'People': [
                        {
                            'Name': 'Name1!'
                        },
                        {
                            'Name': 'Name2!'
                        }
                    ],
                    'Person': {
                        'Name': 'Name3!'
                    }
                }
                """,
            new JsonSerializerSettings
            {
                ContractResolver = contractResolver
            });

        Assert.NotNull(o);
        Assert.NotNull(o.Logger);
        Assert.NotNull(o.Repository);
        Assert.Equal(o.Repository.CreatedOn, DateTime.Parse("2015-04-01 20:00"));

        Assert.Equal(2, count);

        Assert.Equal(new(2000, 12, 12), o.Logger.DateTime);
        Assert.Equal("Debug", o.Logger.Level);
        Assert.Equal("server=.", o.Repository.ConnectionString);
        Assert.Equal(2, o.People.Count);
        Assert.Equal("Name1!", o.People[0].Name);
        Assert.Equal("Name2!", o.People[1].Name);
        Assert.Equal("Name3!", o.Person.Name);
    }


    public interface IBase
    {
        DateTime CreatedOn { get; set; }
    }

    public interface ITaskRepository : IBase
    {
        string ConnectionString { get; set; }
    }

    public interface ILogger
    {
        DateTime DateTime { get; }
        string Level { get; set; }
    }

    public class Base : IBase
    {
        public DateTime CreatedOn { get; set; }
    }

    public class TaskRepository : Base, ITaskRepository
    {
        public string ConnectionString { get; set; }
    }

    public class LogManager : ILogger
    {
        public LogManager(DateTime dt) =>
            DateTime = dt;

        public DateTime DateTime { get; }

        public string Level { get; set; }
    }

    public class TaskController
    {
        public TaskController(ITaskRepository repository, ILogger logger)
        {
            Repository = repository;
            Logger = logger;
        }

        public ITaskRepository Repository { get; }

        public ILogger Logger { get; }
    }

    public class HasSettableProperty
    {
        public ILogger Logger { get; set; }
        public ITaskRepository Repository { get; set; }
        public IList<Person> People { get; set; }
        public Person Person { get; set; }

        public HasSettableProperty(ILogger logger) =>
            Logger = logger;
    }

    [DataContract]
    public class User
    {
        [DataMember(Name = "first_name")] public string FirstName { get; set; }

        [DataMember(Name = "company")] public ICompany Company { get; set; }
    }

    public interface ICompany
    {
        string CompanyName { get; set; }
    }

    [DataContract]
    public class Company : ICompany
    {
        [DataMember(Name = "company_name")] public string CompanyName { get; set; }
    }

    public class AutofacContractResolver : DefaultContractResolver
    {
        readonly IContainer _container;

        public AutofacContractResolver(IContainer container) =>
            _container = container;

        protected override JsonObjectContract CreateObjectContract(Type type)
        {
            // use Autofac to create types that have been registered with it
            if (_container.IsRegistered(type))
            {
                var contract = ResolveContact(type);
                contract.DefaultCreator = () => _container.Resolve(type);

                return contract;
            }

            return base.CreateObjectContract(type);
        }

        JsonObjectContract ResolveContact(Type type)
        {
            // attempt to create the contact from the resolved type
            if (_container.ComponentRegistry.TryGetRegistration(new TypedService(type), out var registration))
            {
                var viewType = (registration.Activator as ReflectionActivator)?.LimitType;
                if (viewType != null)
                {
                    return base.CreateObjectContract(viewType);
                }
            }

            // fall back to using the registered type
            return base.CreateObjectContract(type);
        }
    }
}
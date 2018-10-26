using System;
using System.Collections.Generic;
using Autofac;
using Autofac.Core;
using Autofac.Core.Resolving;
using Machine.Specifications;
using Moq;

namespace Dolittle.DependencyInversion.Autofac.Specifications.for_BindingsPerTenant.given
{
    public class generic_type_binding : bindings_per_tenants
    {
        protected interface first_dependency {}
        protected interface second_dependency {}

        protected interface some_interface<T>
        {
        }

        protected class some_type<T>
        {
        }

        protected static Binding binding;

        protected static MyContext component_context;

        public class MyContext : IComponentContext, IInstanceLookup
        {
            public IComponentRegistry ComponentRegistry { get; set; }

            public IComponentRegistration ComponentRegistration { get; set; }

            public ILifetimeScope ActivationScope => throw new NotImplementedException();

            public IEnumerable<Parameter> Parameters => throw new NotImplementedException();

            public event EventHandler<InstanceLookupEndingEventArgs> InstanceLookupEnding = (s,e) => {};
            public event EventHandler<InstanceLookupCompletionBeginningEventArgs> CompletionBeginning = (s,e) => {};
            public event EventHandler<InstanceLookupCompletionEndingEventArgs> CompletionEnding = (s,e) => {};

            public object ResolveComponent(IComponentRegistration registration, IEnumerable<Parameter> parameters)
            {
                throw new System.NotImplementedException();
            }
        }


        Establish context = () =>
        {
            component_context = new MyContext();
            var activator = new Mock<IInstanceActivator>();
            activator.SetupGet(_ => _.LimitType).Returns(typeof(some_type<string>));
            var componentRegistration = new Mock<IComponentRegistration>();
            componentRegistration.SetupGet(_ => _.Activator).Returns(activator.Object);
            component_context.ComponentRegistration = componentRegistration.Object;
            binding = new Binding(typeof(some_interface<>), new Strategies.Type(typeof(some_type<>)), new Scopes.SingletonPerTenant());
        };
    }
}
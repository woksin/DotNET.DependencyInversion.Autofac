
using Autofac;
using Machine.Specifications;
using Moq;

namespace Dolittle.DependencyInversion.Autofac.Specifications.for_BindingsPerTenant.given
{
    public class callback_binding : bindings_per_tenants
    {
        protected interface first_dependency {}
        protected interface second_dependency {}

        protected class some_type
        {
        }

        protected static Binding binding;
        protected static IComponentContext component_context;

        Establish context = () =>
        {
            component_context = Mock.Of<IComponentContext>();
            binding = new Binding(typeof(some_type), new Strategies.Callback(() => new some_type()), new Scopes.SingletonPerTenant());
        };
    }
}
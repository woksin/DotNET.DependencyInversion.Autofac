
using Machine.Specifications;

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


        Establish context = () =>
        {
            binding = new Binding(typeof(some_type), new Strategies.Callback(() => new some_type()), new Scopes.SingletonPerTenant());
        };
    }
}
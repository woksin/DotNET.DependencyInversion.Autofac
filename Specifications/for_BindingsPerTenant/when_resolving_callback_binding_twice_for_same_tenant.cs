using System;
using Dolittle.Tenancy;
using Machine.Specifications;

namespace Dolittle.DependencyInversion.Autofac.Specifications.for_BindingsPerTenant
{
    public class when_resolving_callback_binding_twice_for_same_tenant : given.callback_binding
    {
        static TenantId tenant = Guid.NewGuid();
        static object first_instance;
        static object second_instance;

        Establish context = () => execution_context_manager.CurrentFor(tenant);

        Because of = () =>
        {
            first_instance = BindingsPerTenants.Resolve(component_context,binding);
            second_instance = BindingsPerTenants.Resolve(component_context,binding);
        };

        It should_result_in_same_instance = () => first_instance.ShouldEqual(second_instance);
    }
}
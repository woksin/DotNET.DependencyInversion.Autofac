using System;
using Dolittle.Tenancy;
using Machine.Specifications;

namespace Dolittle.DependencyInversion.Autofac.Specifications.for_BindingsPerTenant
{
    public class when_resolving_generic_type_binding_for_two_different_tenants : given.generic_type_binding
    {
        static TenantId first_tenant = Guid.NewGuid();
        static TenantId second_tenant = Guid.NewGuid();
        
        static object first_instance;
        static object second_instance;

        Because of = () =>
        {
            execution_context_manager.CurrentFor(first_tenant);
            first_instance = BindingsPerTenants.Resolve(component_context,binding);
            execution_context_manager.CurrentFor(second_tenant);
            second_instance = BindingsPerTenants.Resolve(component_context,binding);
        };

        It should_result_in_two_different_instances = () => first_instance.ShouldNotEqual(second_instance);
    }
}
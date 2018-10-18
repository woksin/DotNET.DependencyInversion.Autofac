using Machine.Specifications;

namespace Dolittle.DependencyInversion.Autofac.Specifications.for_BindingsPerTenant.given
{
    public class bindings_per_tenants : all_dependencies
    {
        Establish context = () => 
        {
            BindingsPerTenants.Container = container.Object;
            BindingsPerTenants.ExecutionContextManager = execution_context_manager;
        };
    }
}
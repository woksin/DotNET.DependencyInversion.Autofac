using Moq;
using Machine.Specifications;
using Dolittle.Execution;
using Autofac;
using Dolittle.Logging;

namespace Dolittle.DependencyInversion.Autofac.Specifications.for_BindingsPerTenant.given
{
    public class all_dependencies
    {
        protected static Mock<global::Autofac.IContainer> container;
        protected static IExecutionContextManager execution_context_manager;

        Establish context = () =>
        {
            container = new Mock<global::Autofac.IContainer>();
            execution_context_manager = new ExecutionContextManager(Mock.Of<ILogger>());
        };
    }
}
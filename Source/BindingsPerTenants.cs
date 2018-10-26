/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Dolittle. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *--------------------------------------------------------------------------------------------*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Autofac;
using Autofac.Core.Resolving;
using Dolittle.Execution;
using Dolittle.Tenancy;

[assembly: InternalsVisibleTo("Dolittle.DependencyInversion.Autofac.Specs")]  

namespace Dolittle.DependencyInversion.Autofac
{
    /// <summary>
    /// Represents a system for dealing with bindings per tenants
    /// </summary>
    internal static class BindingsPerTenants
    {
        /// <summary>
        /// Gets or sets the <see cref="IContainer"/> to be used
        /// </summary>
        internal static global::Autofac.IContainer Container;

        static Dictionary<Binding, Dictionary<TenantId, object>> _instancesPerBindingPerTenant = new Dictionary<Binding, Dictionary<TenantId, object>>();

        static IExecutionContextManager _executionContextManager;

        internal static IExecutionContextManager ExecutionContextManager
        {
            get
            {
                if (_executionContextManager == null)
                    _executionContextManager = Container.Resolve<IExecutionContextManager>();

                return _executionContextManager;
            }

            set
            {
                _executionContextManager = value;
            }
        }

        /// <summary>
        /// Resolve a particular binding for current tenant
        /// </summary>
        /// <param name="context">The <see cref="IComponentContext"/> for the resolution</param>
        /// <param name="binding"><see cref="Binding"/> to resolve</param>
        /// <returns>Instance for the <see cref="TenantId">current tenant</see></returns>
        public static object Resolve(IComponentContext context, Binding binding)
        {
            lock(_instancesPerBindingPerTenant)
            {
                var tenant = ExecutionContextManager.Current.Tenant;

                if (_instancesPerBindingPerTenant.ContainsKey(binding) &&
                    _instancesPerBindingPerTenant[binding].ContainsKey(tenant))
                    return _instancesPerBindingPerTenant[binding][tenant];

                var instancesForTenants = GetInstancesForBinding(binding);

                object instance = null;

                if (binding.Strategy is Strategies.Type) instance = CreateInstanceFor(context, binding);
                if (binding.Strategy is Strategies.Callback) instance = ((Strategies.Callback) binding.Strategy).Target();

                instancesForTenants[tenant] = instance;

                return instance;
            }
        }

        static Dictionary<TenantId, object> GetInstancesForBinding(Binding binding)
        {
            Dictionary<TenantId, object> bindingsForTenants;
            if (!_instancesPerBindingPerTenant.ContainsKey(binding))
            {
                bindingsForTenants = new Dictionary<TenantId, object>();
                _instancesPerBindingPerTenant[binding] = bindingsForTenants;
            }
            else
            {
                bindingsForTenants = _instancesPerBindingPerTenant[binding];
            }

            return bindingsForTenants;
        }

        static object CreateInstanceFor(IComponentContext context, Binding binding)
        {
            object instance;
            var type = ((Strategies.Type) binding.Strategy).Target;
            var constructors = type.GetConstructors().ToArray();
            if (constructors.Length > 1) throw new Exception($"Unable to create instance of '{type.AssemblyQualifiedName}' - more than one constructor");
            var constructor = constructors[0];
            var parameterInstances = constructor.GetParameters().Select(_ => Container.Resolve(_.ParameterType)).ToArray();

            var instanceLookup = context as IInstanceLookup;

            if( binding.Service.ContainsGenericParameters && instanceLookup != null ) 
            {
                instance = Activator.CreateInstance(instanceLookup.ComponentRegistration.Activator.LimitType, parameterInstances);
            } 
            else 
            {
                instance = Activator.CreateInstance(type, parameterInstances);
            }
            
            return instance;
        }
    }
}
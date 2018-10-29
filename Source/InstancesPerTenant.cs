/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Dolittle. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *--------------------------------------------------------------------------------------------*/
using System;
using System.Collections.Generic;
using System.Text;
using Autofac;
using Dolittle.Collections;
using Dolittle.Execution;
using Dolittle.Tenancy;

namespace Dolittle.DependencyInversion.Autofac
{

    /// <summary>
    /// Represents a system that knows about instances per tenant
    /// </summary>
    public class InstancesPerTenant
    {
        Dictionary<string, object> _instancesPerKey = new Dictionary<string, object>();       
        IExecutionContextManager _executionContextManager;
        private readonly ITypeActivator _activator;

        /// <summary>
        /// Initializes a new instance of <see cref="InstancesPerTenant"/>
        /// </summary>
        /// <param name="containerBuilder"><see cref="ContainerBuilder"/> used for building the container</param>
        /// <param name="activator"><see cref="ITypeActivator"/> to use for activating types into instances</param>
        public InstancesPerTenant(ContainerBuilder containerBuilder, ITypeActivator activator)
        {
            containerBuilder.RegisterBuildCallback(c => _executionContextManager = c.Resolve<IExecutionContextManager>());
            _activator = activator;
        }
 

        /// <summary>
        /// Resolve an instance based on context, binding and service
        /// </summary>
        /// <param name="context"><see cref="IComponentContext"/> to resolve from</param>
        /// <param name="binding"><see cref="Binding"/> to resolve</param>
        /// <param name="service"><see cref="Type">Service type</see> asked for</param>
        /// <returns>Resolved instance</returns>
        public object Resolve(IComponentContext context, Binding binding, Type service)
        {
            lock (_instancesPerKey)
            {
                var key = GetKeyFrom(
                    _executionContextManager.Current.Tenant,
                    binding,
                    service);
                if (_instancesPerKey.ContainsKey(key)) return _instancesPerKey[key];

                object instance = null;
                switch (binding.Strategy)
                {
                    case Strategies.Type type:
                        instance = _activator.CreateInstanceFor(context, binding.Service, type.Target);
                        break;

                    case Strategies.Constant constant:
                        instance = constant.Target;
                        break;

                    case Strategies.Callback callback:
                        instance = callback.Target();
                        break;

                    case Strategies.TypeCallback typeCallback:
                        var typeFromCallback = typeCallback.Target();
                        instance = _activator.CreateInstanceFor(context, binding.Service, typeFromCallback);
                        break;
                }

                _instancesPerKey[key] = instance;
                return instance;
            }
        }

        string GetKeyFrom(TenantId tenant, Binding binding, Type service)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.Append(tenant);
            stringBuilder.Append("-");
            stringBuilder.Append(binding.Service.AssemblyQualifiedName);
            if( service.IsGenericType ) 
                service.GetGenericArguments().ForEach(_ => stringBuilder.Append($"-{_.AssemblyQualifiedName}"));

            return stringBuilder.ToString();
        }
    }
}
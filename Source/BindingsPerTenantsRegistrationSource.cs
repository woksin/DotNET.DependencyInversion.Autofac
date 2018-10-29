/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Dolittle. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *--------------------------------------------------------------------------------------------*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Autofac;
using Autofac.Core;
using Autofac.Core.Activators.Delegate;
using Autofac.Core.Lifetime;
using Autofac.Core.Registration;
using Autofac.Core.Resolving;
using Dolittle.Collections;
using Dolittle.Execution;
using Dolittle.Lifecycle;
using Dolittle.Reflection;
using Dolittle.Tenancy;

namespace Dolittle.DependencyInversion.Autofac
{
    /// <summary>
    /// Represents a <see cref="IRegistrationSource"/> that deals with 
    /// </summary>
    public class BindingsPerTenantsRegistrationSource : IRegistrationSource
    {
        static List<Binding> _bindings = new List<Binding>();

        static Dictionary<string, object> _instancesPerKey = new Dictionary<string, object>();

        /// <summary>
        /// Gets or sets the <see cref="IContainer"/> to be used
        /// </summary>
        internal static global::Autofac.IContainer Container;


        /// <inheritdoc/>
        public bool IsAdapterForIndividualComponents => false;

        /// <summary>
        /// Add a <see cref="Binding"/> for the registration source to use
        /// </summary>
        /// <param name="binding"><see cref="Binding"/> to add</param>
        public static void AddBinding(Binding binding)
        {
            _bindings.Add(binding);
        }

        /// <inheritdoc/>
        public IEnumerable<IComponentRegistration> RegistrationsFor(Service service, Func<Service, IEnumerable<IComponentRegistration>> registrationAccessor)
        {
            var serviceWithType = service as IServiceWithType;
            if( serviceWithType == null ) return Enumerable.Empty<IComponentRegistration>();


            if (serviceWithType.ServiceType.HasAttribute<SingletonPerTenantAttribute>() &&
                (!HasService(serviceWithType.ServiceType) &&
                !IsGenericAndHasGenericService(serviceWithType.ServiceType)))
            {
                AddBinding(new Binding(serviceWithType.ServiceType, new Strategies.Type(serviceWithType.ServiceType), new Scopes.Transient()));
            }

            if( serviceWithType == null ||
                (!HasService(serviceWithType.ServiceType) &&
                !IsGenericAndHasGenericService(serviceWithType.ServiceType)))
                return Enumerable.Empty<IComponentRegistration>();

            var registration = new ComponentRegistration(
                Guid.NewGuid(),
                new DelegateActivator(serviceWithType.ServiceType, (c, p) => GetOrCreateInstance(c, serviceWithType)),
                new CurrentScopeLifetime(),
                InstanceSharing.None,
                InstanceOwnership.OwnedByLifetimeScope,
                new[] { service },
                new Dictionary<string, object>()
            );
            
            return new[] { registration };
        }

        object GetOrCreateInstance(IComponentContext c, IServiceWithType serviceWithType)
        {
            lock (_instancesPerKey)
            {
                var binding = GetBindingFor(serviceWithType.ServiceType);
                var key = GetKeyFrom(
                    ExecutionContextManager.Current.Tenant,
                    binding,
                    serviceWithType);
                if (_instancesPerKey.ContainsKey(key)) return _instancesPerKey[key];

                object instance = null;
                switch (binding.Strategy)
                {
                    case Strategies.Type type:
                        instance = CreateInstanceFor(c, binding.Service, type.Target);
                        break;

                    case Strategies.Constant constant:
                        instance = constant.Target;
                        break;

                    case Strategies.Callback callback:
                        instance = callback.Target();
                        break;

                    case Strategies.TypeCallback typeCallback:
                        var typeFromCallback = typeCallback.Target();
                        instance = CreateInstanceFor(c, binding.Service, typeFromCallback);
                        break;
                }

                _instancesPerKey[key] = instance;
                return instance;
            }
        }

        object CreateInstanceFor(IComponentContext context, Type service, Type type)
        {
            object instance;
            var constructors = type.GetConstructors().ToArray();
            if (constructors.Length > 1) throw new Exception($"Unable to create instance of '{type.AssemblyQualifiedName}' - more than one constructor");
            var constructor = constructors[0];
            var parameterInstances = constructor.GetParameters().Select(_ => Container.Resolve(_.ParameterType)).ToArray();

            var instanceLookup = context as IInstanceLookup;

            if( service.ContainsGenericParameters ) 
            {
                var genericArguments = instanceLookup.ComponentRegistration.Activator.LimitType.GetGenericArguments();
                var targetType = type.MakeGenericType(genericArguments);
                instance = Activator.CreateInstance(targetType, parameterInstances);
            } 
            else 
            {
                instance = Activator.CreateInstance(type, parameterInstances);
            }
            
            return instance;
        }


        bool HasService(Type service)
        {
            return _bindings.Any(_ => _.Service == service);
        }
        
        bool IsGenericAndHasGenericService(Type service)
        {
            return service.IsGenericType && _bindings.Any(_ => _.Service == service.GetGenericTypeDefinition());
        }

        Binding GetBindingFor(Type service)
        {
            var binding = _bindings.SingleOrDefault(_ => _.Service == service);
            if( binding == null && service.IsGenericType) binding = _bindings.Single(_ => _.Service == service.GetGenericTypeDefinition());
            if( binding == null ) throw new ArgumentException($"Couldn't find a binding for service {service.AssemblyQualifiedName}");
            return binding;
        }

        string GetKeyFrom(TenantId tenant, Binding binding, IServiceWithType service)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.Append(tenant);
            stringBuilder.Append("-");
            stringBuilder.Append(binding.Service.AssemblyQualifiedName);
            if( service.ServiceType.IsGenericType ) 
                service.ServiceType.GetGenericArguments().ForEach(_ => stringBuilder.Append($"-{_.AssemblyQualifiedName}"));

            return stringBuilder.ToString();
        }

        IExecutionContextManager _executionContextManager;

        IExecutionContextManager ExecutionContextManager
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
    }
}
/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Dolittle. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *--------------------------------------------------------------------------------------------*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Autofac;
using Autofac.Builder;
using Autofac.Core;
using Autofac.Features.ResolveAnything;
using doLittle.Assemblies;
using doLittle.Collections;
using doLittle.Execution;
using doLittle.Reflection;

namespace doLittle.DependencyInversion.Autofac
{
    /// <summary>
    /// Represents async implementation of <see cref="ICanProvideContainer"/> specific for Autofac
    /// </summary>
    public class ContainerProvider : ICanProvideContainer
    {
        /// <inheritdoc/>
        public IContainer Provide(IAssemblies assemblies, IBindingCollection bindings)
        {
            var containerBuilder = new ContainerBuilder();
            var allAssemblies = assemblies.GetAll().ToArray();
            containerBuilder.RegisterAssemblyModules(allAssemblies);

            var selfBindingRegistrationSource = new AnyConcreteTypeNotAlreadyRegisteredSource(type => 
                !type.Namespace.StartsWith("Microsoft") &&
                !type.Namespace.StartsWith("System"));

            selfBindingRegistrationSource.RegistrationConfiguration = HandleLifeCycleFor;
            
            containerBuilder.RegisterSource(selfBindingRegistrationSource);

            DiscoverAndRegisterRegistrationSources(containerBuilder, allAssemblies);

            RegisterUpBindingsIntoContainerBuilder(bindings, containerBuilder);

            var container = new Container(containerBuilder.Build());
            return container;
        }

        static void HandleLifeCycleFor(IRegistrationBuilder<object, ConcreteReflectionActivatorData, SingleRegistrationStyle> builder)
        {
            var service = builder.RegistrationData.Services.First();
            if (service is TypedService)
            {
                var typedService = service as TypedService;
                if (typedService.ServiceType.HasAttribute<SingletonAttribute>())builder.SingleInstance();
            }
        }

        static void RegisterUpBindingsIntoContainerBuilder(IBindingCollection bindings, ContainerBuilder containerBuilder)
        {
            bindings.ForEach(binding =>
            {
                if (binding.Service.ContainsGenericParameters)
                {
                    if (binding.Strategy is Strategies.Type)
                    {
                        var registrationBuilder = containerBuilder.RegisterGeneric(((Strategies.Type)binding.Strategy).Target).As(binding.Service);
                        if (binding.Scope is Scopes.Singleton)registrationBuilder = registrationBuilder.SingleInstance();
                    }
                }
                else
                {
                    if (binding.Strategy is Strategies.Type)
                    {
                        var registrationBuilder = containerBuilder.RegisterType(((Strategies.Type)binding.Strategy).Target).As(binding.Service);
                        if (binding.Scope is Scopes.Singleton)registrationBuilder = registrationBuilder.SingleInstance();
                    }
                    else if (binding.Strategy is Strategies.Constant)
                    {
                        containerBuilder.RegisterInstance(((Strategies.Constant)binding.Strategy).Target).As(binding.Service);
                    }
                    else if (binding.Strategy is Strategies.Callback)
                    {
                        var registrationBuilder = containerBuilder.Register((context)=>((Strategies.Callback)binding.Strategy).Target()).As(binding.Service);
                        if (binding.Scope is Scopes.Singleton)registrationBuilder = registrationBuilder.SingleInstance();

                    }
                }
            });
        }

        void DiscoverAndRegisterRegistrationSources(ContainerBuilder containerBuilder, IEnumerable<Assembly> allAssemblies)
        {
            allAssemblies.ForEach(assembly =>
            {
                var registrationSourceProviderTypes = assembly.GetTypes().Where(type => type.HasInterface<ICanProvideRegistrationSources>());
                registrationSourceProviderTypes.ForEach(registrationSourceProviderType =>
                {
                    ThrowIfRegistrationSourceProviderTypeIsMissingDefaultConstructor(registrationSourceProviderType);
                    var registrationSourceProvider = Activator.CreateInstance(registrationSourceProviderType)as ICanProvideRegistrationSources;
                    var registrationSources = registrationSourceProvider.Provide();
                    registrationSources.ForEach(containerBuilder.RegisterSource);
                });
            });
        }

        void ThrowIfRegistrationSourceProviderTypeIsMissingDefaultConstructor(Type type)
        {
            if (!type.HasDefaultConstructor())throw new RegistrationSourceProviderMustHaveADefaultConstructor(type);
        }
    }
}
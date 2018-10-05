/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Dolittle. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *--------------------------------------------------------------------------------------------*/
using System;
using Autofac;
using Autofac.Multitenant;
using Dolittle.Tenancy;

namespace Dolittle.DependencyInversion.Autofac
{
    /// <summary>
    /// Represents an implementation of <see cref="IContainer"/> specific for Autofac
    /// </summary>
    public class Container : IContainer
    {
        readonly global::Autofac.IContainer _container;

        /// <summary>
        /// Initializes a new instance of <see cref="Container"/>
        /// </summary>
        /// <param name="container"></param>
        public Container(global::Autofac.IContainer container)
        {
            var tenantIdentificationStrategy = new TenantIdentificationStrategy(container);
            var mtc =  new MultitenantContainer(tenantIdentificationStrategy, container);
            //mtc.ConfigureTenant(TenantId.Unknown, new ConfigurationActionBuilder().Build());
            _container = container;
        }

        /// <inheritdoc/>
        public T Get<T>()
        {
            return _container.Resolve<T>();
        }

        /// <inheritdoc/>
        public object Get(Type type)
        {
            return _container.Resolve(type);
        }
    }
}
/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Dolittle. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *--------------------------------------------------------------------------------------------*/
using Autofac;
using Autofac.Multitenant;
using Dolittle.Execution;

namespace Dolittle.DependencyInversion.Autofac
{
    /// <summary>
    /// Represents an implementation of <see cref="ITenantIdentificationStrategy"/>
    /// </summary>
    public class TenantIdentificationStrategy : ITenantIdentificationStrategy
    {
        readonly global::Autofac.IContainer _appContainer;
        IExecutionContextManager _executionContextManager;

        /// <summary>
        /// Initialize a new instance of <see cref="TenantIdentificationStrategy"/>
        /// </summary>
        /// <param name="appContainer">The parent application <see cref="global::Autofac.IContainer"/></param>
        public TenantIdentificationStrategy(global::Autofac.IContainer appContainer)
        {
            _appContainer = appContainer;
        }


        /// <inheritdoc/>
        public bool TryIdentifyTenant(out object tenantId)
        {
            try 
            {
                if( _executionContextManager == null ) _executionContextManager = _appContainer.Resolve<IExecutionContextManager>();
                tenantId = _executionContextManager.Current.Tenant;
                return true;
            } catch {}

            tenantId = null;
            return false;
        }
    }
}
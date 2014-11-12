using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MARC.HI.EHRS.SVC.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MARC.HI.EHRS.SVC.Core.Services;
using MARC.HI.EHRS.CR.Persistence.Data;
using MARC.HI.EHRS.CR.Core.Services;
using System.Diagnostics;
using NHapi.Base.Util;

namespace MARC.HI.EHRS.CR.Messaging.PixPdqv2.Test
{
    /// <summary>
    /// Base Host context for test
    /// </summary>
    public class BaseHostContextTest : IServiceProvider
    {
        // Services
        private Dictionary<Type, Object> m_services;

        /// <summary>
        /// Ctor
        /// </summary>
        public BaseHostContextTest()
        {
            this.m_services = new Dictionary<Type, object>()
            {
                { typeof(IDataPersistenceService), new DatabasePersistenceService() { Context = this } },
                { typeof(IClientRegistryMergeService), new DatabaseMergeService() { Context = this } },
                { typeof(ISystemConfigurationService), new TestConfigurationService() }
            };
        }

        /// <summary>
        /// Get a service of the specified type
        /// </summary>
        public object GetService(Type serviceType)
        {
            Object serviceImpl = null;
            if (!this.m_services.TryGetValue(serviceType, out serviceImpl))
                Trace.TraceError("Could not find service {0}", serviceType);
            return serviceImpl;
        }

        /// <summary>
        /// Set request message parameters
        /// </summary>
        protected void SetRequestMessageParams(NHapi.Base.Model.IMessage request)
        {
            Terser setTerser = new Terser(request);
            setTerser.Set("/MSH-7", DateTime.Now.ToString("yyyyMMddHHmmss"));
            setTerser.Set("/EVN-2", DateTime.Today.ToString("yyyyMMdd"));

        }
    }
}

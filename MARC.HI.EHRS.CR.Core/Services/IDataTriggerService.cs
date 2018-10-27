using MARC.HI.EHRS.SVC.Core.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MARC.HI.EHRS.CR.Core.Services
{
    /// <summary>
    /// Represents a service that is triggered before and after a data trigger
    /// </summary>
    public interface IDataTriggerService<TData> : IUsesHostContext 
        where TData : IContainer
    {

        /// <summary>
        /// Fired before the data is persisted
        /// </summary>
        TData Persisting(TData data);

        /// <summary>
        /// Fired after data has been persisted
        /// </summary>
        TData Persisted(TData data);

        
    }
}

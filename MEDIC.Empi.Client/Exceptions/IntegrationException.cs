using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MEDIC.Empi.Client.Exceptions
{
    /// <summary>
    /// Integration exception
    /// </summary>
    public class IntegrationException : System.Exception
    {

        /// <summary>
        /// Integration Exception
        /// </summary>
        public IntegrationException()
        {

        }

        /// <summary>
        /// Integration exception
        /// </summary>
        public IntegrationException(String message): base(message)
        {
            
        }

        /// <summary>
        /// Integration exception
        /// </summary>
        public IntegrationException(String message, Exception cause) : base(message, cause)
        {

        }

    }
}

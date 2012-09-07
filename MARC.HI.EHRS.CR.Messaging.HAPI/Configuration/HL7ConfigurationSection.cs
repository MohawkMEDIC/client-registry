using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MARC.HI.EHRS.SVC.Core.DataTypes;

namespace MARC.HI.EHRS.CR.Messaging.HL7.Configuration
{

    /// <summary>
    /// Handler definition
    /// </summary>
    public class HandlerDefinition
    {

        /// <summary>
        /// Handler defn ctor
        /// </summary>
        public HandlerDefinition()
        {
            this.Types = new List<string>();
        }

        /// <summary>
        /// Gets or sets the handler
        /// </summary>
        public IHL7MessageHandler Handler { get; set; }

        /// <summary>
        /// Message types that trigger this (MSH-9)
        /// </summary>
        public List<String> Types { get; set; }
    }

    /// <summary>
    /// Service definition
    /// </summary>
    public class ServiceDefinition
    {

        /// <summary>
        /// Service defn ctor
        /// </summary>
        public ServiceDefinition()
        {
            this.Handlers = new List<HandlerDefinition>();
        }

        /// <summary>
        /// Gets or sets the timeout
        /// </summary>
        public TimeSpan ReceiveTimeout { get; set; }

        /// <summary>
        /// Gets or sets the address of the service
        /// </summary>
        public Uri Address { get; set; }

        /// <summary>
        /// Gets or sets the name of the defintiion
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the handlers
        /// </summary>
        public List<HandlerDefinition> Handlers { get; set; }
    }

    /// <summary>
    /// Configuration section for the PIX handler
    /// </summary>
    public class HL7ConfigurationSection
    {

        /// <summary>
        /// PIX configuration section
        /// </summary>
        public HL7ConfigurationSection()
        {
            this.Services = new List<ServiceDefinition>();
        }

        /// <summary>
        /// The address to which to bind
        /// </summary>
        /// <remarks>A full Uri is required and must be tcp:// or mllp://</remarks>
        public List<ServiceDefinition> Services { get; private set; }


    }
}

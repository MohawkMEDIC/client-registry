using System;

namespace MARC.HI.EHRS.CR.Core.ComponentModel
{
    /// <summary>
    /// Identifies a type of record
    /// </summary>
    [Flags]
    public enum RegistrationEventType
    {
        /// <summary>
        /// Any of the items
        /// </summary>
        Any = Register | Revise | Nullify,
        /// <summary>
        /// No service record
        /// </summary>
        None = 0,
        /// <summary>
        /// Marks an event as "Just a component" this prevents it from appearing in
        /// summary queries
        /// </summary>
        ComponentEvent = 0x01,
        /// <summary>
        /// Marks the event as a notification
        /// </summary>
        Notification = 0x02,
        /// <summary>
        /// Registration of a patient
        /// </summary>
        Register = 0x04,
        /// <summary>
        /// Revise of a patient
        /// </summary>
        Revise = 0x08,
        /// <summary>
        /// Nullify of a person
        /// </summary>
        Nullify = 0x10,

    }
}

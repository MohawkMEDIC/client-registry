using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MARC.HI.EHRS.CR.Core.ComponentModel
{
    /// <summary>
    /// Roles a person can play
    /// </summary>
    public enum PersonRole
    {
        /// <summary>
        /// The person is a patient
        /// </summary>
        PAT = 0x01,
        /// <summary>
        /// The person is related to a patient
        /// </summary>
        PRS = 0x02
    }
}

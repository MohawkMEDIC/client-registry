/**
 * Copyright 2015-2015 Mohawk College of Applied Arts and Technology
 * 
 * Licensed under the Apache License, Version 2.0 (the "License"); you 
 * may not use this file except in compliance with the License. You may 
 * obtain a copy of the License at 
 * 
 * http://www.apache.org/licenses/LICENSE-2.0 
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
 * WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the 
 * License for the specific language governing permissions and limitations under 
 * the License.
 * 
 * User: Justin
 * Date: 22-7-2015
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MARC.Everest.Connectors;
using MARC.HI.EHRS.SVC.Core.Services;
using MARC.HI.EHRS.SVC.Core.DataTypes;
using MARC.HI.EHRS.SVC.Core.Issues;

namespace MARC.HI.EHRS.CR.Core.Data
{
    /// <summary>
    /// Result detail related to a persistence problem
    /// </summary>
    public class PersistenceResultDetail : ResultDetail
    {
        /// <summary>
        /// Create a new instance of the invalid state transition detail
        /// </summary>
        internal PersistenceResultDetail(ResultDetailType type, string message, Exception innerException)
            : base(type, message, innerException)
        { }
    }


    /// <summary>
    /// Patient was not found result detail
    /// </summary>
    public class PatientNotFoundResultDetail : ResultDetail
    {
        //public PatientNotFoundResultDetail(ILocalizationService locale) : base(ResultDetailType.Error, locale.GetString("DTPE006"), "QPD^1^3^1^1") { }
        public PatientNotFoundResultDetail(ILocalizationService locale) : base(ResultDetailType.Error, locale.GetString("DTPE006"), null, null) { }

    }

    /// <summary>
    /// Patient was not found result detail
    /// </summary>
    public class UnrecognizedPatientDomainResultDetail : ResultDetail
    {
        //public UnrecognizedPatientDomainResultDetail(ILocalizationService locale) : base(ResultDetailType.Error, locale.GetString("DBCF00C"), "QPD^1^3^1^4") { }
        public UnrecognizedPatientDomainResultDetail(ILocalizationService locale) : base(ResultDetailType.Error, locale.GetString("DBCF00C"), null, null) { }

    }

    /// <summary>
    /// Patient was not found result detail
    /// </summary>
    public class UnrecognizedTargetDomainResultDetail : ResultDetail
    {
        //public UnrecognizedTargetDomainResultDetail(ILocalizationService locale) : base(ResultDetailType.Error, locale.GetString("DBCF00C"), "QPD^1^4^") { }
        public UnrecognizedTargetDomainResultDetail(ILocalizationService locale, String location) : base(ResultDetailType.Error, locale.GetString("DBCF00C"), location) { }

    }

    /// <summary>
    /// Unsupported response mode has been detected
    /// </summary>
    public class UnsupportedResponseModeResultDetail : ResultDetail
    {
        public UnsupportedResponseModeResultDetail(string setMode) :
            base(ResultDetailType.Error, String.Format("'{0}' is an unsupported response mode. This repository only supports Immediate mode transactions", MARC.Everest.Connectors.Util.ToWireFormat(setMode)), (Exception)null) { }
    }

    /// <summary>
    /// Unsupported processing mode result detail
    /// </summary>
    public class UnsupportedProcessingModeResultDetail : ResultDetail
    {
        public UnsupportedProcessingModeResultDetail(string setMode) :
            base(ResultDetailType.Error, String.Format("'{0}' is an unsupported processing mode. This repository only supports Production and Debugging mode transactions", MARC.Everest.Connectors.Util.ToWireFormat(setMode)), (Exception)null) { }
    }

    /// <summary>
    /// Unsupported version identifier or profile identifier was found
    /// </summary>
    public class UnsupportedVersionResultDetail : ResultDetail
    {
        public UnsupportedVersionResultDetail(string message) :
            base(ResultDetailType.Error, message, (Exception)null) { }
    }

    /// <summary>
    /// Unrecognized sender
    /// </summary>
    public class UnrecognizedSenderResultDetail : ResultDetail
    {

        public UnrecognizedSenderResultDetail(DomainIdentifier sender) :
            base(ResultDetailType.Error, String.Format("'{1}^^^&{0}&ISO' is not a known solicitor", sender.Domain, sender.Identifier), (Exception)null)
        { }

    }

    /// <summary>
    /// Detected issue event detail
    /// </summary>
    public class DetectedIssueResultDetail : ResultDetail
    {
        /// <summary>
        /// Gets the issue which triggered this result detail
        /// </summary>
        public DetectedIssue Issue { get; private set; }

        public DetectedIssueResultDetail(string message, DetectedIssue issue) :
            this(ResultDetailType.Error, message, (Exception)null, issue) { 
        }

        public DetectedIssueResultDetail(ResultDetailType type, string message, string location, DetectedIssue issue)
            : base(type, message, location) {
                this.Issue = issue;
        }

        public DetectedIssueResultDetail(ResultDetailType type, string message, Exception exception, DetectedIssue issue)
            : base(type, message, exception) {
                this.Issue = issue;
            }
    }
}

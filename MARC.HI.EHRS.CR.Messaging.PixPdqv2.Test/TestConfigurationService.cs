using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MARC.HI.EHRS.SVC.Core.Services;
using MARC.HI.EHRS.SVC.Core.DataTypes;
using MARC.HI.EHRS.CR.Persistence.Data;

namespace MARC.HI.EHRS.CR.Messaging.PixPdqv2.Test
{
    /// <summary>
    /// Test system configuration service
    /// </summary>
    public class TestConfigurationService : ISystemConfigurationService
    {
        private OidRegistrar m_oidRegistrar;

        /// <summary>
        /// Test configuration service
        /// </summary>
        public TestConfigurationService()
        {
            this.m_oidRegistrar = new OidRegistrar();
            this.m_oidRegistrar.Register(ClientRegistryOids.CLIENT_CRID, "1.2.3.4.5.6.7.8", "ECID", "http://foo.bar").Attributes.Add(new KeyValuePair<string, string>("AssigningAuthorityName", "ECID"));
            this.m_oidRegistrar.Register(ClientRegistryOids.CLIENT_VERSION_CRID, "1.2.3.4.5.6.7.9", "Client Version IDs", "http://foo.bar");
            this.m_oidRegistrar.Register(ClientRegistryOids.DEVICE_CRID, "1.2.3.4.5.6.7.0", "Device IDS", "http://foo.bar");
            this.m_oidRegistrar.Register(ClientRegistryOids.EVENT_OID, "1.2.3.4.5.6.7.1", "Event IDs", "http://foo.bar");
            this.m_oidRegistrar.Register(ClientRegistryOids.LOCATION_CRID, "1.2.3.4.5.6.7.2", "Location IDs", "http://foo.bar");
            this.m_oidRegistrar.Register(ClientRegistryOids.PROVIDER_CRID, "1.2.3.4.5.6.7.3", "Provider IDs", "http://foo.bar");
            this.m_oidRegistrar.Register(ClientRegistryOids.REGISTRATION_EVENT, "1.2.3.4.5.6.7.4", "Registration IDs", "http://foo.bar");
            this.m_oidRegistrar.Register(ClientRegistryOids.REGISTRATION_EVENT_VERSION, "1.2.3.4.5.6.7.5", "Registration Event Version IDs", "http://foo.bar");
            this.m_oidRegistrar.Register(ClientRegistryOids.RELATIONSHIP_OID, "1.2.3.4.5.6.7.6", "Relationship", "http://foo.bar").Attributes.Add(new KeyValuePair<string, string>("AssigningAuthorityName", "ECID_RELATION"));

            // From test configuration
            // TEST domain
            var testDomain = this.m_oidRegistrar.Register("TEST", "2.16.840.1.113883.3.72.5.9.1", "Test", "http://foo.bar");
            testDomain.Attributes.Add(new KeyValuePair<string, string>("AssigningAuthorityName", "TEST"));
            testDomain.Attributes.Add(new KeyValuePair<string, string>("AssigningDevFacility", "TEST_HARNESS|TEST"));

            testDomain = this.m_oidRegistrar.Register("TEST_A", "2.16.840.1.113883.3.72.5.9.2", "Test A", "http://foo.bar");
            testDomain.Attributes.Add(new KeyValuePair<string, string>("AssigningAuthorityName", "TEST_A"));
            testDomain.Attributes.Add(new KeyValuePair<string, string>("AssigningDevFacility", "TEST_HARNESS_A|TEST"));

            testDomain = this.m_oidRegistrar.Register("TEST_B", "2.16.840.1.113883.3.72.5.9.3", "Test B", "http://foo.bar");
            testDomain.Attributes.Add(new KeyValuePair<string, string>("AssigningAuthorityName", "TEST_B"));
            testDomain.Attributes.Add(new KeyValuePair<string, string>("AssigningDevFacility", "TEST_HARNESS_B|TEST"));

            testDomain = this.m_oidRegistrar.Register("NID", "2.16.840.1.113883.3.72.5.9.9", "National ID", "http://foo.bar");
            testDomain.Attributes.Add(new KeyValuePair<string, string>("AssigningAuthorityName", "NID"));
            testDomain.Attributes.Add(new KeyValuePair<string, string>("AssigningDevFacility", "NID_AUTH|TEST"));

        }

        /// <summary>
        /// Get custodianship information
        /// </summary>
        public SVC.Core.DataTypes.CustodianshipData Custodianship
        {
            get
            {
                return new SVC.Core.DataTypes.CustodianshipData()
                {
                    Id = new DomainIdentifier() { Domain = "1.2.3.4.5.6" },
                    Name = "Unit Test"
                };
            }
        }

        /// <summary>
        /// Device identifier
        /// </summary>
        public string DeviceIdentifier
        {
            get { return "1.2.3.4.5.6"; }
        }

        /// <summary>
        /// Device name
        /// </summary>
        public string DeviceName
        {
            get { return "UNIT_TEST"; }
        }

        /// <summary>
        /// True if registered device
        /// </summary>
        public bool IsRegisteredDevice(SVC.Core.DataTypes.DomainIdentifier deviceId)
        {
            switch (deviceId.AssigningAuthority)
            {
                case "TEST_HARNESS":
                case "TEST_HARNESS_A":
                case "TEST_HARNESS_B":
                case "NID_AUTH":
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Juristictional data
        /// </summary>
        public SVC.Core.DataTypes.Jurisdiction JurisdictionData
        {
            get { return null; }
        }

        /// <summary>
        /// OID Registrar
        /// </summary>
        public IOidRegistrarService OidRegistrar
        {
            get { return this.m_oidRegistrar; }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.IO;
using System.Xml.Xsl;
using System.Xml;
using System.Web;
using System.Diagnostics;

namespace ClientRegistryAdmin.Models
{
    /// <summary>
    /// Patient record match
    /// </summary>
    public class PatientMatch
    {
        private string originalXml = null;

        /// <summary>
        /// Name
        /// </summary>
        public String GivenName { get; set; }

        /// <summary>
        /// Name
        /// </summary>
        public String FamilyName { get; set; }

        /// <summary>
        /// Gets or sets the dob
        /// </summary>
        public DateTime DateOfBirth { get; set; }

        /// <summary>
        /// Gets or sets the addr
        /// </summary>
        public String Address { get; set; }

        /// <summary>
        /// Gets or sets the gender
        /// </summary>
        public String Gender { get; set; }

        /// <summary>
        /// Gets the ECID
        /// </summary>
        public String Id { get; set; }

        /// <summary>
        /// Get the confidence
        /// </summary>
        public int Confidence { get; set; }

        /// <summary>
        /// Gets or sets the mother's identifier
        /// </summary>
        public String MothersId { get; set; }

        /// <summary>
        /// Gets or sets the mother's name
        /// </summary>
        public String MothersName { get; set; }

        /// <summary>
        /// City
        /// </summary>
        public string City { get; set; }

        /// <summary>
        /// Precinct
        /// </summary>
        public string Precinct { get; set; }

        /// <summary>
        /// Additional locator
        /// </summary>
        public String Locator { get; set; }

        /// <summary>
        /// State
        /// </summary>
        public string State { get; set; }

        /// <summary>
        /// Other identifiers
        /// </summary>
        public List<KeyValuePair<String, String>> OtherIds { get; set; }

        /// <summary>
        /// Registration id
        /// </summary>
        public decimal RegistrationId { get; set; }

        /// <summary>
        /// Original data
        /// </summary>
        public ClientRegistryAdminService.Person OriginalData { get; set; }

        /// <summary>
        /// Original XML
        /// </summary>
        public String OriginalXml
        {
            get
            {
                if (originalXml != null)
                    return originalXml;

                XmlSerializer xsz = new XmlSerializer(typeof(ClientRegistryAdmin.ClientRegistryAdminService.HealthServiceRecordComponent));
                using (MemoryStream ms = new MemoryStream())
                {
                    xsz.Serialize(ms, this.HealthServiceEvent);
                    return System.Text.Encoding.UTF8.GetString(ms.ToArray());
                }
            }
        }

        /// <summary>
        /// County
        /// </summary>
        public string County { get; set; }

        /// <summary>
        /// Country
        /// </summary>
        public string Country { get; set; }

        /// <summary>
        /// Postal code
        /// </summary>
        public string PostCode { get; set; }

        /// <summary>
        /// Census tract
        /// </summary>
        public string CensusTract { get; set; }

        /// <summary>
        /// Event
        /// </summary>
        public ClientRegistryAdminService.HealthServiceRecordContainer HealthServiceEvent { get; set; }

        /// <summary>
        /// Status
        /// </summary>
        public String Status { get; set; }

        /// <summary>
        /// Version identifier
        /// </summary>
        public String VersionId { get; set; }

        /// <summary>
        /// Get the original XML as a nice HTML string
        /// </summary>
        public HtmlString OriginalHtml
        {
            get
            {
                var baseUrl = System.Web.HttpContext.Current.Server.MapPath("~/Content/XSL/RegistrationEvent.xslt");
                XslCompiledTransform xslt = new XslCompiledTransform();
                try
                {
                    xslt.Load(baseUrl);
                }
                catch
                {
                    return new HtmlString(this.OriginalXml);
                }

                try
                {
                    // Transform
                    StringWriter sw = new StringWriter();
                    using (XmlReader rdr = XmlReader.Create(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(this.OriginalXml))))
                    {
                        using (XmlWriter xw = XmlWriter.Create(sw, new XmlWriterSettings() { ConformanceLevel = ConformanceLevel.Fragment }))
                            xslt.Transform(rdr, xw);
                    }

                    return new HtmlString(sw.ToString());
                }
                catch (Exception e)
                {
                    Trace.WriteLine(e.ToString());
                    return new HtmlString("This data is not in a renderable format");
                }
            }
        }
    }
}

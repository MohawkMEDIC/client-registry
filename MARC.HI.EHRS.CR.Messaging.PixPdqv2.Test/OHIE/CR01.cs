using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NHapi.Base.Model;
using MARC.HI.EHRS.CR.Messaging.HL7.TransportProtocol;
using MARC.HI.EHRS.CR.Messaging.PixPdqv2.Test.Util;
using NHapi.Base.Util;

namespace MARC.HI.EHRS.CR.Messaging.PixPdqv2.Test
{
    /// <summary>
    /// OpenHIE Test 01 - Patient Identity Feed – Invalid register patient message
    /// </summary>
    [TestClass]
    public class CR01 : BaseHostContextTest
    {
        // Handler for PIX
        private PixHandler m_handler;

        /// <summary>
        /// Ctor
        /// </summary>
        public CR01()
        {
            this.m_handler = new PixHandler() { Context = this };
        }


        /// <summary>
        /// Test harness sends ADT^A01 message where the CX.4 of the PID is missing.
        /// </summary>
        [TestMethod]
        public void Step10()
        {
            IMessage request = ResourceUtil.GetRequestMessage("OHIE-CR-01-10");
            base.SetRequestMessageParams(request);
            IMessage response = this.m_handler.HandleMessage(new Hl7MessageReceivedEventArgs(request, new Uri("http://anonymous"), new Uri("llp://test"), DateTime.Now));
            Terser assertTerser = new Terser(response);
            Assert.IsTrue(new List<String>(){ "AE", "AR" }.Contains(assertTerser.Get("/MSA-1")), "Receiver did not respond with AE or AR");

            // Assertion
        }
    }
}

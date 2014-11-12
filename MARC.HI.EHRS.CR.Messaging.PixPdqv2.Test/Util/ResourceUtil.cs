using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NHapi.Base.Model;
using System.IO;
using NHapi.Base.Parser;

namespace MARC.HI.EHRS.CR.Messaging.PixPdqv2.Test.Util
{
    public class ResourceUtil
    {

        /// <summary>
        /// Get a requestmessage
        /// </summary>
        /// <param name="requestName">The name of the request message</param>
        /// <returns>The parsed message</returns>
        public static IMessage GetRequestMessage(String requestName)
        {
            using(Stream resourceStream = typeof(ResourceUtil).Assembly.GetManifestResourceStream(String.Format("MARC.HI.EHRS.CR.Messaging.PixPdqv2.Test.Resources.{0}.txt", requestName)))
                using(StreamReader sr = new StreamReader(resourceStream))
                    return new PipeParser().Parse(sr.ReadToEnd());
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Security.Cryptography.X509Certificates;
using System.Reflection;

namespace MARC.HI.EHRS.CR.Messaging.Admin.Configuration.UI
{
    /// <summary>
    /// HTTP SSL Tool
    /// </summary>
    /// <remarks>Class from http://www.pinvoke.net/default.aspx/httpapi/HttpSetServiceConfiguration.html</remarks>
    public class HttpSslTool
    {

        #region PInvoke

        [DllImport("httpapi.dll", SetLastError = true)]
        public static extern uint HttpInitialize(
            HTTPAPI_VERSION Version,
            uint Flags,
            IntPtr pReserved);

        [DllImport("httpapi.dll", SetLastError = true)]
        static extern uint HttpSetServiceConfiguration(
             IntPtr ServiceIntPtr,
             HTTP_SERVICE_CONFIG_ID ConfigId,
             IntPtr pConfigInformation,
             int ConfigInformationLength,
             IntPtr pOverlapped);

        [DllImport("httpapi.dll", SetLastError = true)]
        static extern uint HttpDeleteServiceConfiguration(
            IntPtr ServiceIntPtr,
            HTTP_SERVICE_CONFIG_ID ConfigId,
            IntPtr pConfigInformation,
            int ConfigInformationLength,
            IntPtr pOverlapped);

        [DllImport("httpapi.dll", SetLastError = true)]
        public static extern uint HttpTerminate(
            uint Flags,
            IntPtr pReserved);

        enum HTTP_SERVICE_CONFIG_ID
        {
            HttpServiceConfigIPListenList = 0,
            HttpServiceConfigSSLCertInfo,
            HttpServiceConfigUrlAclInfo,
            HttpServiceConfigMax
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct HTTP_SERVICE_CONFIG_IP_LISTEN_PARAM
        {
            public ushort AddrLength;
            public IntPtr pAddress;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct HTTP_SERVICE_CONFIG_SSL_SET
        {
            public HTTP_SERVICE_CONFIG_SSL_KEY KeyDesc;
            public HTTP_SERVICE_CONFIG_SSL_PARAM ParamDesc;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct HTTP_SERVICE_CONFIG_SSL_KEY
        {
            public IntPtr pIpPort;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        struct HTTP_SERVICE_CONFIG_SSL_PARAM
        {
            public int SslHashLength;
            public IntPtr pSslHash;
            public Guid AppId;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string pSslCertStoreName;
            public uint DefaultCertCheckMode;
            public int DefaultRevocationFreshnessTime;
            public int DefaultRevocationUrlRetrievalTimeout;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string pDefaultSslCtlIdentifier;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string pDefaultSslCtlStoreName;
            public uint DefaultFlags;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 2)]
        public struct HTTPAPI_VERSION
        {
            public ushort HttpApiMajorVersion;
            public ushort HttpApiMinorVersion;

            public HTTPAPI_VERSION(ushort majorVersion, ushort minorVersion)
            {
                HttpApiMajorVersion = majorVersion;
                HttpApiMinorVersion = minorVersion;
            }
        }

        #endregion

        #region Constants

        public const uint HTTP_INITIALIZE_CONFIG = 0x00000002;
        public const uint HTTP_SERVICE_CONFIG_SSL_FLAG_USE_DS_MAPPER = 0x00000001;
        public const uint HTTP_SERVICE_CONFIG_SSL_FLAG_NEGOTIATE_CLIENT_CERT = 0x00000002;
        public const uint HTTP_SERVICE_CONFIG_SSL_FLAG_NO_RAW_FILTER = 0x00000004;
        private static int NOERROR = 0;
        private static int ERROR_ALREADY_EXISTS = 183;

        #endregion

        #region Public methods

        /// <summary>
        /// Create the parameter for the configuration operation
        /// </summary>
        private static HTTP_SERVICE_CONFIG_SSL_SET CreateParameter(IPAddress ipAddress, int port, byte[] hash, StoreName store)
        {
            HTTP_SERVICE_CONFIG_SSL_SET configSslSet = new HTTP_SERVICE_CONFIG_SSL_SET();
            HTTP_SERVICE_CONFIG_SSL_KEY httpServiceConfigSslKey = new HTTP_SERVICE_CONFIG_SSL_KEY();
            HTTP_SERVICE_CONFIG_SSL_PARAM configSslParam = new HTTP_SERVICE_CONFIG_SSL_PARAM();

            IPEndPoint ipEndPoint = new IPEndPoint(ipAddress, port);
            // serialize the endpoint to a SocketAddress and create an array to hold the values.  Pin the array.
            SocketAddress socketAddress = ipEndPoint.Serialize();
            byte[] socketBytes = new byte[socketAddress.Size];
            GCHandle handleSocketAddress = GCHandle.Alloc(socketBytes, GCHandleType.Pinned);
            // Should copy the first 16 bytes (the SocketAddress has a 32 byte buffer, the size will only be 16,
            //which is what the SOCKADDR accepts
            for (int i = 0; i < socketAddress.Size; ++i)
            {
                socketBytes[i] = socketAddress[i];
            }

            httpServiceConfigSslKey.pIpPort = handleSocketAddress.AddrOfPinnedObject();

            GCHandle handleHash = GCHandle.Alloc(hash, GCHandleType.Pinned);
            configSslParam.AppId = new Guid((Assembly.GetEntryAssembly().GetCustomAttributes(typeof(GuidAttribute), false)[0] as GuidAttribute).Value);
            configSslParam.DefaultCertCheckMode = 0;
            configSslParam.DefaultFlags = HTTP_SERVICE_CONFIG_SSL_FLAG_NEGOTIATE_CLIENT_CERT;
            configSslParam.DefaultRevocationFreshnessTime = 0;
            configSslParam.DefaultRevocationUrlRetrievalTimeout = 0;
            configSslParam.pSslCertStoreName = store.ToString();
            configSslParam.pSslHash = handleHash.AddrOfPinnedObject();
            configSslParam.SslHashLength = hash.Length;
            configSslSet.ParamDesc = configSslParam;
            configSslSet.KeyDesc = httpServiceConfigSslKey;

            return configSslSet;
        }

        /// <summary>
        /// Remove a certificate binding
        /// </summary>
        public static void RemoveCertificate(IPAddress ipAddress, int port, byte[] hash, StoreName store, StoreLocation location)
        {
            uint retVal = (uint)NOERROR; // NOERROR = 0
            HTTPAPI_VERSION httpApiVersion = new HTTPAPI_VERSION(1, 0);
            retVal = HttpInitialize(httpApiVersion, HTTP_INITIALIZE_CONFIG, IntPtr.Zero);
            if ((uint)NOERROR == retVal)
            {

                var configSslSet = CreateParameter(ipAddress, port, hash, store);
                IntPtr pInputConfigInfo = Marshal.AllocCoTaskMem(Marshal.SizeOf(typeof(HTTP_SERVICE_CONFIG_SSL_SET)));
                Marshal.StructureToPtr(configSslSet, pInputConfigInfo, false);

                retVal = HttpDeleteServiceConfiguration(IntPtr.Zero, HTTP_SERVICE_CONFIG_ID.HttpServiceConfigSSLCertInfo, pInputConfigInfo, Marshal.SizeOf(configSslSet), IntPtr.Zero);

                Marshal.FreeCoTaskMem(pInputConfigInfo);
                HttpTerminate(HTTP_INITIALIZE_CONFIG, IntPtr.Zero);
            }

            if ((uint)NOERROR != retVal)
            {
                throw new Win32Exception(Convert.ToInt32(retVal));
            }

        }

        /// <summary>
        /// Set certificate binding
        /// </summary>
        public static void BindCertificate(IPAddress ipAddress, int port, byte[] hash, StoreName store, StoreLocation location)
        {
            uint retVal = (uint)NOERROR; // NOERROR = 0
            HTTPAPI_VERSION httpApiVersion = new HTTPAPI_VERSION(1, 0);
            retVal = HttpInitialize(httpApiVersion, HTTP_INITIALIZE_CONFIG, IntPtr.Zero);
            if ((uint)NOERROR == retVal)
            {

                var configSslSet = CreateParameter(ipAddress, port, hash, store);
                IntPtr pInputConfigInfo = Marshal.AllocCoTaskMem(Marshal.SizeOf(typeof(HTTP_SERVICE_CONFIG_SSL_SET)));
                Marshal.StructureToPtr(configSslSet, pInputConfigInfo, false);

                retVal = HttpSetServiceConfiguration(IntPtr.Zero,
                    HTTP_SERVICE_CONFIG_ID.HttpServiceConfigSSLCertInfo,
                    pInputConfigInfo,
                    Marshal.SizeOf(configSslSet),
                    IntPtr.Zero);

                if ((uint)ERROR_ALREADY_EXISTS == retVal)  // ERROR_ALREADY_EXISTS = 183
                {
                    retVal = HttpDeleteServiceConfiguration(IntPtr.Zero, HTTP_SERVICE_CONFIG_ID.HttpServiceConfigSSLCertInfo, pInputConfigInfo, Marshal.SizeOf(configSslSet), IntPtr.Zero);

                    if ((uint)NOERROR == retVal)
                        retVal = HttpSetServiceConfiguration(IntPtr.Zero, HTTP_SERVICE_CONFIG_ID.HttpServiceConfigSSLCertInfo, pInputConfigInfo, Marshal.SizeOf(configSslSet), IntPtr.Zero);
                }

                Marshal.FreeCoTaskMem(pInputConfigInfo);
                HttpTerminate(HTTP_INITIALIZE_CONFIG, IntPtr.Zero);
            }

            if ((uint)NOERROR != retVal)
            {
                throw new Win32Exception(Convert.ToInt32(retVal));
            }
        }

        #endregion

    }
}

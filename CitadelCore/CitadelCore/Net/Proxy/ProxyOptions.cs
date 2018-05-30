using CitadelCore.Net.Http;
using System;
using System.Collections.Generic;
using System.Text;

namespace CitadelCore.Net.Proxy
{
    public class ProxyOptions
    {
        /// <summary>
        /// The firewall check callback. Used to allow the user to determine if a binary should have
        /// its associated traffic pushed through the filter or not.
        /// </summary>
        public FirewallCheckCallback FirewallCheckCallback { get; set; }

        /// <summary>
        /// Message begin callback enables users to inspect and filter messages immediately after
        /// they begin. Users also have the power to direct how the proxy will continue to handle the
        /// overall transaction that this message belongs to.
        /// </summary>
        public MessageBeginCallback MessageBeginCallback { get; set; }

        /// <summary>
        /// Message end callback enables users to inspect and filter messages once they have completed. 
        /// </summary>
        public MessageEndCallback MessageEndCallback { get; set; }

        /// <summary>
        /// This gets called by the handler function so that the user can provide a custom response.
        /// </summary>
        public BadCertificateCallback BadCertificateCallback { get; set; }

        /// <summary>
        /// Provides the user with the opportunity to implement a custom certificate exemption store.
        /// </summary>
        public ICertificateExemptions CertificateExemptions { get; set; }
    }
}

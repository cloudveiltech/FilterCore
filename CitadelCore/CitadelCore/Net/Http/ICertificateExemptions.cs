/*
* Copyright © 2018 CloudVeil Technology, Inc.
* This Source Code Form is subject to the terms of the Mozilla Public
* License, v. 2.0. If a copy of the MPL was not distributed with this
* file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace CitadelCore.Net.Http
{
    /// <summary>
    /// Implement this type in your application to support SSL exemptions. Initialize this
    /// with your particular new ProxyServer() call.
    /// </summary>
    public interface ICertificateExemptions
    {
        // I couldn't figure out how to bridge the gap between the Handle() function and the static ValidateCertificate.
        // This is the best solution I could come up with for now.

        /// <summary>
        /// This function gets called whenever a bad certificate is encountered by ValidateCertificate.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="certificate"></param>
        void AddExemptionRequest(HttpWebRequest request, X509Certificate certificate);

        /// <summary>
        /// This function gets called by ValidateCertificate to see if a particular certificate is approved for an exemption.
        /// </summary>
        /// <param name="host"></param>
        /// <param name="certificate"></param>
        /// <returns></returns>
        bool IsExempted(HttpWebRequest request, X509Certificate certificate);
    }
}

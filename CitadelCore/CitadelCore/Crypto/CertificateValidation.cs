/*
* Copyright © 2017 CloudVeil Technology, Inc.
* This Source Code Form is subject to the terms of the Mozilla Public
* License, v. 2.0. If a copy of the MPL was not distributed with this
* file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

/*
 * Parts of this code came from https://github.com/Microsoft/referencesource
 * 
 */
/*using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace CitadelCore.Crypto
{
    static class HttpTransportSecurityHelpers
    {
        static Dictionary<string, int> targetNameCounter = new Dictionary<string, int>();

        public static bool AddIdentityMapping(Uri via, EndpointAddress target)
        {
            string key = via.AbsoluteUri;
            string value;
            EndpointIdentity identity = target.Identity;

            if (identity != null && !(identity is X509CertificateEndpointIdentity))
            {
                value = SecurityUtils.GetSpnFromIdentity(identity, target);
            }
            else
            {
                value = SecurityUtils.GetSpnFromTarget(target);
            }

            lock (targetNameCounter)
            {
                int refCount = 0;
                if (targetNameCounter.TryGetValue(key, out refCount))
                {
                    if (!AuthenticationManager.CustomTargetNameDictionary.ContainsKey(key)
                        || AuthenticationManager.CustomTargetNameDictionary[key] != value)
                    {
                        throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(
                            new InvalidOperationException(SR.GetString(SR.HttpTargetNameDictionaryConflict, key, value)));
                    }
                    targetNameCounter[key] = refCount + 1;
                }
                else
                {
                    if (AuthenticationManager.CustomTargetNameDictionary.ContainsKey(key)
                        && AuthenticationManager.CustomTargetNameDictionary[key] != value)
                    {
                        throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(
                            new InvalidOperationException(SR.GetString(SR.HttpTargetNameDictionaryConflict, key, value)));
                    }

                    AuthenticationManager.CustomTargetNameDictionary[key] = value;
                    targetNameCounter.Add(key, 1);
                }
            }

            return true;
        }

        public static void RemoveIdentityMapping(Uri via, EndpointAddress target, bool validateState)
        {
            string key = via.AbsoluteUri;
            string value;
            EndpointIdentity identity = target.Identity;

            if (identity != null && !(identity is X509CertificateEndpointIdentity))
            {
                value = SecurityUtils.GetSpnFromIdentity(identity, target);
            }
            else
            {
                value = SecurityUtils.GetSpnFromTarget(target);
            }

            lock (targetNameCounter)
            {
                int refCount = targetNameCounter[key];
                if (refCount == 1)
                {
                    targetNameCounter.Remove(key);
                }
                else
                {
                    targetNameCounter[key] = refCount - 1;
                }

                if (validateState)
                {
                    if (!AuthenticationManager.CustomTargetNameDictionary.ContainsKey(key)
                        || AuthenticationManager.CustomTargetNameDictionary[key] != value)
                    {
                        throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(
                            new InvalidOperationException(SR.GetString(SR.HttpTargetNameDictionaryConflict, key, value)));
                    }
                }
            }
        }

        static Dictionary<HttpWebRequest, string> serverCertMap = new Dictionary<HttpWebRequest, string>();
        static RemoteCertificateValidationCallback chainedServerCertValidationCallback = null;
        static bool serverCertValidationCallbackInstalled = false;

        public static void AddServerCertMapping(HttpWebRequest request, EndpointAddress to)
        {
            Fx.Assert(request.RequestUri.Scheme == Uri.UriSchemeHttps,
                "Wrong URI scheme for AddServerCertMapping().");
            X509CertificateEndpointIdentity remoteCertificateIdentity = to.Identity as X509CertificateEndpointIdentity;
            if (remoteCertificateIdentity != null)
            {
                // The following condition should have been validated when the channel was created.
                Fx.Assert(remoteCertificateIdentity.Certificates.Count <= 1,
                    "HTTPS server certificate identity contains multiple certificates");
                AddServerCertMapping(request, remoteCertificateIdentity.Certificates[0].Thumbprint);
            }
        }

        static void AddServerCertMapping(HttpWebRequest request, string thumbprint)
        {
            lock (serverCertMap)
            {
                if (!serverCertValidationCallbackInstalled)
                {
                    chainedServerCertValidationCallback = ServicePointManager.ServerCertificateValidationCallback;
                    ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(
                        OnValidateServerCertificate);
                    serverCertValidationCallbackInstalled = true;
                }

                serverCertMap.Add(request, thumbprint);
            }
        }

        static bool OnValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain,
            SslPolicyErrors sslPolicyErrors)
        {
            HttpWebRequest request = sender as HttpWebRequest;
            if (request != null)
            {
                string thumbprint;
                lock (serverCertMap)
                {
                    serverCertMap.TryGetValue(request, out thumbprint);
                }
                if (thumbprint != null)
                {
                    try
                    {
                        ValidateServerCertificate(certificate, thumbprint);
                    }
                    catch (SecurityNegotiationException e)
                    {
                        DiagnosticUtility.TraceHandledException(e, TraceEventType.Information);
                        return false;
                    }
                }
            }

            if (chainedServerCertValidationCallback == null)
            {
                return (sslPolicyErrors == SslPolicyErrors.None);
            }
            else
            {
                return chainedServerCertValidationCallback(sender, certificate, chain, sslPolicyErrors);
            }
        }

        public static void RemoveServerCertMapping(HttpWebRequest request)
        {
            lock (serverCertMap)
            {
                serverCertMap.Remove(request);
            }
        }

        static void ValidateServerCertificate(X509Certificate certificate, string thumbprint)
        {
            string certHashString = certificate.GetCertHashString();
            if (!thumbprint.Equals(certHashString))
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(
                    new SecurityNegotiationException(SR.GetString(SR.HttpsServerCertThumbprintMismatch,
                    certificate.Subject, certHashString, thumbprint)));
            }
        }
    }
}
*/
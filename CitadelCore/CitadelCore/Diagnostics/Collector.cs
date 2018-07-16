using System;
using System.Collections.Generic;
using System.Text;

namespace CitadelCore.Diagnostics
{
    public delegate void SessionReportedHandler(DiagnosticsWebSession webSession);
    public delegate void BadSslReportedHandler(BadSslReport badSslReport);

    /// <summary>
    /// CitadelCore.Net.FilterHttpResponseHandler and CitadelCore.Net.FilterWebsocketHandler will use this class to report their
    /// diagnostics data.
    /// </summary>
    public static class Collector
    {
        public static bool IsDiagnosticsEnabled { get; set; }

        public static event SessionReportedHandler OnSessionReported;
        public static event BadSslReportedHandler OnBadSsl;

        public static void ReportSession(DiagnosticsWebSession webSession)
        {
            if (IsDiagnosticsEnabled)
            {
                OnSessionReported?.Invoke(webSession);
            }
        }

        public static void ReportBadSsl(BadSslReport report)
        {
            if (IsDiagnosticsEnabled)
            {
                OnBadSsl?.Invoke(report);
            }
        }
    }

    public class BadSslReport
    {
        public string Host { get; set; }
        public Uri RequestUri { get; set; }
    }

    /// <summary>
    /// This is the main diagnostics class that the handlers will fill out.
    /// </summary>
    public class DiagnosticsWebSession
    {
        public DateTime DateStarted { get; set; }
        public DateTime DateEnded { get; set; }
        
        public byte[] ClientRequestBody { get; set; }
        public byte[] ServerRequestBody { get; set; }

        public string ClientRequestHeaders { get; set; }
        public string ServerRequestHeaders { get; set; }

        public int StatusCode { get; set; }

        public string ClientRequestUri { get; set; }
        public string ServerRequestUri { get; set; }

        public string ServerResponseHeaders { get; set; }
        public byte[] ServerResponseBody { get; set; }
    }
}

using System;

namespace VsAutoDeploy
{
    internal static class GuidList
    {
        public const string guidVsAutoDeployPkgString = "6f94b552-48ca-405c-a25e-ed51ffa49ef1";

        public const string guidVsAutoDeployCmdSetString = "72e035e5-88ca-4dd8-98aa-6f7aedc59a38";

        public static readonly Guid guidVsAutoDeployCmdSet = new Guid(guidVsAutoDeployCmdSetString);
    }
}
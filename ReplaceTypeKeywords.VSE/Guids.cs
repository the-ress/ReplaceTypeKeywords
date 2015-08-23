// Guids.cs
// MUST match guids.h
using System;

namespace ReplaceTypeKeywords.VSE
{
    static class GuidList
    {
        public const string guidVSPackage2PkgString = "596f94fa-71dc-4f28-831f-73bf10beb48e";
        public const string guidVSPackage2CmdSetString = "8fbac124-05f4-47d3-8258-83f9678a788c";

        public static readonly Guid guidVSPackage2CmdSet = new Guid(guidVSPackage2CmdSetString);
    };
}
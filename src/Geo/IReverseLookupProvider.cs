using System;
using System.Net.Http;
using NLog;

namespace Rangic.Utilities.Geo
{
    public interface IReverseLookupProvider
    {
        string Lookup(double latitude, double longitude);
    }
}

using System;
using System.Net.Http;
using NLog;

namespace Rangic.Utilities.Geo
{
    public interface IReverseLookupProvider
    {
        string Lookup(double latitude, double longitude);
    }

    public class StandardLookupProvider : IReverseLookupProvider
    {
        static private readonly Logger logger = LogManager.GetCurrentClassLogger();


        public string Lookup(double latitude, double longitude)
        {
            var startTime = DateTime.Now;
            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromMilliseconds(5 * 1000);
                    var requestUrl = string.Format(
                        "nominatim/v1/reverse?format=json&lat={0}&lon={1}&addressdetails=1&zoom=18&accept-language=en-us",
                        latitude,
                        longitude);

                    client.BaseAddress = new Uri("http://open.mapquestapi.com/");
                    var task = client.GetAsync(requestUrl);
                    var result = task.Result;
                    if (task.IsCompleted)
                    {
                        if (result.IsSuccessStatusCode)
                        {
                            return result.Content.ReadAsStringAsync().Result;
                        }

                        logger.Warn("GeoLocation request failed: {0}; {1}; {1}", 
                            result.StatusCode, result.ReasonPhrase, result.Content.ReadAsStringAsync().Result);
                    }
                    else
                    {
                        logger.Warn("GeoLocation task failed: canceled {0}, faulted {1}, {2}", 
                            task.IsCanceled, task.IsFaulted, task.Exception);
                    }
                }
            }
            catch (AggregateException ae)
            {
                logger.Warn("Exception getting geolocation ({0} msecs) for {1},{2}:", (DateTime.Now - startTime).TotalMilliseconds, latitude, longitude);
                logger.Warn("  {0}", ae.Message);
                foreach (var inner in ae.InnerExceptions)
                {
                    logger.Warn("  {0}", inner.Message);
                    var i = inner.InnerException;
                    while (i != null)
                    {
                        logger.Warn("     {0}", i.Message);
                        i = i.InnerException;
                    }
                }
            }
            catch (Exception e)
            {
                logger.Warn("Exception getting geolocation: {0} for {1},{2}", e.ToString(), latitude, longitude);
            }

            return null;
        }
    }
}

using System;
using NLog;
using System.IO;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace Rangic.Utilities.Preferences
{
    static public class Preferences<T> where T:BasePreferences, new()
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        static public T Instance { get; private set; }


        static private string Filename { get; set; }
        static public void Save()
        {
            Save(Filename);
        }

        static public T Load(string filename)
        {
            Instance = new T();
            Filename = filename;

            if (File.Exists(filename))
            {
                try
                {
                    dynamic json = JObject.Parse(File.ReadAllText(filename));
                    Instance.FromJson(json);
                }
                catch (Exception e)
                {
                    logger.Error("Exception loading preferences (using defaults) from {0}: {1}", filename, e);
                }
            }
            else
            {
                try
                {
                    Save(filename);
                }
                catch (Exception e)
                {
                    logger.Error("Exception saving preferences to '{0}': {1}", filename, e);
                }
            }

            return Instance;
        }

        static private void Save(string filename)
        {
            var prefs = Instance.ToJson();
            File.WriteAllText(filename, JsonConvert.SerializeObject(prefs));
        }
    }

    abstract public class BasePreferences
    {
        abstract public void FromJson(dynamic json);
        abstract public dynamic ToJson();
    }
}

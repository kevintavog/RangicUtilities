using System;
using System.IO;
using System.Data.SQLite;
using NLog;
using Rangic.Utilities.Os;


namespace Rangic.Utilities.Geo
{
    public class PersistentCachingReverseLookupProvider : IReverseLookupProvider
    {
        static private readonly Logger logger = LogManager.GetCurrentClassLogger();
        static private IReverseLookupProvider innerLookupProvider = new OpenStreetMapLookupProvider();
        private static readonly Object lockObject = new object();


        public string Lookup(double latitude, double longitude)
        {
            var key = String.Format("{0}, {1}", latitude, longitude);
            var result = GetDataForKey(key);
            if (result == null)
            {
                result = innerLookupProvider.Lookup(latitude, longitude);
                StoreDataForKey(key, result);
            }

            return result;
        }

        static private string GetDataForKey(string key)
        {
            try
            {
                using (var connection = new SQLiteConnection(("Data Source=" + DatabasePath)))
                {
                    string result = null;
                    connection.Open();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandType = System.Data.CommandType.Text;
                        command.CommandText = "SELECT fullPlacename FROM LocationCache WHERE geoLocation = @key";
                        command.Parameters.AddWithValue("@key", key);
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                result = reader.GetString(0);
                            }
                        }
                    }

                    return result;
                }
            }
            catch (Exception e)
            {
                logger.Error("Error looking up key [{0}]: {1}", key, e.ToString());
            }
            return null;
        }

        static private void StoreDataForKey(string key, string data)
        {
            if (String.IsNullOrWhiteSpace(data))
            {
                logger.Warn("Ignoring empty data for '{0}'", key);
                return;
            }

            try
            {
                using (var connection = new SQLiteConnection(("Data Source=" + DatabasePath)))
                {
                    connection.Open();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandType = System.Data.CommandType.Text;
                        command.CommandText = "INSERT INTO LocationCache (geoLocation, fullPlacename) VALUES(@key, @data)";
                        command.Parameters.AddWithValue("@key", key);
                        command.Parameters.AddWithValue("@data", data);
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception e)
            {
                logger.Error("Error storing key [{0}]: {1}", key, e.ToString());
            }
        }

        static private string DatabasePath { get { return Path.Combine(DatabaseFolder, "location.cache"); } }
        static private string DatabaseFolder { get { return Path.Combine(Platform.UserDataFolder("Rangic"), "Location"); } }

        static private void EnsureExists(SQLiteConnection connection)
        {
            try
            {
                if (!File.Exists(DatabasePath))
                {
                    lock(lockObject)
                    {
                        if (!File.Exists(DatabasePath))
                        {
                            if (!Directory.Exists(DatabaseFolder))
                            {
                                logger.Warn("Creating location cache folder: {0}", DatabaseFolder);
                                Directory.CreateDirectory(DatabaseFolder);
                            }

                            logger.Warn("Creating cache schema");
                            connection.Open();
                            using (var command = connection.CreateCommand())
                            {
                                command.CommandText = "CREATE TABLE IF NOT EXISTS LocationCache (geoLocation TEXT PRIMARY KEY, fullPlacename TEXT)";
                                command.CommandType = System.Data.CommandType.Text;
                                command.ExecuteNonQuery();
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                logger.Error("Unable to open or create database cache: {0}", e.ToString());
            }
        }
    }
}

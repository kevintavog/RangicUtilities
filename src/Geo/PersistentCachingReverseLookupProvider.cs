using System;
using System.IO;
using Mono.Data.Sqlite;
using NLog;
using Rangic.Utilities.Os;


namespace Rangic.Utilities.Geo
{
    public class PersistentCachingReverseLookupProvider : IReverseLookupProvider
    {
        static private readonly Logger logger = LogManager.GetCurrentClassLogger();
        static private IReverseLookupProvider innerLookupProvider = new OpenStreetMapLookupProvider();
        static private SqliteConnection databaseConnection;
        static private bool creationAttempted;


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
                string result = null;
                var db = GetConnection();
                if (db != null)
                {
                    db.Open();
                    using (var command = db.CreateCommand())
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
                    db.Close();
                }
                
                return result;
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
                var db = GetConnection();
                if (db != null)
                {
                    db.Open();
                    using (var command = db.CreateCommand())
                    {
                        command.CommandType = System.Data.CommandType.Text;
                        command.CommandText = "INSERT INTO LocationCache (geoLocation, fullPlacename) VALUES(@key, @data)";
                        command.Parameters.AddWithValue("@key", key);
                        command.Parameters.AddWithValue("@data", data);
                        command.ExecuteNonQuery();
                    }
                    db.Close();
                }
            }
            catch (Exception e)
            {
                logger.Error("Error storing key [{0}]: {1}", key, e.ToString());
            }
        }

        static private SqliteConnection GetConnection()
        {
            if (databaseConnection == null && creationAttempted == false)
            {
                databaseConnection = OpenOrCreate();
                creationAttempted = true;
            }
            return databaseConnection;
        }

        static private SqliteConnection OpenOrCreate()
        {
            try
            {
                var dbFolder = Path.Combine(Platform.UserDataFolder("Rangic"), "Location");
                var dbPath = Path.Combine(dbFolder, "location.cache");
                if (!Directory.Exists(dbFolder))
                {
                    logger.Warn("Creating location cache folder: {0}", dbFolder);
                    Directory.CreateDirectory(dbFolder);
                }

                var databaseExists = File.Exists(dbPath);
                if (!databaseExists)
                {
                    logger.Warn("Creating location cache file: {0}", dbPath);
                    SqliteConnection.CreateFile(dbPath);
                }

                var connection = new SqliteConnection("Data Source=" + dbPath);

                if (!databaseExists)
                {
                    logger.Warn("Creating cache schema");
                    connection.Open();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "CREATE TABLE IF NOT EXISTS LocationCache (geoLocation TEXT PRIMARY KEY, fullPlacename TEXT)";
                        command.CommandType = System.Data.CommandType.Text;
                        command.ExecuteNonQuery();
                    }
                    connection.Close();
                }
                return connection;
            }
            catch (Exception e)
            {
                logger.Error("Unable to open or create database cache: {0}", e.ToString());
            }
            return null;
        }
    }
}

using Eco.Core.Serialization;
using Eco.EW.Tools;

namespace Eco.EW.Utils
{
    public static class Persistance
    {
        public static bool WriteJsonToFile<T>(T data, string directoryPath, string nameAndExtension)
        {
            // Serialize data to JSON
            string json;
            try
            {
                json = SerializationUtils.SerializeJson(data);
            }
            catch (Exception e)
            {
                Logger.Exception($"Failed to serialize data for storage file \"{nameAndExtension}\"", e);
                return false;
            }

            // Ensure directory exists
            try
            {
                if (!Directory.Exists(directoryPath))
                    Directory.CreateDirectory(directoryPath);
            }
            catch (Exception e)
            {
                Logger.Exception($"Failed to create directory \"{directoryPath}\" while writing storage file \"{nameAndExtension}\"", e);
                return false;
            }

            // Parse path
            string path;
            try
            {
                path = Path.Combine(directoryPath, nameAndExtension);
            }
            catch(Exception e)
            {
                Logger.Exception($"Failed to parse path for writing storage file. Directory = \"{directoryPath}\" | Filename = \"{nameAndExtension}\"", e);
                return false;
            }
            
            // Write JSON to file
            try
            {
                StreamWriter writer = new StreamWriter(path);
                writer.Write(json);
                return true;
            }
            catch (Exception e)
            {
                Logger.Exception($"Failed to write JSON storage data to \"{path}\"", e);
                return false;
            }
        }

        public static bool ReadJsonFromFile<T>(string directoryPath, string nameAndExtension, ref T data)
        {
            if (!Directory.Exists(directoryPath))
            {
                Logger.Silent($"Failed to find directory \"{directoryPath}\" for reading storage file \"{nameAndExtension}\"");
                return false;
            }

            // Parse path
            string path;
            try
            {
                path = Path.Combine(directoryPath, nameAndExtension);
            }
            catch (Exception e)
            {
                Logger.Exception($"Failed to parse path for reading storage file. Directory = \"{directoryPath}\" | Filename = \"{nameAndExtension}\"", e);
                return false;
            }


            if (!File.Exists(path))
            {
                Logger.Silent($"Failed to find file \"{path}\" for reading storage data");
                return false;
            }

            string jsonStr;
            try
            {
                StreamReader reader = new StreamReader(path);
                jsonStr = reader.ReadToEnd();
                reader.Close();
                reader.Dispose();
            }
            catch(Exception e)
            {
                Logger.Exception($"Failed to read storage data from \"{path}\"", e);
                return false;
            }

            T result;
            try
            {
                result = SerializationUtils.DeserializeJson<T>(jsonStr);
            }
            catch (Exception e)
            {
                Logger.Exception($"Failed to parse JSON storage data from \"{path}\"", e);
                return false;
            }

            data = result;
            return true;
        }
    }
}

﻿using Eco.Core.Serialization;
using Eco.Moose.Tools.Logger;
using System.Reflection;

namespace Eco.Moose.Utils.Persistance
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
                Logger.Exception($"Failed to serialize data for storage file \"{nameAndExtension}\"", e, Assembly.GetCallingAssembly());
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
                Logger.Exception($"Failed to create directory \"{directoryPath}\" while writing storage file \"{nameAndExtension}\"", e, Assembly.GetCallingAssembly());
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
                Logger.Exception($"Failed to parse path for writing storage file. Directory = \"{directoryPath}\" | Filename = \"{nameAndExtension}\"", e, Assembly.GetCallingAssembly());
                return false;
            }

            // Write JSON to file
            try
            {
                StreamWriter writer = new StreamWriter(path);
                writer.Write(json);
                writer.Flush();
                Logger.Trace($"Successfully wrote persistance JSON to \"{path}\"", Assembly.GetCallingAssembly());
                return true;
            }
            catch (Exception e)
            {
                Logger.Exception($"Failed to write JSON storage data to \"{path}\"", e, Assembly.GetCallingAssembly());
                return false;
            }
        }

        public static bool ReadJsonFromFile<T>(string directoryPath, string nameAndExtension, ref T data)
        {
            if (!Directory.Exists(directoryPath))
            {
                Logger.Silent($"Failed to find directory \"{directoryPath}\" for reading storage file \"{nameAndExtension}\"", Assembly.GetCallingAssembly());
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
                Logger.Exception($"Failed to parse path for reading storage file. Directory = \"{directoryPath}\" | Filename = \"{nameAndExtension}\"", e, Assembly.GetCallingAssembly());
                return false;
            }


            if (!File.Exists(path))
            {
                Logger.Silent($"Failed to find file \"{path}\" for reading storage data", Assembly.GetCallingAssembly());
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
            catch (Exception e)
            {
                Logger.Exception($"Failed to read storage data from \"{path}\"", e, Assembly.GetCallingAssembly());
                return false;
            }

            T result;
            try
            {
                result = SerializationUtils.DeserializeJson<T>(jsonStr);
            }
            catch (Exception e)
            {
                Logger.Exception($"Failed to parse JSON storage data from \"{path}\"", e, Assembly.GetCallingAssembly());
                return false;
            }

            Logger.Trace($"Successfully read persistance JSON from \"{path}\"", Assembly.GetCallingAssembly());
            data = result;
            return true;
        }
    }
}

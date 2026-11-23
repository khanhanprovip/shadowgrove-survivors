using System;
using System.IO;
using UnityEngine;

namespace RGame.RoguelikeKit
{
    public static class FileManager
    {
        private static string SaveDirectory => Environment.GetEnvironmentVariable("DATN_TEST_PROFILE_DIR")
                                               ?? Application.persistentDataPath;

        public static bool WriteToFile(string fileName, string fileContents, string backupFileName = null)
        {
            string directory = SaveDirectory;
            string path = Path.Combine(directory, fileName);
            string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                Directory.CreateDirectory(directory);
                File.WriteAllText(temporary, fileContents);
                if (File.Exists(path))
                {
                    string backup = backupFileName == null ? null : Path.Combine(directory, backupFileName);
                    File.Replace(temporary, path, backup);
                }
                else
                {
                    File.Move(temporary, path);
                }
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to write to {path}: {e}");
                return false;
            }
            finally
            {
                if (File.Exists(temporary))
                {
                    try { File.Delete(temporary); }
                    catch (IOException e) { Debug.LogWarning(e.Message); }
                    catch (UnauthorizedAccessException e) { Debug.LogWarning(e.Message); }
                }
            }
        }

        public static bool LoadFromFile(string fileName, out string result)
        {
            string path = Path.Combine(SaveDirectory, fileName);
            result = "";
            if (!File.Exists(path)) return false;

            try
            {
                result = File.ReadAllText(path);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to read from {path}: {e}");
                return false;
            }
        }
    }
}

using System;
using System.IO;
using System.Linq;
using Microsoft.Extensions.PlatformAbstractions;
using Newtonsoft.Json.Linq;

namespace Copy.Librdkafka
{
    public class Program
    {
        public static int Main(string[] args)
        {
            try
            {
                var packagesFolder = Environment.GetEnvironmentVariable("NUGET_PACKAGES");

                if (string.IsNullOrEmpty(packagesFolder))
                {
                    packagesFolder = Path.Combine(GetHome(), ".nuget", "packages");
                }

                packagesFolder = Environment.ExpandEnvironmentVariables(packagesFolder);

                var lockJson = JObject.Parse(File.ReadAllText("project.lock.json"));

                foreach (var librdkafkaLib in lockJson["libraries"].OfType<JProperty>().Where(
                    p => p.Name.StartsWith("RdKafka.Internal.librdkafka", StringComparison.Ordinal)))
                {
                    foreach (var filePath in librdkafkaLib.Value["files"].Select(v => v.Value<string>()))
                    {
                        if (filePath.ToString().StartsWith("runtimes/", StringComparison.Ordinal))
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                            File.Copy(Path.Combine(packagesFolder, librdkafkaLib.Name, filePath), filePath, overwrite: true);
                        }
                    }
                }

                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw;
            }
        }

        private static string GetHome()
        {
#if NET451
            return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
#else
            var runtimeEnv = PlatformServices.Default.Runtime;
            if (runtimeEnv.OperatingSystem == "Windows")
            {
                return Environment.GetEnvironmentVariable("USERPROFILE") ??
                    Environment.GetEnvironmentVariable("HOMEDRIVE") + Environment.GetEnvironmentVariable("HOMEPATH");
            }
            else
            {
                var home = Environment.GetEnvironmentVariable("HOME");

                if (string.IsNullOrEmpty(home))
                {
                    throw new Exception("Home directory not found. The HOME environment variable is not set.");
                }

                return home;
            }
#endif
        }
    }
}

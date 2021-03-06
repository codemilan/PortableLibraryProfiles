﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using FrameworkProfiles;
using FrameworkProfiles.FileSystem;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace AsJson
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            try
            {
                foreach (var path in Directory.EnumerateDirectories("."))
                {
                    CleanDirectory(path);
                    ProcessProfiles(DiskFileSystem.Folder(path), Path.Combine(path, "profiles.json"));
                }

                foreach (var zip in Directory.EnumerateFiles(".", "*.zip"))
                    ProcessProfiles(ZipFileSystem.Open(zip), Path.ChangeExtension(zip, "json"));
                ProcessProfiles(DiskFileSystem.Folder(PortableFrameworkProfileEnumerator.MachineProfilePath), "profiles.json");

                Console.WriteLine("Done.");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
            }
            Console.ReadKey();
        }

        static void ProcessProfiles(IFolder path, string jsonFile)
        {
            var profiles = PortableFrameworkProfileEnumerator.EnumeratePortableProfiles(path)
                .OrderBy(x => int.Parse(x.Name.Profile.Substring(7)))
                .Select(p => new
                {
                    p.Name.FullName,
                    ProfileName = p.Name.Profile,
                    p.SupportedByVisualStudio2013,
                    p.SupportedByVisualStudio2015,
                    p.SupportsAsync,
                    p.SupportsGenericVariance,
                    p.NugetTarget,
                    NetStandardGeneration = p.NetStandardGeneration == null ? null : "netstandard" + p.NetStandardGeneration?.ToString(2),
                    DotnetGeneration = p.DotnetGeneration == null ? null : "dotnet" + p.DotnetGeneration?.ToString(2),
                    Frameworks = p.SupportedFrameworks.Select(f => new
                    {
                        f.Name.FullName,
                        f.FriendlyName,
                    }).ToArray(),
                }).ToArray();

            File.WriteAllText(jsonFile, JsonConvert.SerializeObject(profiles, new JsonSerializerSettings() { ContractResolver = new CamelCasePropertyNamesContractResolver() }));
        }

        static void CleanDirectory(string path)
        {
            var versions = Directory.EnumerateDirectories(path);
            foreach (var version in versions)
            {
                foreach (var file in Directory.EnumerateFiles(version).ToArray())
                    File.Delete(file);

                foreach (var subdir in Directory.EnumerateDirectories(version).Where(x => !x.Contains("Profile") && !x.Contains("RedistList")))
                    Directory.Delete(subdir, true);

                var profilePath = Path.Combine(version, "Profile");
                if (!Directory.Exists(profilePath))
                    continue;
                foreach (var profile in Directory.EnumerateDirectories(profilePath))
                {
                    foreach (var file in Directory.EnumerateFiles(profile).ToArray())
                        File.Delete(file);
                    foreach (var subdir in Directory.EnumerateDirectories(profile).Where(x => !x.Contains("SupportedFrameworks") && !x.Contains("RedistList")))
                        Directory.Delete(subdir, true);
                }
            }
        }
    }
}

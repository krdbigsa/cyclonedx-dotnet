using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using CycloneDX.Interfaces;
using CycloneDX.Models;
using NuGet.Configuration;

namespace CycloneDX.Services
{
    public class NugetV3ServiceFactory : INugetServiceFactory
    {
        public INugetService Create(
            RunOptions option,
            IFileSystem fileSystem,
            IGithubService githubService,
            List<string> packageCachePaths)
        {
            var nugetLogger = new NuGet.Common.NullLogger();

            List<NugetInputModel> nugetInputs = new List<NugetInputModel>();

            if (string.IsNullOrEmpty(option.nugetConfigPath) == false)
            {
                var settings = Settings.LoadSpecificSettings(
                    fileSystem.Path.GetDirectoryName(option.nugetConfigPath),
                    fileSystem.Path.GetFileName(option.nugetConfigPath));

                var packageSourceProvider = new PackageSourceProvider(settings);

                var packageSources = packageSourceProvider.LoadPackageSources();

                nugetInputs.AddRange(
                    from packageSource in packageSources
                    let isPasswordClearText = packageSource.Credentials?.IsPasswordClearText ?? false
                    select NugetInputFactory.Create(
                        packageSource.Source,
                        packageSource.Credentials?.Username,
                        isPasswordClearText
                            ? packageSource.Credentials?.PasswordText
                            : packageSource.Credentials?.Password,
                        isPasswordClearText));
            }
            else if (string.IsNullOrEmpty(option.baseUrl) == false)
            {
                nugetInputs.Add(
                    NugetInputFactory.Create(
                        option.baseUrl,
                        option.baseUrlUserName,
                        option.baseUrlUSP,
                        option.isPasswordClearText));
            }
            else
            {
                nugetInputs.Add(NugetInputFactory.Create("https://api.nuget.org/v3/index.json", null, null, false));
            }

            return new NugetV3Service(
                nugetInputs,
                fileSystem,
                packageCachePaths,
                githubService,
                nugetLogger,
                option.disableHashComputation);
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Git;
using Serilog;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using Nuke.Common.Tools.MinVer;

class Build : NukeBuild
{
    public static int Main() => Execute<Build>(x => x.Test);

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly string Configuration = IsLocalBuild ? "Debug" : "Release";

    [Solution] readonly Solution Solution;

    [GitRepository] readonly GitRepository GitRepository;

    [MinVer] readonly MinVer MinVer;
    
    AbsolutePath sourceDirectory => RootDirectory;
    AbsolutePath testDirectory => RootDirectory;
    AbsolutePath artifactsDirectory => RootDirectory / "artifacts";

    Target Print => _ => _
        .Executes(() =>
        {
            Log.Information("Branch = {Branch}", GitRepository.Branch);
            Log.Information("Commit = {Commit}", GitRepository.Commit);
            Log.Information("MinVer = {Value}", MinVer?.Version);
            Log.Information("Configuration = {Configuration}", Configuration);
        });

    Target Clean => _ => _
        .DependsOn(Print)
        .Executes(() =>
        {
            sourceDirectory.GlobDirectories("**/bin", "**/obj").DeleteDirectories();
            testDirectory.GlobDirectories("**/bin", "**/obj").DeleteDirectories();
            artifactsDirectory.CreateOrCleanDirectory();
        });

    Target Restore => _ => _
        .DependsOn(Print)
        .Executes(() =>
        {
            DotNetRestore(s => s
                .SetProjectFile(Solution));
        });

    Target Compile => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            DotNetBuild(s => s
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration)
                .EnableNoRestore());
        });

    Target Test => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            DotNetRun(s => s
                .SetProjectFile(Solution.Projects.FirstOrDefault(x => x.Name == "Flake.Tests"))
                .SetProperties( new Dictionary<string, object>
                {
                    ["CollectCoverage"] = "true" , 
                    ["CoverletOutputFormat"] = "cobertura",
                })
                .SetConfiguration(Configuration)
                .SetFramework("net90")
                .EnableNoRestore()
                .EnableNoBuild());
        });
}
using System.Linq;
using Fallout.Common;
using Fallout.Common.IO;
using Fallout.Solutions;
using Fallout.Common.Tools.DotNet;
using Fallout.Common.Git;
using Serilog;
using static Fallout.Common.Tools.DotNet.DotNetTasks;
using Fallout.Common.Tools.MinVer;

class Build : FalloutBuild
{
    public static int Main() => Execute<Build>(x => x.Test);

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration  Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [Solution] readonly Solution Solution;

    [GitRepository] readonly GitRepository GitRepository;

    [MinVer] readonly MinVer MinVer;
    
    AbsolutePath sourceDirectory => RootDirectory;
    AbsolutePath testDirectory => RootDirectory;
    AbsolutePath artifactsDirectory => RootDirectory / "artifacts";
    AbsolutePath TestResultsDirectory => artifactsDirectory / "results";

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
                .SetVersion(MinVer?.Version)
                .EnableNoRestore());
        });

    Target Test => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            DotNetRun(s => s
                .SetProjectFile(Solution.Projects.FirstOrDefault(x => x.Name == "Flake.Tests"))
                .SetConfiguration(Configuration)
                .SetVersion(MinVer?.Version)
                .SetFramework("net10.0")
                .EnableNoRestore()
                .EnableNoBuild());
        });
}
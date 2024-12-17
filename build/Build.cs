using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Cryptography.X509Certificates;
using System.Xml;
using Nuke.Common;
using Nuke.Common.CI.AzurePipelines;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Utilities.Collections;
using Nuke.Common.Git;
using Serilog;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using static Nuke.Common.IO.FileSystemTasks;
using Nuke.Common.Tools.MinVer;

class Build : NukeBuild
{
    public static int Main() => Execute<Build>(x => x.Test);

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly string Configuration = IsLocalBuild ? "Debug" : "Release";

    [Solution] readonly Solution Solution;

    [GitRepository] readonly GitRepository GitRepository;

    AbsolutePath sourceDirectory => RootDirectory;
    AbsolutePath testDirectory => RootDirectory;
    AbsolutePath artifactsDirectory => RootDirectory / "artifacts";

    AzurePipelines AzurePipelines => AzurePipelines.Instance;

    // [MinVer] readonly MinVer MinVer;

    Target Print => _ => _
        .Executes(() =>
        {
            Log.Information("Branch = {Branch}", AzurePipelines?.SourceBranch);
            Log.Information("Commit = {Commit}", AzurePipelines?.SourceVersion);
            //Log.Information("MinVer = {Value}", MinVer?.Version);
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
            DotNetTest(s => s
                .SetProjectFile(Solution)
                .SetProperties( new Dictionary<string, object>
                {
                    ["CollectCoverage"] = "true" , 
                    ["CoverletOutputFormat"] = "cobertura",
                })
                .SetConfiguration(Configuration)
                .EnableNoRestore()
                .EnableNoBuild());
        });
}
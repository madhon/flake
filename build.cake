#addin "nuget:?package=Cake.MinVer&version=2.0.0"
#addin nuget:?package=Cake.Git&version=2.0.0

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");

var solutions = GetFiles("./**/*.sln");
var solutionPaths = solutions.Select(solution => solution.GetDirectory());

GitBranch currentBranch = GitBranchCurrent("./");

var version = MinVer();


Setup(context =>
{
   Information($"Building Flake with configuration {configuration} on branch {currentBranch.FriendlyName}");
       context.Information($"Version: {version.Version}");
    context.Information($"Major: {version.Major}");
    context.Information($"Minor: {version.Minor}");
    context.Information($"Patch: {version.Patch}");
    context.Information($"PreRelease: {version.PreRelease}");
    context.Information($"BuildMetadata: {version.BuildMetadata}");
});


Task("Clean")
    .Does(() =>
{
    foreach(var path in solutionPaths)
    {
        Information("Cleaning {0}", path);
        CleanDirectories(path + "/**/bin/" + configuration);
        CleanDirectories(path + "/**/obj/" + configuration);
    }
});

Task("Build")
    .Does(() =>
{
    DotNetBuild("./Flake.sln", new DotNetCoreBuildSettings
    {
        Configuration = configuration,
    });
});

Task("Test")
    .Does(() =>
{
    DotNetTest("./Flake.sln", new DotNetCoreTestSettings
    {
        Configuration = configuration,
        NoBuild = true,
    });
});

Task("Default")
    .IsDependentOn("Clean")
    .IsDependentOn("Build")
    .IsDependentOn("Test");

Task("CI")
  .IsDependentOn("Default");


RunTarget(target);

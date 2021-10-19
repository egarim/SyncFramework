var target = Argument("target", "BuildRelease");
var configuration = Argument("Configuration", "Debug");
var solution = "./src/SyncFramework.slnf";
var configurationRelease = Argument("configuration", "Release");


// Run dotnet restore to restore all package references.
//INFO https://andrewlock.net/running-tests-with-dotnet-xunit-using-cake/

Task("Build")
    .Does(() =>
    {
        DotNetCoreBuild(solution,
           new DotNetCoreBuildSettings()
                {
                    Configuration = configuration
                });
    });


Task("BuildRelease")
    .IsDependentOn("Test")
    .Does(() =>
    {
        DotNetCoreBuild(solution,
           new DotNetCoreBuildSettings()
                {
                    Configuration = configurationRelease
                });
    });

Task("Test")
    .IsDependentOn("Build")
    .Does(() =>
    {
        var projects = GetFiles("./src/tests/**/*.csproj");
        foreach(var project in projects)
        {
            DotNetCoreTest(
                project.FullPath,
                new DotNetCoreTestSettings()
                {
                    Configuration = configuration,
                    NoBuild = true
                });
        }
    });

RunTarget(target);
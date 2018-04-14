var target = Argument<string>("Target", "Build");

var SolutionFile = MakeAbsolute(File("SecretsManager.sln"));
var outputFolder = SolutionFile.GetDirectory().Combine("outputs");
var testFolder = SolutionFile.GetDirectory().Combine("tests");

Setup(context => {
    CleanDirectory(outputFolder);
});

Task("Restore")
    .Does(() =>
{
    DotNetCoreRestore(SolutionFile.FullPath);
});

Task("Build")
    .IsDependentOn("Restore")
    .Does(() =>
{
    var settings = new DotNetCoreBuildSettings
    {
        NoRestore = true,
        Configuration = "Debug"
    };

    DotNetCoreBuild(SolutionFile.FullPath, settings);
});

Task("Test")
    .IsDependentOn("Build")
    .Does(() =>
{
    var files = GetFiles($"{testFolder}/**/*.csproj");

    foreach (var file in files)
    {
        var settings = new DotNetCoreTestSettings
        {
            NoBuild = false,
            NoRestore = true,
            Configuration = "Debug"
        };

        DotNetCoreTest(file.FullPath, settings);
    }
});

Task("Pack")
    .IsDependentOn("Test")
    .Does(() => 
{
    var settings = new DotNetCorePackSettings
    {
        Configuration = "Release",
        NoRestore = true,
        OutputDirectory = outputFolder
    };

    DotNetCorePack(SolutionFile.FullPath, settings);
});

RunTarget(target);
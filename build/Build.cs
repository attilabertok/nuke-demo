using System;
using System.Collections.Generic;
using System.Linq;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.CI.AzurePipelines;
using Nuke.Common.Execution;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.GitVersion;
using Nuke.Common.Utilities.Collections;
using Serilog;

using static Nuke.Common.EnvironmentInfo;
using static Nuke.Common.ControlFlow;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.IO.PathConstruction;

[ShutdownDotNetAfterServerBuild]
[AzurePipelines(
    AzurePipelinesImage.WindowsLatest,
    InvokedTargets = new[] { nameof(Compile) })]
class Build : NukeBuild
{
    public static int Main () => Execute<Build>(x => x.Compile);

    [CI] readonly AzurePipelines AzurePipelines;

    [GitVersion]
    readonly GitVersion GitVersion;

    [Solution(GenerateProjects = true)] readonly Solution Solution;

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    const string MainBranch = "main";

    [PackageExecutable(
        packageId: "xunit.runner.console",
        packageExecutable: "xunit.console.exe")]
    readonly Tool Xunit;


    IEnumerable<Project> TestProjects => Partition.GetCurrent(Solution.GetProjects("*.Tests.*"));

    Target Clean => _ => _
        .Before(Restore)
        .Executes(() =>
        {
            var projectsToClean = Solution.Projects.Where(p => p != Solution._build);
            var pathsToClean = projectsToClean.SelectMany(p => p.Path.GlobDirectories("*/bin", "*/obj"));
            pathsToClean.ForEach(DeleteDirectory);
        });

    Target PrintVersion => _ => _
        .Executes(() =>
        {
            Log.Information("GitVersion = {Value}", GitVersion.MajorMinorPatch);
        });

    Target Restore => _ => _
        .Executes(() =>
        {
            DotNetRestore(s => s
                .SetProjectFile(Solution)
                .EnableNoCache());
        });

    Target Compile => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            DotNetBuild(s => s
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration)
                .EnableNoLogo()
                .EnableNoRestore()
                .SetAssemblyVersion(GitVersion.AssemblySemVer)
                .SetFileVersion(GitVersion.AssemblySemFileVer)
                .SetInformationalVersion(GitVersion.InformationalVersion));
        });

    Target RunUnitTests => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
        });
}

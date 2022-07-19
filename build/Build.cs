using System;
using System.Collections.Generic;
using System.Linq;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.CI.AzurePipelines;
using Nuke.Common.Execution;
using Nuke.Common.Git;
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
using Assert = Nuke.Common.Assert;

[ShutdownDotNetAfterServerBuild]
[AzurePipelines(
    AzurePipelinesImage.WindowsLatest,
    InvokedTargets = new[] { nameof(Compile) })]
public class Build : NukeBuild
{
    public static int Main () => Execute<Build>(x => x.Compile);

    [CI] readonly AzurePipelines AzurePipelines;

    [GitVersion]
    readonly GitVersion GitVersion;

    [GitRepository]
    readonly GitRepository GitRepository;

    [PathExecutable]
    readonly Tool Git;

    [Solution(GenerateProjects = true)] readonly Solution Solution;

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    const string MainBranch = "main";

    IDictionary<string, Project> TestProjects => Partition.GetCurrent(Solution.GetProjects($"*.{NameSegment.Tests}.*"))
        .ToDictionary(ProjectHelper.GetTestProjectType, ProjectHelper.Self);


    Target Clean => _ => _
        .Before(Restore)
        .Executes(() =>
        {
            var projectsToClean = Solution.Projects.Where(p => p != Solution._build);
            var pathsToClean = projectsToClean.SelectMany(p => p.Path.GlobDirectories("*/bin", "*/obj"));
            pathsToClean.ForEach(DeleteDirectory);
        });

    Target Version => _ => _
        .Executes(() =>
        {
            Log.Information("GitVersion = {Value}", GitVersion.MajorMinorPatch);
        });

    Target Status => _ => _
        .Executes(() =>
        {
            Git("status");
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

    Target TestUnit => _ => _
        .DependsOn(Compile)
        .ProceedAfterFailure()
        .Executes(() =>
        {
            DotNetTest(s => s
                .SetProjectFile(TestProjects[NameSegment.TestType.Unit])
                .EnableNoRestore()
                .EnableNoBuild()
                .SetFilter(NameSegment.TestType.Unit)
            );
        });

    Target TestFunctional => _ => _
        .DependsOn(Compile)
        .ProceedAfterFailure()
        .Executes(() =>
        {
            Assert.False(GitRepository.IsOnDevelopBranch());
            DotNetTest(s => s
                .SetProjectFile(TestProjects[NameSegment.TestType.Functional])
                .EnableNoRestore()
                .EnableNoBuild()
                .SetFilter(NameSegment.TestType.Functional)
            );
        });

    Target TestAcceptance => _ => _
        .DependsOn(Compile)
        .ProceedAfterFailure()
        .Executes(() =>
        {
            DotNetTest(s => s
                .SetProjectFile(TestProjects[NameSegment.TestType.Acceptance])
                .EnableNoRestore()
                .EnableNoBuild()
                .SetFilter(NameSegment.TestType.Acceptance)
            );
        });

    Target Test => _ => _
        .DependsOn(Compile)
        .DependsOn(TestUnit)
        .DependsOn(TestAcceptance)
        .DependsOn(TestFunctional)
        .Executes(() => { });
}

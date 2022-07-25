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
using Nuke.Common.Tools.Coverlet;
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

public class Folder
{
    public const string TestResults = ".test_results";
}

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

    public IProcess ApiProcess { get; set; }

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    readonly AbsolutePath TestResultsDirectory = RootDirectory / Folder.TestResults;

    const string MainBranch = "main";

    List<string> testFilter = Enumerable.Empty<string>().ToList();

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
            Log.Information("GitVersion = {Value}", GitVersion.FullSemVer);
        });

    Target Status => _ => _
        .Executes(() =>
        {
            Git("status");
        });

    Target Restore => _ => _
        .Executes(() =>
        {
            DotNetRestore(_ => _
                .SetProjectFile(Solution)
                .EnableNoCache());
        });

    Target Compile => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            DotNetBuild(_ => _
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
            testFilter = new List<string>{ NameSegment.TestType.Unit };
            Log.Information($"Test filter set to {string.Join(", ", testFilter)} Tests.");
        })
        .Triggers(TestExecute);

    Target StartApi => _ => _
        .Unlisted()
        .OnlyWhenDynamic(() => testFilter.Contains(NameSegment.TestType.Functional))
        .Executes(() =>
            {
                ApiProcess = ProcessTasks.StartProcess("dotnet", "run", Solution.TodoApi.Directory);
            }
        );

    Target TestFunctional => _ => _
        .DependsOn(Compile)
        .OnlyWhenDynamic(() => !GitRepository.IsOnFeatureBranch())
        .ProceedAfterFailure()
        .Executes(() =>
        {
            testFilter = new List<string> {NameSegment.TestType.Functional };
            Log.Information($"Test filter set to {string.Join(", ", testFilter)} Tests.");
        })
        .Triggers(TestExecute);

    Target StopApi => _ => _
        .Unlisted()
        .OnlyWhenDynamic(() => testFilter.Contains(NameSegment.TestType.Functional))
        .Executes(() =>
            {
                ApiProcess.Kill();
            }
        );

    Target TestAcceptance => _ => _
        .DependsOn(Compile)
        .ProceedAfterFailure()
        .Executes(() =>
        {
            testFilter = new List<string> {NameSegment.TestType.Acceptance };
            Log.Information($"Test filter set to {string.Join(", ", testFilter)} Tests.");
        })
        .Triggers(TestExecute);

    Target Test => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            testFilter = new List<string> { NameSegment.TestType.Acceptance, NameSegment.TestType.Functional, NameSegment.TestType.Unit };
            Log.Information($"Test filter set to {string.Join(", ", testFilter)} Tests.");
        })
        .Triggers(TestExecute);

    Target TestExecute => _ => _
        .DependsOn(Compile, StartApi)
        .Executes(() =>
        {
            DotNetTest(_ => _
                    .SetResultsDirectory(TestResultsDirectory)
                    .EnableCollectCoverage()
                    .SetCoverletOutputFormat(CoverletOutputFormat.cobertura)
                    .SetDataCollector("XPlat Code Coverage")
                    .EnableNoRestore()
                    .EnableNoBuild()
                    .CombineWith(TestProjects.Where(project => testFilter.Contains(project.Key)), (_, testProject) => _
                        .SetProjectFile(testProject.Value)
                        .SetLoggers($"trx;LogFileName={testProject.Value.Name}.trx")
                        .SetCoverletOutput(TestResultsDirectory / $"{testProject.Value.Name}.xml")
                        .SetFilter(testProject.Key)),
                completeOnFailure: true);
        })
        .Triggers(StopApi);
}

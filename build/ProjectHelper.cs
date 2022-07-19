using System;
using System.Linq;
using Nuke.Common.ProjectModel;
using Nuke.Common.Utilities.Collections;

public class ProjectHelper
{
    public static Project Self(Project instance) => instance;

    public static string GetTestProjectType(Project s) => s.Name.Split('.', StringSplitOptions.RemoveEmptyEntries)
        .SkipUntil(NameSegment.IsTest)
        .Skip(1)
        .First();
}

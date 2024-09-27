using NodeDev.Core;
using NodeDev.Blazor.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NodeDev.Core.Class;

namespace NodeDev.Tests;

public class ProjectServiceTest
{
    [Fact]
    public void TestsChangeProject()
    {
        var project = new Project(Guid.NewGuid());
        var projectService = new ProjectService();

        projectService.ChangeProject(project);

        Assert.Equal(project, projectService.Project);
    }

    [Fact]
    public void TestsProjectChangedEvent()
    {
        var project = new Project(Guid.NewGuid());
        var projectService = new ProjectService();
        bool isEventTriggered = false;

        projectService.ProjectChanged += () => isEventTriggered = true;
        projectService.ChangeProject(project);

        Assert.True(isEventTriggered);
    }
}


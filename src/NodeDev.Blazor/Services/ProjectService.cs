﻿using NodeDev.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeDev.Blazor.Services
{
    /// <summary>
    /// Service used to keep a singleton of the project throughout the application.
    /// </summary>
    public class ProjectService
    {
        public Project Project { get; private set; }

        public delegate void ProjectChangedHandler();
        /// <summary>
        /// Event used to notify subscribers when the current project has changed.
        /// </summary>
        public event ProjectChangedHandler? ProjectChanged;

        /// <summary>
        /// Instanciates a default project as the current project.
        /// </summary>
        public ProjectService()
        {
            Project = Project.CreateNewDefaultProject();
        }

        /// <summary>
        /// Changes the current project and notifies all subscribers of <see cref="ProjectChanged" /> that the project has changed.
        /// </summary>
        /// <param name="project"></param>
        public void ChangeProject(Project project)
        {
            Project = project;
            ProjectChanged?.Invoke();
        }
    }
}
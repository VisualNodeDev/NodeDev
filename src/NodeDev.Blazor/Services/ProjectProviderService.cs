using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeDev.Blazor.Services
{
    internal class ProjectProviderService
    {
        private Core.Project _project;

        public Core.Project Project
        {
            get => _project;
            set => _project = value;
        }

        public ProjectProviderService()
        {
            _project = Core.Project.CreateNewDefaultProject();
        }
    }
}

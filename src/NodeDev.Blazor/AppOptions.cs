using Microsoft.Extensions.Configuration;
using NodeDev.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace NodeDev.Blazor;

public class AppOptions
{
    public string? ProjectsDirectory { get; set; } = null;
}

# NodeDev

[![.NET](https://github.com/snakex64/NodeDev/actions/workflows/dotnet.yml/badge.svg)](https://github.com/snakex64/NodeDev/actions/workflows/dotnet.yml)

Visual Programming environment made with Blazor and BlazorDiagrams.

Check out the [roadmap](https://github.com/orgs/VisualNodeDev/projects/2/views/1)

# What we do
NodeDev is an alpha tool allowing you to create entire software using a visual flow-base interface instead of the usual written code style.
It is still in very early alpha, full of bugs and pretty slow, but it's improving fast! Check out a recent demo clip

https://github.com/VisualNodeDev/NodeDev/assets/39806655/f4a8e7fd-08a1-4f37-8cae-0b7a7c2d4759

# Tell me more !
NodeDev runs in .NET and allows you to manipulate any .NET objects, call any method, create an .NET type you want.
You can create classes, methods and properties as you would in C# and mix them with real pre-existing types seemlessly.

The types you create in the interface are added to a dynamic assembly at runtime, allowing you to use them in your program like normal .NET types (because they are) !
A simple example is the ability to create a List<> of a custom class, or use reflection with the NodeDev classes.

# Hopes and future
The goal is to allow NodeDev to export a project to an actual .dll and/or an independant executable file. There are a lot of things funky with the UI and tons of missing features required to make it usable in real projects but the core has been very easy to add on top of and things have been moving steadily.
There are nice features that I would love to add such as the ability to search and add nuget packages, create solutions of multiple projects, import .dlls directly and/or link to csproj, etc... The roadmap is but a small set of features.

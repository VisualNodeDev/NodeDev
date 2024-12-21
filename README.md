# NodeDev

[![.NET](https://github.com/snakex64/NodeDev/actions/workflows/dotnet.yml/badge.svg)](https://github.com/snakex64/NodeDev/actions/workflows/dotnet.yml)

Visual Programming environment made with Blazor and BlazorDiagrams.

Check out the [roadmap](https://github.com/orgs/VisualNodeDev/projects/2/views/1)

# What we do
NodeDev is an alpha tool allowing you to create entire software using a visual flow-base interface instead of the usual written code style.
It is still in very early alpha and full of bugs but it's improving fast! Check out a recent demo clip
(Note that any debugging capabilities such as visually seeing the execution of nodes (connections/links flashing red as they are executed) and being able to mouse over connections to see values is completely gone since July 2024. We are in the process of adding back debugging capabilities now that the execution of nodes is done natively)

https://github.com/VisualNodeDev/NodeDev/assets/39806655/f4a8e7fd-08a1-4f37-8cae-0b7a7c2d4759

Here's a newer demo with the generated IL code shown:

https://github.com/user-attachments/assets/b8c915b9-2ade-43ff-920e-1efabf299ecf


# Tell me more !
NodeDev runs in .NET and allows you to manipulate any .NET objects, call any method, create an .NET type you want.
You can create classes, methods and properties as you would in C# and mix them with real pre-existing types seemlessly.

The types you create in the interface are added to a dynamic assembly at runtime, allowing you to use them in your program like normal .NET types (because they are) !
A simple example is the ability to create a List<> of a custom class, or use reflection with the NodeDev classes.

Since July 2024, the method's content are finally generated with 100% native IL code, nothing is interpreted anymore and this should allow near native execution time. Many optimisations are still left to be done such as reducing the stack size of each methods but  in nearly all applications this should never be an issue.

# Hopes and future
The goal is to allow NodeDev to export a project to an actual .dll and/or an independant executable file. There are a lot of things funky with the UI and tons of missing features required to make it usable in real projects but the core has been very easy to add on top of and things have been moving steadily.
There are nice features that I would love to add such as the ability to search and add nuget packages, create solutions of multiple projects, import .dlls directly and/or link to csproj, etc... The roadmap is but a small set of features.

// PATH: RDA.Application/AssemblyReference.cs
using System.Reflection;

namespace RDA.Application;

public static class AssemblyReference
{
    public static readonly Assembly Assembly = typeof(AssemblyReference).Assembly;
}

using System;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

namespace RemoveOldPackageRestore
{
    class Program
    {
        static int Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.Error.WriteLine(
                     @"Usage: {0} SOURCE-ROOT

Removes the following artifacts from the *.csproj files in SOURCE-ROOT:

- The ""EnsureNuGetPackageBuildImports"" MSBuild Target.
- The Import of ""$(SolutionDir)\.nuget\NuGet.targets"".
- The ""SolutionDir"" property definition.

When project file is modified, a backup file (*.csproj.orig) is created
next to it.
",
                    typeof(Program).Assembly.GetName().Name);
                return -1;
            }

            try
            {
                foreach (string fileName in Directory.EnumerateFiles(args[0], "*.csproj", SearchOption.AllDirectories))
                {
                    var doc = XDocument.Load(fileName, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

                    bool modified = false;
                    using (var reader = doc.CreateReader())
                    {
                        var mgr = new XmlNamespaceManager(reader.NameTable);
                        mgr.AddNamespace("msb", "http://schemas.microsoft.com/developer/msbuild/2003");

                        // <Target Name="EnsureNuGetPackageBuildImports" BeforeTargets="PrepareForBuild">
                        //  <PropertyGroup>
                        //  <ErrorText>This project references NuGet package(s) that....</ErrorText>
                        //  </PropertyGroup>
                        //  <Error Condition="!Exists('$(SolutionDir)\.nuget\NuGet.targets')" Text="$([System.String]::Format('$(ErrorText)', '$(SolutionDir)\.nuget\NuGet.targets'))" />
                        // </Target>
                        var targets = doc.XPathSelectElements(@"/msb:Project/msb:Target[@Name='EnsureNuGetPackageBuildImports']", mgr);
                        foreach (var target in targets)
                        {
                            target.Remove();
                            modified = true;
                        }

                        // <Import Project="$(SolutionDir)\.nuget\NuGet.targets" Condition="Exists('$(SolutionDir)\.nuget\NuGet.targets')" />
                        var imports = doc.XPathSelectElements(@"/msb:Project/msb:Import[@Project='$(SolutionDir)\.nuget\NuGet.targets']", mgr);
                        foreach (var import in imports)
                        {
                            import.Remove();
                            modified = true;
                        }

                        // <SolutionDir Condition="$(SolutionDir) == '' Or $(SolutionDir) == '*Undefined*'">..\..\..\..\</SolutionDir>
                        var solutionDirs = doc.XPathSelectElements(@"/msb:Project/msb:PropertyGroup/msb:SolutionDir", mgr);
                        foreach (var solutionDir in solutionDirs)
                        {
                            var condition = solutionDir.Attribute("Condition");
                            if ("$(SolutionDir) == '' Or $(SolutionDir) == '*Undefined*'".Equals(condition?.Value))
                            {
                                solutionDir.Remove();
                                modified = true;
                            }
                            else
                            {
                                Console.WriteLine("{0}: warning: skipped unknown SolutionDir-Property.", fileName);
                            }
                        }
                    }

                    if (modified)
                    {
                        Console.WriteLine(fileName);
                        File.Copy(fileName, fileName + ".orig");
                        doc.Save(fileName);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                return ex.HResult;
            }

            return 0;
        }
    }
}

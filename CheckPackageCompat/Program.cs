using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NuGet.Common;
using NuGet.Packaging;
using NuGet.Packaging.Rules;

namespace CheckPackageCompat
{
    class Program
    {
        static int Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.Error.WriteLine(@"Usage: {0} PACKAGES-PATH [-csv]

Checks all packages (*.nupkg) in PACKAGE-PATH for compatiblity with PackageReference.
Issues shown are the same that the Migration Tool inside Visual Studio 2017 would show.
With the -csv option, output can be redirected to a .csv file for further processing.
",
                    typeof(Program).Assembly.GetName().Name);
                return -1;
            }

            string packagePath = args[0];
            bool csv = args.Length > 1 && args[1].Equals("-csv", StringComparison.OrdinalIgnoreCase);

            try
            {
                bool hasIssues = false;
                foreach (var file in Directory.EnumerateFiles(packagePath, "*.nupkg", SearchOption.AllDirectories))
                {
                    using (var reader = new PackageArchiveReader(file))
                    {
                        var packageRules = RuleSet.PackagesConfigToPackageReferenceMigrationRuleSet;
                        var issues = new List<PackagingLogMessage>();

                        foreach (var rule in packageRules)
                        {
                            var foundIssues = rule.Validate(reader).OrderBy(p => p.Code.ToString(), StringComparer.CurrentCulture);
                            if (foundIssues.Any())
                            {
                                hasIssues = true;
                            }
                            issues.AddRange(foundIssues);
                        }

                        if (issues.Any())
                        {
                            if (csv)
                            {
                                foreach (var issue in issues)
                                {
                                    Console.Write(Path.GetFileName(file));
                                    Console.Write(';');
                                    Console.Write(issue.Code);
                                    Console.Write(';');
                                    Console.WriteLine();
                                }
                            }
                            else
                            {
                                Console.WriteLine(file);
                                foreach (var issue in issues)
                                {
                                    Console.WriteLine("\t" + issue.Message);
                                }
                            }
                        }
                    }
                }

                if (hasIssues)
                    return 1;
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

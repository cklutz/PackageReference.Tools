# PackageReference.Tools
Tossed together tools to help when [migrating from packages.config to PackageReference](https://docs.microsoft.com/en-us/nuget/reference/migrate-packages-config-to-package-reference).

Basically, use the tools as follows.

1. Run the `CheckPackageCompat` tool on your `packages` folder.
   Evaluate the results and determine if the issues need to be dealt with
   (via package update, ignore issues if possible, etc.)

2. Do the actual migration using the Visual Studio 2017 (15.7+) feature:
   `Migrate packages.config to PackageReference...`.
   Hint: if the option doesn't show for you, open the Package Manager
   console once.

3. After your migrated build compiles, get rid of the old style restoring
   functionality by runnig `RemoveOldPackageRestore` on your sources-folder.



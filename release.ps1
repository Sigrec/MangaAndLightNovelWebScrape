param (
    [Alias("v")]
    [ValidatePattern('\d{1,}\.\d{1,}\.\d{1,}')]
    [Parameter(Position=0,
               mandatory=$true,
               HelpMessage="https://learn.microsoft.com/en-us/dotnet/csharp/versioning")]
    [String]$version
)

# Update csproj file
$csprojPath = "C:\MangaAndLightNovelWebScrape\Src\MangaAndLightNovelWebScrape.csproj"
$csproj = [xml](Get-Content -Path $csprojPath)
$csprojNode = $csproj.Project.PropertyGroup
$csprojNode.PackageVersion = $version
$csprojNode.Version = $version
$csprojNode.FileVersion = "$($version).0"
$csproj.Save($csprojPath)

#Move README to Src directory
Move-Item -Path "C:\MangaAndLightNovelWebScrape\README.md" -Destination "C:\MangaAndLightNovelWebScrape\Src"

# Pack to create Release
dotnet pack -c Release

# Push to nuget
dotnet nuget push "C:\MangaAndLightNovelWebScrape\Src\bin\Release\MangaAndLightNovelWebScrape.$($version).nupkg" --api-key $env:MangaLightNovelWebScrapeNugetApiKey --source "https://api.nuget.org/v3/index.json"

# Move README back to Solution root before pushing to git
Move-Item -Path "C:\MangaAndLightNovelWebScrape\Src\README.md" -Destination "C:\MangaAndLightNovelWebScrape"

# Push changes to git
git add . | git commit -m "v$version" | git push origin master
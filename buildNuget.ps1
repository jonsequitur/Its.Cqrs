rm -Force -Recurse .\NuGet
mkdir NuGet
nuget pack ".\Domain\Domain.csproj" -Properties "Configuration=Release;Platform=AnyCPU" -OutputDirectory ".\NuGet"
nuget pack ".\Domain.Api\Domain.Api.csproj" -Properties "Configuration=Release;Platform=AnyCPU" -OutputDirectory ".\NuGet"
nuget pack ".\Domain.Sql\Domain.Sql.csproj" -Properties "Configuration=Release;Platform=AnyCPU" -OutputDirectory ".\NuGet"
nuget pack ".\Domain.Testing\Domain.Testing.csproj" -Properties "Configuration=Release;Platform=AnyCPU" -OutputDirectory ".\NuGet"
nuget pack ".\Recipes\Recipes.csproj" -Properties "Configuration=Release;Platform=AnyCPU" -OutputDirectory ".\NuGet"



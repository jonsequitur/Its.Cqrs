rm -Force -Recurse .\NuGet
mkdir NuGet

@(
	".\Domain\Domain.csproj", 
	".\Domain.Api\Domain.Api.csproj",
	".\Domain.Sql\Domain.Sql.csproj",
	".\Domain.Testing\Domain.Testing.csproj",
	".\Recipes\Recipes.csproj"
) | foreach {
	nuget pack $_ -Properties "Configuration=Release;Platform=AnyCPU" -OutputDirectory ".\NuGet"
}

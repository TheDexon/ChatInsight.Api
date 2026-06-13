# Добавь в ChatInsight.Api.csproj в <ItemGroup> с пакетами:

  <PackageReference Include="Pgvector" Version="0.3.0" />
  <PackageReference Include="Pgvector.EntityFrameworkCore" Version="0.2.2" />

(версии должны быть совместимы с Npgsql 9 / EF Core 9; если NuGet предложит
свежее в рамках 0.x — бери)

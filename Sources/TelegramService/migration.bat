
dotnet ef migrations add Intial -c TgServiceDbContext -o Database/Migrations -- --environment Migration
dotnet ef database update -- --environment Migration
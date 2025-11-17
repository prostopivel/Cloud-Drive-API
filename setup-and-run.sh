cd src/Services/Auth/Auth.Infrastructure
dotnet ef migrations add "InitialCreate" -o "Data/Migrations"
dotnet ef database update

docker-compose up -d postgres redis rabbitmq auth-service
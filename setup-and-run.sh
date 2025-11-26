cd src/Services/Auth/Auth.Infrastructure
dotnet ef migrations add "InitialCreate" -o "Data/Migrations"
dotnet ef database update

docker-compose up -d auth-db redis rabbitmq auth-service

docker-compose up -d metadata-db redis rabbitmq filemetadata-service
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace FileMetadata.Infrastructure.Data
{
    public class FileMetadataDbContextFactory : IDesignTimeDbContextFactory<FileMetadataDbContext>
    {
        public FileMetadataDbContext CreateDbContext(string[] args)
        {
            // Current dir: \src\Services\FileMetadata\FileMetadata.Infrastructure
            var apiProjectPath = Path.GetFullPath(
                Path.Combine(Directory.GetCurrentDirectory(), "../FileMetadata.API"));

            var configuration = new ConfigurationBuilder()
                .SetBasePath(apiProjectPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.Development.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var connectionString = configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            }

            Console.WriteLine($"Using connection string: {connectionString}");
            Console.WriteLine($"Config base path: {apiProjectPath}");

            var optionsBuilder = new DbContextOptionsBuilder<FileMetadataDbContext>();
            optionsBuilder.UseNpgsql(connectionString);

            return new FileMetadataDbContext(optionsBuilder.Options);
        }
    }
}

namespace FileMetadata.IntegrationTests.DTOs
{
    public record OwnershipValidationResponse
    {
        public bool BelongsToUser { get; init; }
    }
}

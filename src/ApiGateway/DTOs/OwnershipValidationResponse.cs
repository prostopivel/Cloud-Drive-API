namespace ApiGateway.DTOs
{
    public record OwnershipValidationResponse
    {
        public bool BelongsToUser { get; init; }
    }
}

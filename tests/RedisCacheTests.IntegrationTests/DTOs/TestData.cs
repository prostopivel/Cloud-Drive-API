namespace RedisCacheTests.IntegrationTests.DTOs
{
    public record TestData
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Value { get; set; }
    }
}

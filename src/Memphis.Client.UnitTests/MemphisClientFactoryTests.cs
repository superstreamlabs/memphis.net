namespace Memphis.Client.UnitTests
{
    public class MemphisClientFactoryTests
    {
        [Theory]
        [InlineData("http://www.google.com", "www.google.com")]
        [InlineData("https://www.yahoo.com", "www.yahoo.com")]
        [InlineData("http://http.http.http://", "http.http.http://")]
        public void GivenUrl_WhenNormalizeHost_ThenReturnNormalizedHost(string inputUri, string expectedValue)
        {
            var actualNormalizedValue = MemphisClientFactory.NormalizeHost(inputUri);
            Assert.Equal(expectedValue, actualNormalizedValue);
        }
    }
}
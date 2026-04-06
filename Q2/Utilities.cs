namespace Q2
{
    public class Utilities
    {
        private static string _baseUrl = null!; 

        public static void Initialize(IConfiguration configuration)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            _baseUrl = configuration["GivenAPIBaseUrl"]
                ?? throw new InvalidOperationException("GivenAPIBaseUrl is not configured in appsettings.json");
        }

        /// <summary>
        /// Generates an absolute URL by combining a base URL with a relative path.
        /// </summary>
        /// <param name="path">The relative path to append to the base URL. Can be null or empty.</param>
        /// <returns>A string representing the absolute URL formed by combining the base URL and the provided path.</returns>
        /// <exception cref="InvalidOperationException">Thrown if Utilities has not been initialized with a base URL.</exception>
        public static string GetAbsoluteUrl(string path)
        {
            if (_baseUrl == null)
                throw new InvalidOperationException("UrlUtilities has not been initialized. Call Initialize(IConfiguration) first.");

            // Remove the '/' character at the beginning or end
            string baseUrl = _baseUrl.TrimEnd('/');
            path = path?.TrimStart('/') ?? string.Empty;

            // Combine baseUrl and path
            return $"{baseUrl}/{path}";
        }
    }
}

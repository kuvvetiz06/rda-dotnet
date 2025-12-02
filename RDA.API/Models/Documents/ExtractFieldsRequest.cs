namespace RDA.API.Models.Documents
{
    public class ExtractFieldsRequest
    {
        /// <summary>
        /// Yüklenecek PDF dosyası.
        /// </summary>
        public IFormFile File { get; set; } = default!;

        /// <summary>
        /// LLM'e verilecek talimat/prompt.
        /// </summary>
        public string Prompt { get; set; } = string.Empty;
    }
}

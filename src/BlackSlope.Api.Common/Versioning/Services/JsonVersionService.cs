using System.IO.Abstractions;
using System.Text.Json;
using BlackSlope.Api.Common.Versioning.Interfaces;
using Microsoft.AspNetCore.Hosting;

namespace BlackSlope.Api.Common.Versioning.Services
{
    public class JsonVersionService : IVersionService
    {
        private readonly IFileSystem _fileSystem;
        private readonly IWebHostEnvironment _hostingEnvironment;

        public JsonVersionService(IFileSystem fileSystem, IWebHostEnvironment hostingEnvironment)
        {
            _fileSystem = fileSystem;
            _hostingEnvironment = hostingEnvironment;
        }

        public Version GetVersion()
        {
            var filepath = _fileSystem.Path.Combine(_hostingEnvironment.ContentRootPath, "..", "Blackslope.Api.Common", "Versioning", "version.json");
            var fileContents = _fileSystem.File.ReadAllText(filepath);
            return JsonSerializer.Deserialize<Version>(fileContents);
        }
    }
}

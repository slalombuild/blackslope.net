using System.IO.Abstractions;
using BlackSlope.Api.Common.Versioning.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Newtonsoft.Json.Linq;

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
            var filepath = _fileSystem.Path.Combine(_hostingEnvironment.ContentRootPath, "Common", "Version", "version.json");
            var fileContents = _fileSystem.File.ReadAllText(filepath);
            dynamic task = JObject.Parse(fileContents);
            return new Version(task.version.ToString());
        }
    }
}

using System.Text;
using XamlX.TypeSystem;

namespace NFMWorld.XamlX.Core
{
    public class XamlFileSource(string filePath, string xml) : IFileSource
    {
        public string FilePath { get; } = filePath;
        public byte[] FileContents { get; } = Encoding.UTF8.GetBytes(xml);
    }
}
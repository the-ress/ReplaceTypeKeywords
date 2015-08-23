using ReplaceTypeKeywords.Core;
using System;
using System.IO;
using System.Text;

namespace ReplaceTypeKeywords
{
    internal class Program
    {
        internal static void Main(String[] args)
        {
            var fileNames = Directory.GetFiles(Environment.CurrentDirectory, "*.cs", SearchOption.AllDirectories);
            foreach (var fileName in fileNames)
            {
                String source;
                Encoding encoding;

                using (var inStream = new StreamReader(fileName, Encoding.Default, detectEncodingFromByteOrderMarks: true))
                {
                    source = inStream.ReadToEnd();
                    encoding = inStream.CurrentEncoding;
                }

                source = TypeKeywordsReplacer.Process(source);
                File.WriteAllText(fileName, source, encoding);
            }
        }
    }
}

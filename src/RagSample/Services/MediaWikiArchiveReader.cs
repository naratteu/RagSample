using Microsoft.Extensions.Configuration;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml;
using RagSample.Models;
using RagSample.Configurations;

namespace RagSample.Services;

public sealed class MediaWikiArchiveReader(
    IConfiguration Configuration)
{
    public async IAsyncEnumerable<MediaWikiPageData> ReadWikipediaDumpAsync(
        IReadOnlyList<string> documentIdList,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var config = Configuration.GetSection("MediaWikiArchive").Get<MediaWikiArchiveConfig>();
        ArgumentNullException.ThrowIfNull(config);

        var filePath = Environment.ExpandEnvironmentVariables(config.ArchiveXmlFilePath);
        await using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
        using var reader = XmlReader.Create(fs, new XmlReaderSettings { Async = true, IgnoreWhitespace = true });

        var currentId = default(string);
        var currentTitle = default(string);
        var currentText = default(string);
        var insidePage = false;

        while (await reader.ReadAsync().ConfigureAwait(false))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (reader.NodeType == XmlNodeType.Element && reader.Name == "page")
            {
                currentId = null;
                currentTitle = null;
                currentText = null;
                insidePage = true;

                // 내부 태그 탐색
                while (insidePage && await reader.ReadAsync().ConfigureAwait(false))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        if (reader.Name == "id" && reader.Depth == 2)
                        {
                            currentId = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
                        }
                        else if (reader.Name == "title" && reader.Depth == 2)
                        {
                            currentTitle = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
                        }
                        else if (reader.Name == "text" && reader.Depth == 3)
                        {
                            currentText = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
                        }
                    }
                    else if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "page")
                    {
                        insidePage = false;
                    }
                }

                // title과 text가 모두 존재하면 반환
                if (!string.IsNullOrEmpty(currentId) &&
                    !string.IsNullOrEmpty(currentTitle) &&
                    !string.IsNullOrEmpty(currentText))
                {
                    if (documentIdList.Any())
                    {
                        if (!documentIdList.Contains(currentId))
                            continue;
                    }

                    yield return new MediaWikiPageData(currentId, currentTitle, RemoveNestedBlocks(currentText));
                }
            }
        }
    }

    private string RemoveNestedBlocks(string input)
    {
        StringBuilder output = new StringBuilder();
        int braceLevel = 0;
        int bracketLevel = 0;
        int tableLevel = 0;
        int i = 0;

        while (i < input.Length)
        {
            if (i < input.Length - 1 && input[i] == '{' && input[i + 1] == '{')
            {
                braceLevel++;
                i += 2;
            }
            else if (i < input.Length - 1 && input[i] == '}' && input[i + 1] == '}')
            {
                if (braceLevel > 0)
                {
                    braceLevel--;
                }
                i += 2;
            }
            else if (i < input.Length - 1 && input[i] == '[' && input[i + 1] == '[')
            {
                bracketLevel++;
                i += 2;
            }
            else if (i < input.Length - 1 && input[i] == ']' && input[i + 1] == ']')
            {
                if (bracketLevel > 0)
                {
                    bracketLevel--;
                }
                i += 2;
            }
            else if (i < input.Length - 1 && input[i] == '{' && input[i + 1] == '|')
            {
                tableLevel++;
                i += 2;
            }
            else if (i < input.Length - 1 && input[i] == '|' && input[i + 1] == '}')
            {
                if (tableLevel > 0)
                {
                    tableLevel--;
                }
                i += 2;
            }
            else
            {
                if (braceLevel == 0 && bracketLevel == 0 && tableLevel == 0)
                {
                    output.Append(input[i]);
                }
                i++;
            }
        }

        return output.ToString();
    }
}

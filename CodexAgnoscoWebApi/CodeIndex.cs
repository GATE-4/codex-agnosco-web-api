namespace CodexAgnoscoWebApi;

using Microsoft.CodeAnalysis.CSharp;
using System.Net.Http.Json;

public class CodeIndex
{
    private readonly IHttpClientFactory _http;
    public CodeIndex(IHttpClientFactory http) { _http = http; }

    public async Task IndexRepository(string repoPath)
    {
        var chunks = new List<object>();

        foreach (var file in Directory.EnumerateFiles(repoPath, "*.cs", SearchOption.AllDirectories))
        {
            var code = await File.ReadAllTextAsync(file);
            var tree = CSharpSyntaxTree.ParseText(code);
            var root = await tree.GetRootAsync();
            
            foreach (var method in root.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>())
            {
                chunks.Add(new
                {
                    file_path = file,
                    symbol = method.Identifier.Text,
                    code = method.ToFullString()
                });
            }

            foreach (var @class in root.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax>())
            {
                chunks.Add(new
                {
                    file_path = file,
                    symbol = @class.Identifier.Text,
                    code = @class.ToFullString()
                });
            }
        }
        
        var http = _http.CreateClient();
        await http.PostAsJsonAsync("http://localhost:8001/index", chunks);
    }


    public string GetCodeByLocation(string filePath, int lineNumber)
    {
        var lines = File.ReadAllLines(filePath);
        int start = Math.Max(0, lineNumber - 5);
        int end = Math.Min(lines.Length, lineNumber + 15);
        return string.Join("\n", lines.Skip(start).Take(end - start));
    }
}

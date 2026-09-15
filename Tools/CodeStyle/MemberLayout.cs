using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

static class MemberLayout
{
    static readonly HashSet<string> UnityTypes = new("Vector2 Vector3 Vector4 Vector2Int Vector3Int Quaternion Color Color32 Rect RectInt Bounds LayerMask GameObject Transform Camera Font Texture Texture2D RenderTexture Material Shader Animation AnimationClip Animator Canvas RectTransform GUIStyle UIDocument VisualElement Button Label TextField ScrollView PanelSettings StyleSheet ThemeStyleSheet VisualTreeAsset Mesh Renderer MeshRenderer MeshFilter Collider RaycastHit Ray Plane TouchScreenKeyboard".Split(' '));

    static int Category(FieldDeclarationSyntax field)
    {
        var type = field.Declaration.Type;
        var names = type.DescendantTokens().Select(t => t.ValueText).ToArray();

        if (names.Any(UnityTypes.Contains) || type.ToString().Contains("UnityEngine"))
        {
            return 2;
        }

        if (type.DescendantNodesAndSelf().OfType<PredefinedTypeSyntax>().Any())
        {
            return 1;
        }

        return 0;
    }

    static bool HasOrderedInitializer(FieldDeclarationSyntax field) => field.Declaration.Variables.Any(v => v.Initializer != null && v.Initializer.Value is not LiteralExpressionSyntax && !(v.Initializer.Value is PrefixUnaryExpressionSyntax unary && unary.Operand is LiteralExpressionSyntax));

    public static string Format(string text)
    {
        foreach (var symbols in new[] { new[] { "UNITY_EDITOR", "DEVELOPMENT_BUILD" }, new[] { "ARENA_HEADLESS" }, Array.Empty<string>() })
        {
            var options = new CSharpParseOptions(preprocessorSymbols: symbols);
            var root = CSharpSyntaxTree.ParseText(text, options).GetRoot();
            var edits = new List<(int Start, int Length, string Text)>();

            foreach (var type in root.DescendantNodes().OfType<TypeDeclarationSyntax>())
            {
                var fields = new List<FieldDeclarationSyntax>();

                void Flush()
                {
                    if (fields.Count == 0)
                    {
                        return;
                    }

                    var ordered = fields.OrderBy(Category).ToArray();
                    if (!fields.Where(HasOrderedInitializer).SequenceEqual(ordered.Where(HasOrderedInitializer)) || fields.Any(f => f.Declaration.Variables.Any(v => v.Initializer != null && v.Initializer.Value.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>().Any(n => fields.Any(other => other != f && other.Declaration.Variables.Any(ov => ov.Identifier.ValueText == n.Identifier.ValueText) && (fields.IndexOf(other) < fields.IndexOf(f)) != (Array.IndexOf(ordered, other) < Array.IndexOf(ordered, f)))))))
                    {
                        ordered = fields.ToArray();
                    }
                    var lineStart = text.LastIndexOf('\n', fields[0].SpanStart - 1) + 1;
                    // Attributes and comments move together with each declaration.
                    var start = fields[0].FullSpan.Start;
                    var end = fields[^1].FullSpan.End;
                    var indent = new string(' ', root.SyntaxTree.GetText().Lines.GetLineFromPosition(fields[0].SpanStart).ToString().TakeWhile(char.IsWhiteSpace).Count());
                    var chunks = new List<string>();
                    var previous = -1;
                    var widths = ordered.GroupBy(Category).ToDictionary(g => g.Key, g => g.Max(f => (indent + string.Join(" ", f.Modifiers.Select(m => m.Text)) + (f.Modifiers.Count > 0 ? " " : "") + f.Declaration.Type).Length) / 4 * 4 + 4);

                    foreach (var field in ordered)
                    {
                        int category = Category(field);
                        var raw = field.ToFullString().Trim('\r', '\n');
                        int typeEnd = field.Declaration.Type.Span.End - field.FullSpan.Start;
                        int nameStart = field.Declaration.Variables[0].Identifier.SpanStart - field.FullSpan.Start;
                        var original = field.ToFullString();
                        int prefixStart = original.LastIndexOf('\n', typeEnd - 1) + 1;
                        int width = typeEnd - prefixStart;
                        string tabs = new string('\t', Math.Max(1, (widths[category] - width + 3) / 4));
                        raw = (original.Substring(0, typeEnd) + tabs + original.Substring(nameStart)).Trim('\r', '\n');

                        if (previous != -1 && previous != category)
                        {
                            chunks.Add("");
                        }

                        chunks.Add(raw);
                        previous = category;
                    }

                    edits.Add((start, end - start, "\r\n" + string.Join("\r\n", chunks) + "\r\n"));
                    fields.Clear();
                }

                foreach (var member in type.Members)
                {
                    if (member is not FieldDeclarationSyntax field || member.ContainsDirectives)
                    {
                        Flush();
                        continue;
                    }

                    fields.Add(field);
                }

                Flush();
            }

            foreach (var edit in edits.OrderByDescending(e => e.Start))
            {
                text = text.Remove(edit.Start, edit.Length).Insert(edit.Start, edit.Text);
            }

            if (CSharpSyntaxTree.ParseText(text, options).GetDiagnostics().Any(d => d.Severity == DiagnosticSeverity.Error))
            {
                throw new Exception("Member formatting produced invalid syntax.");
            }
        }

        return text;
    }
}




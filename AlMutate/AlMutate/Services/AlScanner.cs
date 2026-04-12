using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;
using AlMutate.Models;

namespace AlMutate.Services;

public static class AlScanner
{
    /// <summary>
    /// Scans a single .al file for mutation candidates using all provided operators.
    /// </summary>
    public static List<MutationCandidate> ScanFile(string filePath, List<MutationOperator> operators)
    {
        var absPath = Path.GetFullPath(filePath);
        var source = File.ReadAllText(absPath);
        var lines = source.Split('\n');

        var tree = SyntaxTree.ParseObjectText(source, absPath);
        var root = tree.GetRoot();

        var visitor = new MutationVisitor(tree, lines, absPath, operators);
        visitor.Visit(root);
        return visitor.Candidates;
    }

    /// <summary>
    /// Recursively scans all .al files in the given directory for mutation candidates.
    /// </summary>
    public static List<MutationCandidate> ScanDirectory(string directory, List<MutationOperator> operators)
    {
        var results = new List<MutationCandidate>();

        foreach (var file in Directory.GetFiles(directory, "*.al", SearchOption.AllDirectories).OrderBy(f => f))
        {
            results.AddRange(ScanFile(file, operators));
        }

        return results;
    }

    // -----------------------------------------------------------------------
    // Inner walker
    // -----------------------------------------------------------------------

    private sealed class MutationVisitor : SyntaxWalker
    {
        private readonly SyntaxTree _tree;
        private readonly string[] _lines;
        private readonly string _filePath;
        private readonly List<MutationOperator> _operators;

        public List<MutationCandidate> Candidates { get; } = new();

        // Operator look-up maps (built once at construction time for speed)

        // node_type -> operator_token -> operators  (for binary / unary / boolean / exit)
        private readonly Dictionary<string, Dictionary<string, List<MutationOperator>>> _tokenOps;

        // call_expression operators grouped by identifier
        private readonly Dictionary<string, List<MutationOperator>> _callOps;

        public MutationVisitor(
            SyntaxTree tree,
            string[] lines,
            string filePath,
            List<MutationOperator> operators)
        {
            _tree = tree;
            _lines = lines;
            _filePath = filePath;
            _operators = operators;

            // Build token-based lookup
            _tokenOps = new Dictionary<string, Dictionary<string, List<MutationOperator>>>(StringComparer.OrdinalIgnoreCase);
            _callOps = new Dictionary<string, List<MutationOperator>>(StringComparer.OrdinalIgnoreCase);

            foreach (var op in operators)
            {
                if (op.NodeType == "call_expression")
                {
                    var id = op.Identifier ?? "";
                    if (!_callOps.TryGetValue(id, out var list))
                        _callOps[id] = list = new();
                    list.Add(op);
                }
                else if (op.OperatorToken != null)
                {
                    if (!_tokenOps.TryGetValue(op.NodeType, out var byToken))
                        _tokenOps[op.NodeType] = byToken = new(StringComparer.OrdinalIgnoreCase);
                    if (!byToken.TryGetValue(op.OperatorToken, out var opList))
                        byToken[op.OperatorToken] = opList = new();
                    opList.Add(op);
                }
            }
        }

        // -----------------------------------------------------------------------
        // Helper methods
        // -----------------------------------------------------------------------

        private int GetLine(SyntaxNode node) =>
            _tree.GetLineSpan(node.Span).StartLinePosition.Line + 1; // 1-based

        private string GetRawLine(int oneBasedLine) =>
            oneBasedLine >= 1 && oneBasedLine <= _lines.Length
                ? _lines[oneBasedLine - 1]
                : string.Empty;

        /// <summary>
        /// Returns true when the node is inside a MethodDeclaration or TriggerDeclaration body.
        /// Property declarations and object-level attributes are excluded.
        /// </summary>
        private static bool IsInsideProcedureBody(SyntaxNode node)
        {
            var parent = node.Parent;
            while (parent != null)
            {
                var kind = parent.Kind;
                if (kind == SyntaxKind.MethodDeclaration || kind == SyntaxKind.TriggerDeclaration)
                    return true;
                parent = parent.Parent;
            }
            return false;
        }

        /// <summary>
        /// Builds a MutationCandidate by replacing the first occurrence of oldToken
        /// with newToken on the original source line.
        /// </summary>
        private MutationCandidate? BuildTokenReplacement(
            SyntaxNode node,
            string operatorId,
            string oldToken,
            string newToken)
        {
            int line = GetLine(node);
            var original = GetRawLine(line);

            // Replace the first occurrence of the old operator token in the line.
            // We do a whole-word-aware replace to avoid replacing inside identifiers.
            var mutated = ReplaceFirstToken(original, oldToken, newToken);
            if (mutated == original)
                return null; // no change — shouldn't happen but be safe

            return new MutationCandidate(_filePath, line, operatorId, original, mutated);
        }

        /// <summary>
        /// Replaces the first occurrence of <paramref name="oldToken"/> in <paramref name="line"/>
        /// that is surrounded by non-identifier characters (word-boundary aware for keywords like "and"/"or").
        /// </summary>
        private static string ReplaceFirstToken(string line, string oldToken, string newToken)
        {
            // For multi-char symbolic tokens (>, >=, etc.) a simple IndexOf is fine.
            // For keyword tokens (and, or, not, mod, div, true, false) we need word boundaries.
            bool isWord = oldToken.All(char.IsLetter);

            int start = 0;
            while (start < line.Length)
            {
                int idx = line.IndexOf(oldToken, start, StringComparison.OrdinalIgnoreCase);
                if (idx < 0) break;

                if (isWord)
                {
                    // Check that adjacent chars are not word chars (i.e. we have a word boundary)
                    bool leftOk = idx == 0 || !char.IsLetterOrDigit(line[idx - 1]) && line[idx - 1] != '_';
                    bool rightOk = idx + oldToken.Length >= line.Length
                                   || !char.IsLetterOrDigit(line[idx + oldToken.Length]) && line[idx + oldToken.Length] != '_';

                    if (leftOk && rightOk)
                        return line[..idx] + newToken + line[(idx + oldToken.Length)..];

                    start = idx + 1;
                }
                else
                {
                    // For symbolic operators we still need to avoid matching ">=" when looking for ">"
                    // We handle this by checking that there's no follow-on = when oldToken is ">"
                    // and similarly for "<". The SyntaxWalker already ensures we only visit nodes
                    // with the exact kind, so the node's OperatorToken.Text is exact — but the
                    // *line replacement* must not be tripped by a longer operator.
                    bool safeRight = true;
                    if (oldToken == ">" && idx + 1 < line.Length && line[idx + 1] == '=')
                        safeRight = false;
                    else if (oldToken == "<" && idx + 1 < line.Length && (line[idx + 1] == '=' || line[idx + 1] == '>'))
                        safeRight = false;

                    if (safeRight)
                        return line[..idx] + newToken + line[(idx + oldToken.Length)..];

                    start = idx + 1;
                }
            }

            return line; // unchanged
        }

        // -----------------------------------------------------------------------
        // Visit methods
        // -----------------------------------------------------------------------

        public override void VisitBinaryExpression(BinaryExpressionSyntax node)
        {
            if (IsInsideProcedureBody(node))
            {
                var opText = node.OperatorToken.Text;

                // Map SyntaxKind to node_type string
                var nodeType = node.Kind switch
                {
                    SyntaxKind.GreaterThanExpression => "comparison_expression",
                    SyntaxKind.GreaterThanOrEqualExpression => "comparison_expression",
                    SyntaxKind.LessThanExpression => "comparison_expression",
                    SyntaxKind.LessThanOrEqualExpression => "comparison_expression",
                    SyntaxKind.EqualsExpression => "comparison_expression",
                    SyntaxKind.NotEqualsExpression => "comparison_expression",
                    SyntaxKind.AddExpression => "additive_expression",
                    SyntaxKind.SubtractExpression => "additive_expression",
                    SyntaxKind.MultiplyExpression => "multiplicative_expression",
                    SyntaxKind.DivideExpression => "multiplicative_expression",
                    SyntaxKind.ModuloExpression => "multiplicative_expression",
                    SyntaxKind.IntegerDivideExpression => "multiplicative_expression",
                    SyntaxKind.LogicalAndExpression => "logical_expression",
                    SyntaxKind.LogicalOrExpression => "logical_expression",
                    _ => null
                };

                if (nodeType != null
                    && _tokenOps.TryGetValue(nodeType, out var byToken)
                    && byToken.TryGetValue(opText, out var ops))
                {
                    foreach (var op in ops)
                    {
                        var candidate = BuildTokenReplacement(node, op.Id, opText, op.Replacement!);
                        if (candidate != null)
                            Candidates.Add(candidate);
                    }
                }
            }

            base.VisitBinaryExpression(node);
        }

        public override void VisitUnaryExpression(UnaryExpressionSyntax node)
        {
            if (IsInsideProcedureBody(node) && node.Kind == SyntaxKind.UnaryNotExpression)
            {
                if (_tokenOps.TryGetValue("unary_expression", out var byToken)
                    && byToken.TryGetValue("not", out var ops))
                {
                    foreach (var op in ops)
                    {
                        if (op.IsStatementRemoval)
                        {
                            // Replace the "not " prefix with nothing
                            int line = GetLine(node);
                            var original = GetRawLine(line);
                            var mutated = RemoveNotPrefix(original, node.ToString());
                            if (mutated != original)
                                Candidates.Add(new MutationCandidate(_filePath, line, op.Id, original, mutated));
                        }
                    }
                }
            }

            base.VisitUnaryExpression(node);
        }

        private static string RemoveNotPrefix(string line, string nodeText)
        {
            // nodeText is like "not Rec.IsEmpty()"  — remove "not " from the line
            var idx = line.IndexOf("not ", StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return line;
            return line[..idx] + line[(idx + 4)..];
        }

        public override void VisitLiteralExpression(LiteralExpressionSyntax node)
        {
            if (!IsInsideProcedureBody(node))
            {
                base.VisitLiteralExpression(node);
                return;
            }

            // Boolean literals have Literal.Kind == BooleanLiteralValue
            if (node.Literal.Kind == SyntaxKind.BooleanLiteralValue
                && _tokenOps.TryGetValue("boolean", out var boolByToken))
            {
                // The actual true/false text is in the node text
                var litText = node.ToString(); // "true" or "false"
                if (boolByToken.TryGetValue(litText, out var boolOps))
                {
                    foreach (var op in boolOps)
                    {
                        var candidate = BuildTokenReplacement(node, op.Id, litText, op.Replacement!);
                        if (candidate != null)
                            Candidates.Add(candidate);
                    }
                }
            }

            base.VisitLiteralExpression(node);
        }

        public override void VisitExitStatement(ExitStatementSyntax node)
        {
            if (!IsInsideProcedureBody(node))
            {
                base.VisitExitStatement(node);
                return;
            }

            var exitValue = node.ExitValue;
            if (exitValue != null)
            {
                var valueText = exitValue.ToString();
                if (_tokenOps.TryGetValue("exit_statement", out var byToken)
                    && byToken.TryGetValue(valueText, out var ops))
                {
                    foreach (var op in ops)
                    {
                        int line = GetLine(node);
                        var original = GetRawLine(line);
                        var mutated = ReplaceFirstToken(original, valueText, op.Replacement!);
                        if (mutated != original)
                            Candidates.Add(new MutationCandidate(_filePath, line, op.Id, original, mutated));
                    }
                }
            }

            base.VisitExitStatement(node);
        }

        public override void VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            if (!IsInsideProcedureBody(node))
            {
                base.VisitInvocationExpression(node);
                return;
            }

            // Extract the method name (last segment of member access or plain identifier)
            var methodName = GetMethodName(node.Expression);
            if (methodName == null)
            {
                base.VisitInvocationExpression(node);
                return;
            }

            if (!_callOps.TryGetValue(methodName, out var ops))
            {
                base.VisitInvocationExpression(node);
                return;
            }

            // Only match if this invocation is the direct statement (not nested inside another expr)
            // i.e. its parent is an ExpressionStatement or similar — we allow both statement and
            // expression contexts since the line-based mutation works either way.

            foreach (var op in ops)
            {
                int line = GetLine(node);
                var original = GetRawLine(line);

                if (op.IsStatementRemoval)
                {
                    // Check argument_match is not set (i.e. plain removal, not bc-specific)
                    if (op.ArgumentMatch == null && op.IdentifierReplacement == null)
                    {
                        // Comment out the whole statement line
                        var mutated = CommentOutLine(original);
                        if (mutated != original)
                            Candidates.Add(new MutationCandidate(_filePath, line, op.Id, original, mutated));
                    }
                }
                else if (op.ArgumentMatch != null && op.Replacement != null)
                {
                    // bc-specific: replace the argument value
                    // e.g. Modify(true) -> Modify(false)
                    var nodeText = node.ToString();
                    if (nodeText.Contains("(" + op.ArgumentMatch + ")", StringComparison.OrdinalIgnoreCase)
                        || nodeText.Contains(op.ArgumentMatch, StringComparison.OrdinalIgnoreCase))
                    {
                        var mutated = ReplaceArgumentInLine(original, methodName, op.ArgumentMatch, op.Replacement);
                        if (mutated != original)
                            Candidates.Add(new MutationCandidate(_filePath, line, op.Id, original, mutated));
                    }
                }
                else if (op.IdentifierReplacement != null)
                {
                    // bc-specific: replace the method name itself
                    // e.g. FindSet -> FindFirst
                    var mutated = ReplaceMethodName(original, methodName, op.IdentifierReplacement);
                    if (mutated != original)
                        Candidates.Add(new MutationCandidate(_filePath, line, op.Id, original, mutated));
                }
            }

            base.VisitInvocationExpression(node);
        }

        private static string? GetMethodName(CodeExpressionSyntax expr)
        {
            if (expr.Kind == SyntaxKind.MemberAccessExpression)
            {
                var ma = (MemberAccessExpressionSyntax)expr;
                return ma.Name.Identifier.Text;
            }
            if (expr.Kind == SyntaxKind.IdentifierName)
            {
                var id = (IdentifierNameSyntax)expr;
                return id.Identifier.Text;
            }
            return null;
        }

        private static string CommentOutLine(string line)
        {
            // Preserve leading whitespace, prepend //
            var trimmed = line.TrimStart();
            var indent = line[..^trimmed.Length];
            return indent + "// " + trimmed;
        }

        private static string ReplaceArgumentInLine(string line, string methodName, string oldArg, string newArg)
        {
            // Find methodName( ... oldArg ... ) and replace oldArg
            // We search for the pattern "(oldArg)" as the whole argument list is simple
            var pattern = "(" + oldArg + ")";
            var replacement = "(" + newArg + ")";
            var idx = line.IndexOf(pattern, StringComparison.OrdinalIgnoreCase);
            if (idx >= 0)
                return line[..idx] + replacement + line[(idx + pattern.Length)..];
            return line;
        }

        private static string ReplaceMethodName(string line, string oldName, string newName)
        {
            // Replace the last occurrence of oldName followed by ( to avoid partial matches
            var idx = line.LastIndexOf(oldName, StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return line;
            // Verify it's a word boundary
            bool leftOk = idx == 0 || !char.IsLetterOrDigit(line[idx - 1]) && line[idx - 1] != '_';
            bool rightOk = idx + oldName.Length < line.Length
                           && !char.IsLetterOrDigit(line[idx + oldName.Length]) && line[idx + oldName.Length] != '_';
            if (leftOk && rightOk)
                return line[..idx] + newName + line[(idx + oldName.Length)..];
            return line;
        }
    }
}

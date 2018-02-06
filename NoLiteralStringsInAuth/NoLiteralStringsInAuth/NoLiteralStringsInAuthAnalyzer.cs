using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace NoLiteralStringsInAuth
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class NoLiteralStringsInAuthAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "NoLiteralStringsInAuth";
        private const string Category = "Authentication";
        private const string Title = "Arguments passed in should not be literal strings";
        private const string MessageFormat = "The arguments should not be literal strings";
        private const string Description = "Using literal strings as arguments can lead to hard to find errors. It is preferable to use a value stored in a single location across the application";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, true, Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context) =>
            context.RegisterCompilationStartAction(AnalyzeCompilation);

        private static void AnalyzeCompilation(CompilationStartAnalysisContext compilationContext)
        {
            compilationContext.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
        }

        private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
        {
            if (!(context.Node is InvocationExpressionSyntax methodInvokeSyntax))
            {
                return;
            }
            var childNodes = methodInvokeSyntax.ChildNodes();
            var methodCaller = childNodes.OfType<MemberAccessExpressionSyntax>().FirstOrDefault();
            if (methodCaller == null) return;
            var argumentsCount = CountArguments(childNodes, SyntaxKind.StringLiteralExpression);
            if (argumentsCount == 0) return;
            context.ReportDiagnostic(Diagnostic.Create(Rule, methodCaller.GetLocation()));
        }

        private static int CountArguments(IEnumerable<SyntaxNode> childNodes, SyntaxKind? kind = null) =>
            childNodes.OfType<ArgumentListSyntax>().Where(s => s.Arguments.Any(a => a.Expression.Kind() == kind)).Select(s => s.Arguments.Count).FirstOrDefault();
    }
}

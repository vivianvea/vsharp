﻿//------------------------------------------------------------------------------
// PHP Sharp. Because PHP isn't good enough.
// Copyright (C) 2019  Niklas Gransjøen
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/>.
//------------------------------------------------------------------------------

using PHPSharp.Symbols;
using PHPSharp.Text;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;

namespace PHPSharp.Syntax
{
    internal class Parser
    {
        private readonly ImmutableArray<SyntaxToken> _tokens;
        private int _position;

        public Parser(SourceText text)
        {
            List<SyntaxToken> tokens = new List<SyntaxToken>();

            Lexer lexer = new Lexer(text);
            SyntaxToken token;
            do
            {
                token = lexer.Lex();

                if (token.Kind != SyntaxKind.BadToken && token.Kind != SyntaxKind.WhitespaceToken)
                    tokens.Add(token);
            } while (token.Kind != SyntaxKind.EndOfFileToken);

            _tokens = tokens.ToImmutableArray();
            Diagnostics.AddRange(lexer.Diagnostics);
        }

        #region Properties

        public DiagnosticBag Diagnostics { get; } = new DiagnosticBag();

        #endregion Properties

        #region Private properties

        private SyntaxToken Current => Peek(0);
        private SyntaxToken LookAhead => Peek(1);

        #endregion Private properties

        #region Methods

        public CompilationUnitSyntax ParseCompilationUnit()
        {
            if (Current.Kind == SyntaxKind.LineCommentToken)
                NextToken();

            StatementSyntax statement = ParseStatement();
            SyntaxToken endOfFileToken = MatchToken(SyntaxKind.EndOfFileToken);

            return new CompilationUnitSyntax(statement, endOfFileToken);
        }

        #endregion Methods

        #region ParseStatement

        private StatementSyntax ParseStatement(bool requireSemicolon = true)
        {
            if (SyntaxFacts.GetKindIsTypeKeyword(Current.Kind))
                return ParseVariableDeclarationStatement(requireSemicolon);

            return Current.Kind switch
            {
                SyntaxKind.OpenBraceToken => ParseBlockStatement(),
                SyntaxKind.IfKeyword => ParseIfStatement(),
                SyntaxKind.WhileKeyword => ParseWhileStatement(),
                SyntaxKind.ForKeyword => ParseForStatement(),

                _ => ParseExpressionStatement(requireSemicolon),
            };
        }

        private BlockStatementSyntax ParseBlockStatement()
        {
            ImmutableArray<StatementSyntax>.Builder statements = ImmutableArray.CreateBuilder<StatementSyntax>();

            SyntaxToken openBraceToken = MatchToken(SyntaxKind.OpenBraceToken);

            while (Current.Kind != SyntaxKind.CloseBraceToken &&
                   Current.Kind != SyntaxKind.EndOfFileToken)
            {
                SyntaxToken currentToken = Current;

                StatementSyntax statement = ParseStatement();
                statements.Add(statement);

                // If no tokens were consumed by the parse call,
                // we should escape the loop. Parse errors will
                // have already been reported.
                if (currentToken == Current)
                    break;
            }

            SyntaxToken closeBraceToken = MatchToken(SyntaxKind.CloseBraceToken);

            return new BlockStatementSyntax(openBraceToken, statements.ToImmutable(), closeBraceToken);
        }

        private VariableDeclarationStatementSyntax ParseVariableDeclarationStatement(bool requireSemicolon)
        {
            SyntaxToken keyword = NextToken();
            SyntaxToken identifier = MatchToken(SyntaxKind.IdentifierToken);
            SyntaxToken equals = MatchToken(SyntaxKind.EqualsToken);
            ExpressionSyntax initializer = ParseExpression();

            SyntaxToken? semicolonToken = null;
            if (requireSemicolon)
                semicolonToken = MatchToken(SyntaxKind.SemicolonToken);

            return new VariableDeclarationStatementSyntax(keyword, identifier, equals, initializer, semicolonToken);
        }

        private IfStatementSyntax ParseIfStatement()
        {
            SyntaxToken keyword = MatchToken(SyntaxKind.IfKeyword);
            ParenthesizedExpressionSyntax condition = ParseParenthesizedExpression();
            StatementSyntax statement = ParseStatement();

            if (statement.Kind == SyntaxKind.VariableDeclarationStatement)
                Diagnostics.ReportCannotDeclareConditional(statement.Span);

            ElseClauseSyntax? elseClause = ParseElseClause();

            return new IfStatementSyntax(keyword, condition, statement, elseClause);
        }

        private ElseClauseSyntax? ParseElseClause()
        {
            if (Current.Kind != SyntaxKind.ElseKeyword)
                return null;

            SyntaxToken keyword = NextToken();
            StatementSyntax statement = ParseStatement();

            if (statement.Kind == SyntaxKind.VariableDeclarationStatement)
                Diagnostics.ReportCannotDeclareConditional(statement.Span);

            return new ElseClauseSyntax(keyword, statement);
        }

        private WhileStatementSyntax ParseWhileStatement()
        {
            SyntaxToken keyword = MatchToken(SyntaxKind.WhileKeyword);
            ParenthesizedExpressionSyntax condition = ParseParenthesizedExpression();
            StatementSyntax statement = ParseStatement();

            if (statement.Kind == SyntaxKind.VariableDeclarationStatement)
                Diagnostics.ReportCannotDeclareConditional(statement.Span);

            return new WhileStatementSyntax(keyword, condition, statement);
        }

        private ForStatementSyntax ParseForStatement()
        {
            SyntaxToken keyword = MatchToken(SyntaxKind.ForKeyword);

            SyntaxToken leftParenthesis = MatchToken(SyntaxKind.OpenParenthesisToken);

            StatementSyntax initStatement = ParseStatement(requireSemicolon: false);
            SyntaxToken initSemicolon = MatchToken(SyntaxKind.SemicolonToken);
            if (initStatement.Kind != SyntaxKind.VariableDeclarationStatement &&
                (initStatement.Kind != SyntaxKind.ExpressionStatement || ((ExpressionStatementSyntax)initStatement).Expression.Kind != SyntaxKind.AssignmentExpression))
            {
                Diagnostics.ReportDeclarationOrAssignmentOnly(initStatement.Span, initStatement.Kind);
            }

            ExpressionSyntax condition = ParseExpression();
            SyntaxToken conditionSemicolon = MatchToken(SyntaxKind.SemicolonToken);

            StatementSyntax updateStatement = ParseStatement(requireSemicolon: false);

            SyntaxToken rightParenthesis = MatchToken(SyntaxKind.CloseParenthesisToken);

            StatementSyntax statement = ParseStatement();
            if (statement.Kind == SyntaxKind.VariableDeclarationStatement)
                Diagnostics.ReportCannotDeclareConditional(statement.Span);

            return new ForStatementSyntax(
                keyword,
                leftParenthesis,
                initStatement, initSemicolon,
                condition, conditionSemicolon,
                updateStatement,
                rightParenthesis,
                statement);
        }

        private ExpressionStatementSyntax ParseExpressionStatement(bool requireSemicolon)
        {
            ExpressionSyntax expression = ParseExpression();

            SyntaxToken? semicolonToken = null;
            if (requireSemicolon)
                semicolonToken = MatchToken(SyntaxKind.SemicolonToken);

            return new ExpressionStatementSyntax(expression, semicolonToken);
        }

        #endregion ParseStatement

        #region ParseExpression

        private ExpressionSyntax ParseExpression(int parentPrecedence = 0)
        {
            ExpressionSyntax left;
            if (Current.Kind == SyntaxKind.IdentifierToken && SyntaxFacts.GetKindIsAssignmentOperator(LookAhead.Kind))
            {
                left = ParseAssignmentExpression();
            }
            else if (CanParseSuffixUnaryExpression())
            {
                left = ParsePostIncrementOrDecrement();
            }
            else if (CanParsePrefixUnaryExpression(parentPrecedence, out int unaryOperatorPrecedence))
            {
                left = ParsePreUnaryExpression(unaryOperatorPrecedence);
            }
            else
            {
                left = ParsePrimaryExpression();
            }

            while (true)
            {
                int precedence = Current.Kind.GetBinaryOperatorPrecedence();
                if (precedence != 0 && precedence > parentPrecedence)
                {
                    SyntaxToken operatorToken = NextToken();
                    ExpressionSyntax right = ParseExpression(precedence);
                    left = new BinaryExpressionSyntax(left, operatorToken, right);
                }
                else break;
            }

            return left;
        }

        private bool CanParseSuffixUnaryExpression()
        {
            if (Current.Kind != SyntaxKind.IdentifierToken)
                return false;

            if (!SyntaxFacts.GetKindIsUnaryOperator(LookAhead.Kind))
                return false;

            return LookAhead.Kind == SyntaxKind.PlusPlusToken || LookAhead.Kind == SyntaxKind.MinusMinusToken;
        }

        private bool CanParsePrefixUnaryExpression(int parentPrecedence, out int unaryOperatorPrecedence)
        {
            unaryOperatorPrecedence = Current.Kind.GetUnaryOperatorPrecedence();
            return unaryOperatorPrecedence != 0 && unaryOperatorPrecedence > parentPrecedence;
        }

        private AssignmentExpressionSyntax ParseAssignmentExpression()
        {
            SyntaxToken identifierToken = MatchToken(SyntaxKind.IdentifierToken);
            SyntaxToken operatorToken = NextToken();
            ExpressionSyntax right = ParseExpression();

            return new AssignmentExpressionSyntax(identifierToken, operatorToken, right);
        }

        private UnaryExpressionSyntax ParsePostIncrementOrDecrement()
        {
            NameExpressionSyntax identifierToken = ParseNameExpression();
            SyntaxToken operatorToken = NextToken();

            return new UnaryExpressionSyntax(operatorToken, identifierToken, UnaryType.Post);
        }

        private UnaryExpressionSyntax ParsePreUnaryExpression(int unaryOperatorPrecedence)
        {
            SyntaxToken operatorToken = NextToken();

            ExpressionSyntax operand;
            if (operatorToken.Kind == SyntaxKind.MinusMinusToken || operatorToken.Kind == SyntaxKind.PlusPlusToken)
                operand = ParseNameExpression();
            else
                operand = ParseExpression(unaryOperatorPrecedence);

            return new UnaryExpressionSyntax(operatorToken, operand, UnaryType.Pre);
        }

        private ExpressionSyntax ParsePrimaryExpression()
        {
            return Current.Kind switch
            {
                SyntaxKind.OpenParenthesisToken => ParseParenthesizedExpression(),

                SyntaxKind.TypeofKeyword => ParseTypeofExpression(),
                SyntaxKind.NameofKeyword => ParseNameofExpression(),

                SyntaxKind.TrueKeyword => ParseBooleanLiteral(isTrue: true),
                SyntaxKind.FalseKeyword => ParseBooleanLiteral(isTrue: false),

                SyntaxKind.IntegerToken => ParseIntegerLiteral(),
                SyntaxKind.FloatToken => ParseFloatLiteral(),
                SyntaxKind.StringToken => ParseStringLiteral(),

                _ => ParseNameOrCallExpression(),
            };
        }

        private ParenthesizedExpressionSyntax ParseParenthesizedExpression()
        {
            SyntaxToken leftParenthesis = MatchToken(SyntaxKind.OpenParenthesisToken);
            ExpressionSyntax expression = ParseExpression();
            SyntaxToken rightParenthesis = MatchToken(SyntaxKind.CloseParenthesisToken);

            return new ParenthesizedExpressionSyntax(leftParenthesis, expression, rightParenthesis);
        }

        private TypeofExpressionSyntax ParseTypeofExpression()
        {
            SyntaxToken typeofKeyword = MatchToken(SyntaxKind.TypeofKeyword);
            SyntaxToken leftParenthesis = MatchToken(SyntaxKind.OpenParenthesisToken);
            LiteralExpressionSyntax typeLiteral = ParseTypeLiteral();
            SyntaxToken rightParenthesis = MatchToken(SyntaxKind.CloseParenthesisToken);

            return new TypeofExpressionSyntax(typeofKeyword, leftParenthesis, typeLiteral, rightParenthesis);
        }

        private NameofExpressionSyntax ParseNameofExpression()
        {
            SyntaxToken nameofKeyword = MatchToken(SyntaxKind.NameofKeyword);
            SyntaxToken leftParenthesis = MatchToken(SyntaxKind.OpenParenthesisToken);
            SyntaxToken identifierToken = MatchToken(SyntaxKind.IdentifierToken);
            SyntaxToken rightParenthesis = MatchToken(SyntaxKind.CloseParenthesisToken);

            return new NameofExpressionSyntax(nameofKeyword, leftParenthesis, identifierToken, rightParenthesis);
        }

        private LiteralExpressionSyntax ParseTypeLiteral()
        {
            if (!SyntaxFacts.GetKindIsTypeKeyword(Current.Kind))
            {
                Diagnostics.ReportTypeExpected(Current.Span, Current.Kind);
            }
            else if (Current.Kind == SyntaxKind.VarKeyword)
            {
                Diagnostics.ReportUnexpectedVarKeyword(Current.Span);
            }

            SyntaxToken typeToken = NextToken();
            return new LiteralExpressionSyntax(typeToken, typeToken.Text);
        }

        private LiteralExpressionSyntax ParseBooleanLiteral(bool isTrue)
        {
            SyntaxToken keywordToken = MatchToken(isTrue ? SyntaxKind.TrueKeyword : SyntaxKind.FalseKeyword);

            return new LiteralExpressionSyntax(keywordToken, isTrue);
        }

        private LiteralExpressionSyntax ParseIntegerLiteral()
        {
            SyntaxToken integerToken = MatchToken(SyntaxKind.IntegerToken);
            if (!int.TryParse(integerToken.Text, out int value))
                Diagnostics.ReportInvalidValue(integerToken.Span, integerToken.Text, TypeSymbol.Int);

            return new LiteralExpressionSyntax(integerToken, value);
        }

        private LiteralExpressionSyntax ParseFloatLiteral()
        {
            SyntaxToken floatToken = MatchToken(SyntaxKind.FloatToken);

            // Remove eventual 'f' character.
            string? floatString = floatToken.Text?.Replace("f", "", ignoreCase: false, CultureInfo.InvariantCulture);
            if (!double.TryParse(floatString, out double value))
                Diagnostics.ReportInvalidValue(floatToken.Span, floatToken.Text, TypeSymbol.Float);

            return new LiteralExpressionSyntax(floatToken, value);
        }

        private LiteralExpressionSyntax ParseStringLiteral()
        {
            SyntaxToken stringToken = MatchToken(SyntaxKind.StringToken);
            return new LiteralExpressionSyntax(stringToken);
        }

        private ExpressionSyntax ParseNameOrCallExpression()
        {
            if (LookAhead.Kind == SyntaxKind.OpenParenthesisToken)
                return ParseCallExpression();
            else
                return ParseNameExpression();
        }

        private CallExpressionSyntax ParseCallExpression()
        {
            SyntaxToken identifierToken = MatchToken(SyntaxKind.IdentifierToken);
            SyntaxToken leftParenthesis = MatchToken(SyntaxKind.OpenParenthesisToken);
            SeparatedSyntaxCollection<ExpressionSyntax> arguments = ParseArguments();
            SyntaxToken rightParenthesis = MatchToken(SyntaxKind.CloseParenthesisToken);

            return new CallExpressionSyntax(identifierToken, leftParenthesis, arguments, rightParenthesis);
        }

        private SeparatedSyntaxCollection<ExpressionSyntax> ParseArguments()
        {
            ImmutableArray<SyntaxNode>.Builder builder = ImmutableArray.CreateBuilder<SyntaxNode>();

            while (Current.Kind != SyntaxKind.CloseParenthesisToken &&
                   Current.Kind != SyntaxKind.EndOfFileToken)
            {
                ExpressionSyntax expression = ParseExpression();
                builder.Add(expression);

                // Don't expect comma after final argument.
                if (Current.Kind != SyntaxKind.CloseParenthesisToken &&
                    Current.Kind != SyntaxKind.EndOfFileToken)
                {
                    var comma = MatchToken(SyntaxKind.CommaToken);
                    builder.Add(comma);
                }
            }

            return new SeparatedSyntaxCollection<ExpressionSyntax>(builder.ToImmutable());
        }

        private NameExpressionSyntax ParseNameExpression()
        {
            SyntaxToken identifierToken = MatchToken(SyntaxKind.IdentifierToken);
            return new NameExpressionSyntax(identifierToken);
        }

        #endregion ParseExpression

        #region Helper methods

        private SyntaxToken Peek(int offset)
        {
            int index = _position + offset;
            if (index >= _tokens.Length)
                return _tokens[_tokens.Length - 1];

            return _tokens[index];
        }

        private SyntaxToken NextToken()
        {
            SyntaxToken current = Current;

            do
            {
                _position++;
            }
            while (Current.Kind == SyntaxKind.LineCommentToken);

            return current;
        }

        private SyntaxToken MatchToken(SyntaxKind kind)
        {
            if (Current.Kind == kind)
                return NextToken();

            Diagnostics.ReportUnexpectedToken(Current.Span, Current.Kind, kind);
            return new SyntaxToken(kind, Current.Position, null, null);
        }

        #endregion Helper methods
    }
}
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

using System;
using System.Collections.Immutable;

namespace PHPSharp.Binding
{
    internal abstract class BoundTreeRewriter
    {
        public virtual BoundStatement RewriteStatement(BoundStatement node)
        {
            return node.Kind switch
            {
                BoundNodeKind.BlockStatement => RewriteBlockStatement((BoundBlockStatement)node),
                BoundNodeKind.VariableDeclarationStatement => RewriteVariableDeclarationStatement((BoundVariableDeclarationStatement)node),
                BoundNodeKind.IfStatement => RewriteIfStatement((BoundIfStatement)node),
                BoundNodeKind.WhileStatement => RewriteWhileStatement((BoundWhileStatement)node),
                BoundNodeKind.ForStatement => RewriteForStatement((BoundForStatement)node),
                BoundNodeKind.ExpressionStatement => RewriteExpressionStatement((BoundExpressionStatement)node),

                _ => throw new Exception($"Unexpected node: '{node.Kind}'."),
            };
        }

        protected virtual BoundStatement RewriteBlockStatement(BoundBlockStatement node)
        {
            ImmutableArray<BoundStatement>.Builder? builder = null;
            for (int i = 0; i < node.Statements.Length; i++)
            {
                BoundStatement oldStatement = node.Statements[i];
                BoundStatement newStatement = RewriteStatement(oldStatement);

                if (builder is null && newStatement != oldStatement)
                {
                    // There's at least one different element, so we initialize the builder and copy all ignored lines over.
                    builder = ImmutableArray.CreateBuilder<BoundStatement>(node.Statements.Length);
                    for (int j = 0; j < i; j++)
                        builder.Add(node.Statements[j]);
                }

                if (builder != null)
                    builder.Add(newStatement);
            }

            if (builder is null)
                return node;

            return new BoundBlockStatement(builder.MoveToImmutable());
        }

        protected virtual BoundStatement RewriteVariableDeclarationStatement(BoundVariableDeclarationStatement node)
        {
            BoundExpression initializer = RewriteExpression(node.Initializer);
            if (initializer == node.Initializer)
                return node;

            return new BoundVariableDeclarationStatement(node.Variable, initializer);
        }

        protected virtual BoundStatement RewriteIfStatement(BoundIfStatement node)
        {
            BoundExpression condition = RewriteExpression(node.Condition);
            BoundStatement thenStatement = RewriteStatement(node.ThenStatement);
            BoundStatement? elseStatement = (node.ElseStatement == null) ? null : RewriteStatement(node.ElseStatement);

            if (condition == node.Condition &&
                thenStatement == node.ThenStatement &&
                elseStatement == node.ElseStatement)
            {
                return node;
            }

            return new BoundIfStatement(condition, thenStatement, elseStatement);
        }

        protected virtual BoundStatement RewriteWhileStatement(BoundWhileStatement node)
        {
            BoundExpression condition = RewriteExpression(node.Condition);
            BoundStatement body = RewriteStatement(node.Body);

            if (condition == node.Condition && body == node.Body)
                return node;

            return new BoundWhileStatement(condition, body);
        }

        protected virtual BoundStatement RewriteForStatement(BoundForStatement node)
        {
            BoundStatement initializationStatement = RewriteStatement(node.InitializationStatement);
            BoundExpression condition = RewriteExpression(node.Condition);
            BoundStatement body = RewriteStatement(node.Body);
            BoundStatement updateStatement = RewriteStatement(node.UpdateStatement);

            if (initializationStatement == node.InitializationStatement &&
                condition == node.Condition &&
                body == node.Body &&
                updateStatement == node.UpdateStatement)
            {
                return node;
            }

            return new BoundForStatement(initializationStatement, condition, body, updateStatement);
        }

        protected virtual BoundStatement RewriteExpressionStatement(BoundExpressionStatement node)
        {
            BoundExpression expression = RewriteExpression(node.Expression);
            if (expression == node.Expression)
                return node;

            return new BoundExpressionStatement(expression);
        }

        public virtual BoundExpression RewriteExpression(BoundExpression node)
        {
            return node.Kind switch
            {
                BoundNodeKind.LiteralExpression => RewriteLiteralExpression((BoundLiteralExpression)node),
                BoundNodeKind.VariableExpression => RewriteVariableExpression((BoundVariableExpression)node),
                BoundNodeKind.AssignmentExpression => RewriteAssignmentExpression((BoundAssignmentExpression)node),
                BoundNodeKind.UnaryExpression => RewriteUnaryExpression((BoundUnaryExpression)node),
                BoundNodeKind.BinaryExpression => RewriteBinaryExpression((BoundBinaryExpression)node),

                _ => throw new Exception($"Unexpected node: '{node.Kind}'."),
            };
        }

        protected virtual BoundExpression RewriteLiteralExpression(BoundLiteralExpression node)
        {
            return node;
        }

        protected virtual BoundExpression RewriteVariableExpression(BoundVariableExpression node)
        {
            return node;
        }

        protected virtual BoundExpression RewriteAssignmentExpression(BoundAssignmentExpression node)
        {
            BoundExpression expression = RewriteExpression(node.Expression);
            if (expression == node.Expression)
                return node;

            return new BoundAssignmentExpression(node.Variable, expression);
        }

        protected virtual BoundExpression RewriteUnaryExpression(BoundUnaryExpression node)
        {
            BoundExpression operand = RewriteExpression(node.Operand);
            if (operand == node.Operand)
                return node;

            return new BoundUnaryExpression(node.Op, operand);
        }

        protected virtual BoundExpression RewriteBinaryExpression(BoundBinaryExpression node)
        {
            BoundExpression left = RewriteExpression(node.Left);
            BoundExpression right = RewriteExpression(node.Right);

            if (left == node.Left && right == node.Right)
                return node;

            return new BoundBinaryExpression(left, node.Op, right);
        }
    }
}
﻿//------------------------------------------------------------------------------
// VSharp - Viv's C#-esque sandbox.
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

using VSharp.Symbols;
using System;
using System.Collections.Generic;
using System.Linq;

namespace VSharp.Binding
{
    internal class BoundLiteralExpression : BoundExpression
    {
        public BoundLiteralExpression(object value)
        {
            Value = value;

            Type = value switch
            {
                bool _ => TypeSymbol.Boolean,
                int _ => TypeSymbol.Int,
                string _ => TypeSymbol.String,
                double _ => TypeSymbol.Float,

                _ => throw new Exception($"Literal '{value}' of type '{value.GetType()}' is illegal."),
            };
        }

        #region Properties

        public override BoundNodeKind Kind => BoundNodeKind.LiteralExpression;
        public override TypeSymbol Type { get; }

        public object Value { get; }

        #endregion Properties

        public override IEnumerable<BoundNode> GetChildren()
        {
            return Enumerable.Empty<BoundNode>();
        }
    }
}
﻿//------------------------------------------------------------------------------
// VSharp - Viv's C#-esque sandbox.
// Copyright (C) 2019  Vivian Vea
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

namespace VSharp.Binding
{
    internal enum BoundUnaryOperatorKind
    {
        Identity,
        Negation,

        PreDecrement,
        PreIncrement,
        PostDecrement,
        PostIncrement,

        LogicalNegation,
        OnesComplement,
    }

    internal static class BoundUnaryOperatorKindExtensions
    {
        public static bool IsIncrementOrDecrement(this BoundUnaryOperatorKind kind) => kind is
            BoundUnaryOperatorKind.PreDecrement or
            BoundUnaryOperatorKind.PreIncrement or
            BoundUnaryOperatorKind.PostDecrement or
            BoundUnaryOperatorKind.PostIncrement;
    }
}
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.Immutable;

namespace SyntaxTree {

    /// <summary>
    /// Modify a type into a function, array, or pointer
    /// </summary>
    public abstract class TypeModifier : PTNode {
        public enum Kind {
            FUNCTION,
            ARRAY,
            POINTER
        }

        public TypeModifier(Kind kind) {
            this.kind = kind;
        }
        public readonly Kind kind;

        public abstract AST.ExprType GetDecoratedType(AST.Env env, AST.ExprType type);

    }

    public class FunctionModifier : TypeModifier {
        public FunctionModifier(List<ParamDecln> param_declns, Boolean has_varargs)
            : base(Kind.FUNCTION) {
            this.param_declns = param_declns;
            this.has_varargs = has_varargs;
        }

        public FunctionModifier(ParameterTypeList _param_type_list)
            : base(Kind.FUNCTION) {
            param_type_list = _param_type_list;
        }
        public ParameterTypeList param_type_list;

        public readonly List<ParamDecln> param_declns;
        public readonly Boolean has_varargs;

        public override AST.ExprType GetDecoratedType(AST.Env env, AST.ExprType ret_t) {
            var args = param_declns.ConvertAll(decln => decln.GetParamDecln(env));
            return AST.TFunction.Create(ret_t, args, has_varargs);
        }

    }

    public class ArrayModifier : TypeModifier {
        public ArrayModifier(Option<Expr> num_elems_opt)
            : base(Kind.ARRAY) {
            this.num_elems_opt = num_elems_opt;
        }

        public override AST.ExprType GetDecoratedType(AST.Env env, AST.ExprType type) {

            if (num_elems_opt.IsNone) {
                return new AST.TIncompleteArray(type);
            }

            AST.Expr num_elems = AST.TypeCast.MakeCast(num_elems_opt.Value.GetExpr(env), new AST.TLong(true, true));

            if (!num_elems.IsConstExpr) {
                throw new InvalidOperationException("Expected constant length.");
            }

            return new AST.TArray(type, ((AST.ConstLong)num_elems).value);
        }


        public readonly Option<Expr> num_elems_opt;
    }

    public class PointerModifier : TypeModifier {
        public PointerModifier(IReadOnlyList<TypeQual> type_quals)
            : base(Kind.POINTER) {
            this.type_quals = type_quals;
        }

        public override AST.ExprType GetDecoratedType(AST.Env env, AST.ExprType type) {
            Boolean is_const = type_quals.Contains(TypeQual.CONST);
            Boolean is_volatile = type_quals.Contains(TypeQual.VOLATILE);

            // This is commented out, for incomplete struct declaration.
            //if (!type.IsComplete) {
            //    throw new InvalidOperationException("The type a pointer points to must be complete.");
            //}

            return new AST.TPointer(type, is_const, is_volatile);
        }

        public readonly IReadOnlyList<TypeQual> type_quals;
    }

    /// <summary>
    /// Has a list of modifiers. Has an optional name.
    /// </summary>
    public abstract class OptionalDeclr : PTNode {
        protected OptionalDeclr(ImmutableList<TypeModifier> typeModifiers) {
            this.TypeModifiers = typeModifiers;
        }

        public abstract Option<String> OptionalName { get; }
        public ImmutableList<TypeModifier> TypeModifiers { get; }
    }

    /// <summary>
    /// Has a list of modifiers. Has no name.
    /// </summary>
    public class AbstractDeclr : OptionalDeclr {
        protected AbstractDeclr(ImmutableList<TypeModifier> typeModifiers)
            : base(typeModifiers) { }

        private static Option<String> noneName { get; } = new None<String>();
        public override Option<String> OptionalName => noneName;
    }

    /// <summary>
    /// Has a name and a list of modifiers.
    /// </summary>
    public class Declr : OptionalDeclr {

        [Obsolete]
        public Declr(String name, IReadOnlyList<TypeModifier> modifiers)
            : this(name, modifiers.ToImmutableList()) { }

        protected Declr(String name, ImmutableList<TypeModifier> typeModifiers)
            : base(typeModifiers) {
            this.Name = name;
            this.OptionalName = Option.Some(name);
        }

        

        public String Name { get; }

        public override Option<String> OptionalName { get; }

        /// <summary>
        /// A declarator consists of 1) a name, and 2) a list of decorators.
        /// This method returns the name, and the modified type.
        /// </summary>
        public virtual Tuple<String, AST.ExprType> GetNameAndType(AST.Env env, AST.ExprType base_type) =>
            Tuple.Create(
                Name,
                TypeModifiers
                    .Reverse()
                    .Aggregate(base_type, (type, modifier) => modifier.GetDecoratedType(env, type))
            );

    }
}
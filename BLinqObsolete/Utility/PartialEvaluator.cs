// PartialEvaluator.cs
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace BLinq.Utility {

    public static class PartialEvaluator {

        private static bool CanBeEvaluatedLocally(Expression expression) {
            return expression.NodeType != ExpressionType.Parameter;
        }

        public static Expression Eval(Expression expression) {
            return Eval(expression, PartialEvaluator.CanBeEvaluatedLocally);
        }

        public static Expression Eval(Expression expression, Func<Expression, bool> fnCanBeEvaluated) {
            return SubtreeEvaluator.Eval(Nominator.Nominate(fnCanBeEvaluated, expression), expression);
        }

        private sealed class SubtreeEvaluator : ExpressionVisitor {

            private HashSet<Expression> candidates;

            private SubtreeEvaluator(HashSet<Expression> candidates) {
                this.candidates = candidates;
            }

            internal static Expression Eval(HashSet<Expression> candidates, Expression exp) {
                return new SubtreeEvaluator(candidates).Visit(exp);
            }

            protected override Expression Visit(Expression exp) {
                if (exp == null) {
                    return null;
                }
                if (this.candidates.Contains(exp)) {
                    return this.Evaluate(exp);
                }
                return base.Visit(exp);
            }

            private Expression Evaluate(Expression e) {
                if (e.NodeType == ExpressionType.Constant) {
                    return e;
                }
                Type type = e.Type;
                if (type.IsValueType) {
                    e = Expression.Convert(e, typeof(object));
                }
                Expression<Func<object>> lambda = Expression.Lambda<Func<object>>(e);
                Func<object> fn = lambda.Compile();
                return Expression.Constant(fn(), type);
            }
        }

        /// <summary>
        /// Performs bottom-up analysis to determine which nodes can possibly
        /// be part of an evaluated sub-tree.
        /// </summary>
        private sealed class Nominator : ExpressionVisitor {
            private Func<Expression, bool> fnCanBeEvaluated;
            private HashSet<Expression> candidates;
            private bool cannotBeEvaluated;

            private Nominator(Func<Expression, bool> fnCanBeEvaluated) {
                this.candidates = new HashSet<Expression>();
                this.fnCanBeEvaluated = fnCanBeEvaluated;
            }

            internal static HashSet<Expression> Nominate(Func<Expression, bool> fnCanBeEvaluated, Expression expression) {
                Nominator nominator = new Nominator(fnCanBeEvaluated);
                nominator.Visit(expression);
                return nominator.candidates;
            }

            protected override Expression VisitConstant(ConstantExpression c) {
                return base.VisitConstant(c);
            }

            protected override Expression Visit(Expression expression) {
                if (expression != null) {
                    bool saveCannotBeEvaluated = this.cannotBeEvaluated;
                    this.cannotBeEvaluated = false;
                    base.Visit(expression);
                    if (!this.cannotBeEvaluated) {
                        if (this.fnCanBeEvaluated(expression)) {
                            this.candidates.Add(expression);
                        }
                        else {
                            this.cannotBeEvaluated = true;
                        }
                    }
                    this.cannotBeEvaluated |= saveCannotBeEvaluated;
                }
                return expression;
            }
        }
    }
}

﻿using CFGToolkit.AST;
using CFGToolkit.AST.Visitors;
using CFGToolkit.AST.Visitors.Cases;
using CFGToolkit.AST.Visitors.Traversals;

namespace CFGToolkit.ExpressionEvaluator
{
    public class Evaluator
    {
        public static object Eval(string expression, Dictionary<string, object> variables, ExpressionParser.ExpressionLanguage language)
        {
            if (language == ExpressionParser.ExpressionLanguage.C)
            {
                var parseTree = ExpressionParser.ExpressionParser.Parse(expression, ExpressionParser.ExpressionLanguage.C);
                var parentSet = new SetParentVisitor();
                var traversal = new PostOrderTreeTraversal<bool>(parentSet);
                traversal.Accept(parseTree, new TreeTraversalContext());

                var finalVariables = new Dictionary<string, object>(variables);
                finalVariables["true"] = true;
                finalVariables["false"] = false;

                var traversal2 = new PostOrderTreeTraversal<object>(new EvalVisitor(finalVariables));
                return traversal2.Accept(parseTree, new TreeTraversalContext());
            }
            throw new InvalidOperationException("Unsupported expression");
        }

        public class EvalVisitor : IVisitor<ISyntaxElement, TreeTraversalContext, object>
        {
            private Dictionary<ISyntaxElement, object> _values = new Dictionary<ISyntaxElement, object>();
            private Dictionary<string, object> _variables;

            public EvalVisitor(Dictionary<string, object> variables)
            {
                _variables = variables;
            }

            public object Visit(ISyntaxElement element, TreeTraversalContext context)
            {
                if (element is SyntaxToken t)
                {
                    if (_variables.ContainsKey(t.Value))
                    {
                        _values[t] = _variables[t.Value];
                        return _values[t];
                    }

                    if (t.Parent.Name == "number")
                    {
                       var value = double.Parse(t.Value.Trim());
                        _values[t] = value;
                        return value;
                    }
                }

                if (element is SyntaxNode node)
                {
                    if (node.Name.Contains("operator"))
                    {
                        _values[node] = 0.0;
                        return 0.0;
                    }

                    if (node.Children.Count == 1)
                    {
                        var val = _values[node.Children.First()];
                        _values[node] = val;
                        return val;
                    }
                    else
                    {
                        if (node.Name == "primary")
                        {
                            if (node.Children.Count == 3) // brackets
                            {
                                var val = _values[node.Children[1]];
                                _values[node] = val;
                                return val;
                            }
                        }

                        if (node.Name == "expression_primary")
                        {
                            if (node.Children.Count == 1)
                            {
                                var val = _values[node.Children.First()];
                                _values[node] = val;
                                return val;
                            }
                            else
                            {
                                var val = _values[node.Children.Last()];

                                if (val is double)
                                {
                                    var op = node.Children.First() as SyntaxNode;
                                    var opToken = op.Children[0] as SyntaxToken;
                                    var doubleVal = (double)val;

                                    switch (opToken.Value)
                                    {
                                        case "+":
                                            break;
                                        case "-":
                                            doubleVal = -doubleVal;
                                            break;
                                        case "--":
                                            doubleVal = --doubleVal;
                                            break;
                                        case "++":
                                            doubleVal = ++doubleVal;
                                            break;
                                        default:
                                            throw new Exception("Unsupported operator");

                                    }

                                    _values[node] = doubleVal;
                                    return val;
                                }

                                if (val is DateTime)
                                {
                                    _values[node] = val;
                                    return val;
                                }
                            }
                        }
                        else if (node.Name.StartsWith("expression_"))
                        {
                            if (!node.Name.EndsWith("_many"))
                            {
                                var left = node.Children[0];
                                var leftValue = _values[left];

                                if (leftValue is double)
                                {
                                    if (node.Children.Count == 2)
                                    {
                                        var right = node.Children[1] as SyntaxNodeMany;

                                        if (right.Repeated.Count != 0)
                                        {
                                            foreach (SyntaxNode item in right.Repeated)
                                            {
                                                var op = item.Children[0] as SyntaxNode;
                                                var opToken = op.Children[0] as SyntaxToken;
                                                var opRight = item.Children[1];
                                                var opRightValue = _values[opRight];
                                                var opRightValueDouble = (double)opRightValue;

                                                switch (opToken.Value)
                                                {
                                                    case "+":
                                                        var total = 0.0;
                                                        total += (double)leftValue;
                                                        total += opRightValueDouble;
                                                        _values[node] = total;
                                                        break;
                                                    case "-":
                                                        total = 0.0;
                                                        total += (double)leftValue;
                                                        total -= opRightValueDouble;
                                                        _values[node] = total;
                                                        break;
                                                    case "*":
                                                        total = 0.0;
                                                        total += (double)leftValue;
                                                        total *= opRightValueDouble;
                                                        _values[node] = total;
                                                        break;
                                                    case "/":
                                                        total = 0.0;
                                                        total += (double)leftValue;
                                                        total /= opRightValueDouble;
                                                        _values[node] = total;
                                                        break;
                                                    case "<":
                                                        _values[node] = (double)leftValue < opRightValueDouble;
                                                        break;
                                                    case "<=":
                                                        _values[node] = (double)leftValue <= opRightValueDouble;
                                                        break;
                                                    case ">=":
                                                        _values[node] = (double)leftValue >= opRightValueDouble;
                                                        break;
                                                    case ">":
                                                        _values[node] = (double)leftValue > opRightValueDouble;
                                                        break;
                                                    default:
                                                        throw new Exception("Unsupported operator");
                                                }
                                            }
                                        }
                                        else
                                        {
                                            _values[node] = leftValue;
                                        }
                                    }
                                    else
                                    {
                                        _values[node] = leftValue;
                                    }
                                }

                                if (leftValue is DateTime)
                                {
                                    var current = (DateTime)leftValue;

                                    if (node.Children.Count == 2)
                                    {
                                        var right = node.Children[1] as SyntaxNodeMany;

                                        if (right.Repeated.Count != 0)
                                        {
                                            foreach (SyntaxNode item in right.Repeated)
                                            {
                                                var op = item.Children[0] as SyntaxNode;
                                                var opToken = op.Children[0] as SyntaxToken;
                                                var opRight = item.Children[1];
                                                var opRightValue = _values[opRight];
                                                var opRightValueDateTime = (DateTime)opRightValue;

                                                switch (opToken.Value)
                                                {
                                                    case "<=":
                                                        _values[node] = current <= opRightValueDateTime;
                                                        break;
                                                    case "<":
                                                        _values[node] = current < opRightValueDateTime;
                                                        break;
                                                    case ">=":
                                                        _values[node] = current >= opRightValueDateTime;
                                                        break;
                                                    case ">":
                                                        _values[node] = current > opRightValueDateTime;
                                                        break;
                                                    default:
                                                        throw new Exception("Unsupported operator");
                                                }
                                            }
                                        }
                                        else
                                        {
                                            _values[node] = current;
                                        }
                                    }
                                    else
                                    {
                                        _values[node] = current;
                                    }
                                }

                                if (leftValue is bool)
                                {
                                    if (node.Children.Count == 2)
                                    {
                                        var right = node.Children[1] as SyntaxNodeMany;

                                        if (right.Repeated.Count != 0)
                                        {
                                            foreach (SyntaxNode item in right.Repeated)
                                            {
                                                var op = item.Children[0] as SyntaxNode;
                                                var opToken = op.Children[0] as SyntaxToken;
                                                var opRight = item.Children[1];
                                                var opRightValue = _values[opRight];
                                                var opRightValueBool = (bool)opRightValue;

                                                switch (opToken.Value)
                                                {
                                                    case "&&":
                                                        _values[node] = (bool)leftValue && opRightValueBool;
                                                        break;
                                                    case "||":
                                                        _values[node] = (bool)leftValue || opRightValueBool;
                                                        break;
                                                    default:
                                                        throw new Exception("Unsupported operator");
                                                }
                                            }
                                        }
                                        else
                                        {
                                            _values[node] = (bool)leftValue;
                                        }
                                    }
                                    else
                                    {
                                        _values[node] = (bool)leftValue;
                                    }
                                }
                            }
                        }
                    }
                }
                return 0.0;
            }
        }
    }
}
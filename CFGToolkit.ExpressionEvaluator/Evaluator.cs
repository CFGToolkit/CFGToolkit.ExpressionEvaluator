using CFGToolkit.AST;
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
                var setParentTraversal = new PostOrderTreeTraversal<bool>(new SetParentVisitor());
                setParentTraversal.Accept(parseTree, new TreeTraversalContext());

                var finalVariables = new Dictionary<string, object>(variables);
                finalVariables["true"] = true;
                finalVariables["false"] = false;

                var evalTraversal = new PostOrderTreeTraversal<object>(new EvalVisitor(finalVariables));
                return evalTraversal.Accept(parseTree, new TreeTraversalContext());
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
                        if (int.TryParse(t.Value.Trim(), out int res))
                        {
                            _values[t] = res;
                        }
                        else
                        {
                            var value = double.Parse(t.Value.Trim());
                            _values[t] = value;
                            return value;
                        }
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

                            if (node.Children.Count == 4) // array
                            {
                                var variable = _values[node.Children[0]];
                                var index = _values[node.Children[2]];

                                if (index is not int)
                                {
                                    throw new Exception("Array index is not an integer");
                                }

                                if (!variable.GetType().IsArray)
                                {
                                    throw new Exception("Variable is not an array");
                                }

                                if (variable is int[] intArray)
                                {
                                    _values[node] = intArray[(int)index];
                                }

                                if (variable is bool[] boolArray)
                                {
                                    _values[node] = boolArray[(int)index];
                                }

                                if (variable is double[] doubleArray)
                                {
                                    _values[node] = doubleArray[(int)index];
                                }

                                return variable;
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
                                    HandleExpressionPrimaryDouble(node, val);
                                }

                                if (val is int)
                                {
                                    HandleExpressionPrimaryInt(node, val);
                                }

                                if (val is DateTime)
                                {
                                    HandleExpressionPrimaryDateTime(node, val);
                                }

                                if (val is bool)
                                {
                                    HandleExpressionPrimaryBool(node, val);
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
                                    HandleExpressionDouble(node, leftValue);
                                }

                                if (leftValue is int)
                                {
                                    HandleExpressionInt(node, leftValue);
                                }

                                if (leftValue is DateTime)
                                {
                                    HandleExpressionDateTime(node, leftValue);
                                }

                                if (leftValue is bool)
                                {
                                    HandleExpressionBool(node, leftValue);
                                }
                            }
                        }
                    }
                }
                return 0.0;
            }

            private void HandleExpressionDouble(SyntaxNode node, object leftValue)
            {
                if (node.Children.Count == 2)
                {
                    var right = (SyntaxNodeMany)node.Children[1];

                    if (right.Repeated.Count != 0)
                    {
                        foreach (SyntaxNode item in right.Repeated)
                        {
                            var op = (SyntaxNode)item.Children[0];
                            var opToken = (SyntaxToken)op.Children[0];
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

            private void HandleExpressionBool(SyntaxNode node, object leftValue)
            {
                if (node.Children.Count == 2)
                {
                    var right = (SyntaxNodeMany)node.Children[1];

                    if (right.Repeated.Count != 0)
                    {
                        foreach (SyntaxNode item in right.Repeated)
                        {
                            var op = (SyntaxNode)item.Children[0];
                            var opToken = (SyntaxToken)op.Children[0];
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

            private void HandleExpressionDateTime(SyntaxNode node, object leftValue)
            {
                var current = (DateTime)leftValue;

                if (node.Children.Count == 2)
                {
                    var right = (SyntaxNodeMany)node.Children[1];

                    if (right.Repeated.Count != 0)
                    {
                        foreach (SyntaxNode item in right.Repeated)
                        {
                            var op = (SyntaxNode)item.Children[0];
                            var opToken = (SyntaxToken)op.Children[0];
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

            private void HandleExpressionInt(SyntaxNode node, object leftValue)
            {
                if (node.Children.Count == 2)
                {
                    var right = (SyntaxNodeMany)node.Children[1];

                    if (right.Repeated.Count != 0)
                    {
                        foreach (SyntaxNode item in right.Repeated)
                        {
                            var op = (SyntaxNode)item.Children[0];
                            var opToken = (SyntaxToken)op.Children[0];
                            var opRight = item.Children[1];
                            var opRightValue = _values[opRight];
                            var opRightValueDouble = double.Parse(opRightValue.ToString());

                            switch (opToken.Value)
                            {
                                case "+":
                                    double total = 0;
                                    total += (int)leftValue;
                                    total += opRightValueDouble;
                                    _values[node] = total;
                                    break;
                                case "-":
                                    total = 0;
                                    total += (int)leftValue;
                                    total -= opRightValueDouble;
                                    _values[node] = total;
                                    break;
                                case "*":
                                    total = 0;
                                    total += (int)leftValue;
                                    total *= opRightValueDouble;
                                    _values[node] = total;
                                    break;
                                case "/":
                                    total = 0;
                                    total += (int)leftValue;
                                    total /= opRightValueDouble;
                                    _values[node] = total;
                                    break;
                                case "<":
                                    _values[node] = (int)leftValue < opRightValueDouble;
                                    break;
                                case "<=":
                                    _values[node] = (int)leftValue <= opRightValueDouble;
                                    break;
                                case ">=":
                                    _values[node] = (int)leftValue >= opRightValueDouble;
                                    break;
                                case ">":
                                    _values[node] = (int)leftValue > opRightValueDouble;
                                    break;
                                case "&":
                                    _values[node] = (int)leftValue & (int)opRightValueDouble;
                                    break;
                                case "&&":
                                    _values[node] = (int)leftValue > 0 && (int)opRightValueDouble > 0;
                                    break;
                                default:
                                    throw new Exception("Unsupported operator");
                            }

                            if (_values[node] is double d)
                            {
                                if (int.TryParse(d.ToString(), out var res))
                                {
                                    _values[node] = res;
                                }
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

            private void HandleExpressionPrimaryDateTime(SyntaxNode node, object val)
            {
                _values[node] = val;
            }

            private void HandleExpressionPrimaryInt(SyntaxNode node, object val)
            {
                var op = (SyntaxNode)node.Children.First();
                var opToken = (SyntaxToken)op.Children[0];
                var doubleVal = (int)val;

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
            }

            private void HandleExpressionPrimaryDouble(SyntaxNode node, object val)
            {
                var op = (SyntaxNode)node.Children.First();
                var opToken = (SyntaxToken)op.Children[0];
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
            }

            private void HandleExpressionPrimaryBool(SyntaxNode node, object val)
            {
                var op = (SyntaxNode)node.Children.First();
                var opToken = (SyntaxToken)op.Children[0];
                var doubleVal = (bool)val;

                switch (opToken.Value)
                {
                    case "!":
                        doubleVal = !doubleVal;
                        break;
                    default:
                        throw new Exception("Unsupported operator");

                }

                _values[node] = doubleVal;
            }
        }
    }
}

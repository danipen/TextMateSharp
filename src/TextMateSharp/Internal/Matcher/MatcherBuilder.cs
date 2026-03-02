using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace TextMateSharp.Internal.Matcher
{
    public class MatcherBuilder<T>
    {
        public List<MatcherWithPriority<T>> Results;
        private readonly Tokenizer _tokenizer;
        private readonly IMatchesName<T> _matchesName;
        private string _token;

        public MatcherBuilder(string expression, IMatchesName<T> matchesName)
        {
            if (expression == null) throw new ArgumentNullException(nameof(expression));
            if (matchesName == null) throw new ArgumentNullException(nameof(matchesName));

            this.Results = new List<MatcherWithPriority<T>>();
            this._tokenizer = new Tokenizer(expression);
            this._matchesName = matchesName;

            this._token = _tokenizer.Next();
            while (_token != null)
            {
                int priority = 0;
                if (_token.Length == 2 && _token[1] == ':')
                {
                    switch (_token[0])
                    {
                        case 'R':
                            priority = 1;
                            break;
                        case 'L':
                            priority = -1;
                            break;
                    }
                    _token = _tokenizer.Next();
                }
                Predicate<T> matcher = ParseConjunction();
                if (matcher != null)
                {
                    Results.Add(new MatcherWithPriority<T>(matcher, priority));
                }
                if (!",".Equals(_token))
                {
                    break;
                }
                _token = _tokenizer.Next();
            }
        }

        private Predicate<T> ParseInnerExpression()
        {
            Predicate<T> firstMatcher = ParseConjunction();
            if (firstMatcher == null)
            {
                return null;
            }

            // Fast path: single conjunction, no OR separators.
            if (!"|".Equals(_token) && !",".Equals(_token))
            {
                return firstMatcher;
            }

            List<Predicate<T>> matchers = new List<Predicate<T>>();
            matchers.Add(firstMatcher);
            while (true)
            {
                if ("|".Equals(_token) || ",".Equals(_token))
                {
                    do
                    {
                        _token = _tokenizer.Next();
                    } while ("|".Equals(_token) || ",".Equals(_token)); // ignore subsequent
                    // commas
                }
                else
                {
                    break;
                }

                Predicate<T> matcher = ParseConjunction();
                if (matcher == null)
                {
                    break;
                }

                matchers.Add(matcher);
                if (!"|".Equals(_token) && !",".Equals(_token))
                {
                    break;
                }
            }

            if (matchers.Count == 1)
            {
                return matchers[0];
            }
            // some (or)
            return matcherInput =>
            {
                for (int i = 0; i < matchers.Count; i++)
                {
                    if (matchers[i].Invoke(matcherInput))
                    {
                        return true;
                    }
                }
                return false;
            };
        }

        private Predicate<T> ParseConjunction()
        {
            Predicate<T> firstMatcher = ParseOperand();
            if (firstMatcher == null)
            {
                return null;
            }

            // Fast path: single operand, no AND chain.
            Predicate<T> secondMatcher = ParseOperand();
            if (secondMatcher == null)
            {
                return firstMatcher;
            }

            List<Predicate<T>> matchers = new List<Predicate<T>>();
            matchers.Add(firstMatcher);
            matchers.Add(secondMatcher);

            Predicate<T> matcher = ParseOperand();
            while (matcher != null)
            {
                matchers.Add(matcher);
                matcher = ParseOperand();
            }
            // every (and)
            return matcherInput =>
            {
                for (int i = 0; i < matchers.Count; i++)
                {
                    if (!matchers[i].Invoke(matcherInput))
                    {
                        return false;
                    }
                }
                return true;
            };
        }

        private Predicate<T> ParseOperand()
        {
            if ("-".Equals(_token))
            {
                _token = _tokenizer.Next();
                Predicate<T> expressionToNegate = ParseOperand();
                return matcherInput =>
                {
                    if (expressionToNegate == null)
                    {
                        return false;
                    }
                    return !expressionToNegate.Invoke(matcherInput);
                };
            }
            if ("(".Equals(_token))
            {
                _token = _tokenizer.Next();
                Predicate<T> expressionInParents = ParseInnerExpression();
                if (")".Equals(_token))
                {
                    _token = _tokenizer.Next();
                }
                return expressionInParents;
            }
            if (IsIdentifier(_token))
            {
                List<string> identifiers = new List<string>();
                do
                {
                    identifiers.Add(_token);
                    _token = _tokenizer.Next();
                } while (_token != null && IsIdentifier(_token));
                return matcherInput => this._matchesName.Match(identifiers, matcherInput);
            }
            return null;
        }

        private static bool IsIdentifier(string token)
        {
            if (string.IsNullOrEmpty(token))
                return false;

            /* Aprox. 2-3 times faster than:
             * static final Pattern IDENTIFIER_REGEXP = Pattern.compile("[\\w\\.:]+");
             * IDENTIFIER_REGEXP.matcher(token).matches();
             *
             * Aprox. 10% faster than:
             * token.chars().allMatch(ch -> ... )
             */
            for (int i = 0; i < token.Length; i++)
            {
                char ch = token[i];
                if (ch == '.' || ch == ':' || ch == '_'
                    || ch >= 'a' && ch <= 'z'
                    || ch >= 'A' && ch <= 'Z'
                    || ch >= '0' && ch <= '9')
                    continue;
                return false;
            }
            return true;
        }

        class Tokenizer
        {

            private static Regex REGEXP = new Regex("([LR]:|[\\w\\.:][\\w\\.:\\-]*|[\\,\\|\\-\\(\\)])");
            private readonly string _input;
            Match _currentMatch;

            public Tokenizer(string input)
            {
                _input = input ?? throw new ArgumentNullException(nameof(input));
            }

            public string Next()
            {
                if (_currentMatch == null)
                {
                    _currentMatch = REGEXP.Match(_input);
                }
                else
                {
                    _currentMatch = _currentMatch.NextMatch();
                }

                if (_currentMatch.Success)
                    return _currentMatch.Value;

                return null;
            }
        }
    }
}
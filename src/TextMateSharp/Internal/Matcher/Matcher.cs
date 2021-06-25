using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace TextMateSharp.Internal.Matcher
{
    public class Matcher<T>
    {
        private static Regex IDENTIFIER_REGEXP = new Regex("[\\w\\.:]+");

        public static ICollection<MatcherWithPriority<List<string>>> CreateMatchers(string expression)
        {
            return CreateMatchers(expression, new NameMatcher());
        }

        private static ICollection<MatcherWithPriority<T>> CreateMatchers<T>(string selector, IMatchesName<T> matchesName)
        {
            return new Matcher<T>(selector, matchesName).results;
        }

        private List<MatcherWithPriority<T>> results;
        private Tokenizer tokenizer;
        private IMatchesName<T> matchesName;
        private String token;

        public Matcher(string expression, IMatchesName<T> matchesName)
        {
            this.results = new List<MatcherWithPriority<T>>();
            this.tokenizer = new Tokenizer(expression);
            this.matchesName = matchesName;

            this.token = tokenizer.Next();
            while (token != null)
            {
                int priority = 0;
                if (token.Length == 2 && token[1] == ':')
                {
                    switch (token[0])
                    {
                        case 'R':
                            priority = 1;
                            break;
                        case 'L':
                            priority = -1;
                            break;
                    }
                    token = tokenizer.Next();
                }
                Predicate<T> matcher = ParseConjunction();
                if (matcher != null)
                {
                    results.Add(new MatcherWithPriority<T>(matcher, priority));
                }
                if (!",".Equals(token))
                {
                    break;
                }
                token = tokenizer.Next();
            }
        }

        private Predicate<T> parseInnerExpression()
        {
            List<Predicate<T>> matchers = new List<Predicate<T>>();
            Predicate<T> matcher = ParseConjunction();
            while (matcher != null)
            {
                matchers.Add(matcher);
                if (token.Equals("|") || token.Equals(","))
                {
                    do
                    {
                        token = tokenizer.Next();
                    } while (token.Equals("|") || token.Equals(",")); // ignore subsequent
                                                                      // commas
                }
                else
                {
                    break;
                }
                matcher = ParseConjunction();
            }
            // some (or)
            return matcherInput =>
            {
                foreach (Predicate<T> matcher1 in matchers)
                {
                    if (matcher1.Invoke(matcherInput))
                    {
                        return true;
                    }
                }
                return false;
            };
        }

        private Predicate<T> ParseConjunction()
        {
            List<Predicate<T>> matchers = new List<Predicate<T>>();
            Predicate<T> matcher = ParseOperand();
            while (matcher != null)
            {
                matchers.Add(matcher);
                matcher = ParseOperand();
            }
            // every (and)
            return matcherInput =>
            {
                foreach (Predicate<T> matcher1 in matchers)
                {
                    if (!matcher1.Invoke(matcherInput))
                    {
                        return false;
                    }
                }
                return true;
            };
        }

        private Predicate<T> ParseOperand()
        {
            if ("-".Equals(token))
            {
                token = tokenizer.Next();
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
            if ("(".Equals(token))
            {
                token = tokenizer.Next();
                Predicate<T> expressionInParents = parseInnerExpression();
                if (")".Equals(token))
                {
                    token = tokenizer.Next();
                }
                return expressionInParents;
            }
            if (IsIdentifier(token))
            {
                ICollection<string> identifiers = new List<string>();
                do
                {
                    identifiers.Add(token);
                    token = tokenizer.Next();
                } while (IsIdentifier(token));
                return matcherInput => this.matchesName.Match(identifiers, matcherInput);
            }
            return null;
        }

        private bool IsIdentifier(string token)
        {
            return token != null && IDENTIFIER_REGEXP.Match(token).Success;
        }

        class Tokenizer
        {

            private static Regex REGEXP = new Regex("([LR]:|[\\w\\.:]+|[\\,\\|\\-\\(\\)])");

            Match match;

            public Tokenizer(string input)
            {
                this.match = REGEXP.Match(input);
            }

            public string Next()
            {
                if (match == null)
                    return null;

                match = match.NextMatch();

                if (match != null)
                    return match.Value;

                return null;
            }
        }
    }
}
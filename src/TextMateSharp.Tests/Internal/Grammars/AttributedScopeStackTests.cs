using NUnit.Framework;
using TextMateSharp.Internal.Grammars;

namespace TextMateSharp.Tests.Internal.Grammars
{
    [TestFixture]
    internal class AttributedScopeStackTests
    {
        [Test]
        public void Equals_ShouldMatchEquivalentStacks()
        {
            AttributedScopeStack stack1 = new AttributedScopeStack(new AttributedScopeStack(null, "source.cs", 1), "meta.test", 2);
            AttributedScopeStack stack2 = new AttributedScopeStack(new AttributedScopeStack(null, "source.cs", 1), "meta.test", 2);

            Assert.IsTrue(stack1.Equals(stack2));
            Assert.IsTrue(stack2.Equals(stack1));
        }

        [Test]
        public void Equals_ShouldReturnFalseForDifferentType()
        {
            AttributedScopeStack stack = new AttributedScopeStack(null, "source.cs", 1);

            Assert.IsFalse(stack.Equals(42));
        }

        [Test]
        public void Equals_ShouldReturnFalseForNull()
        {
            AttributedScopeStack stack = new AttributedScopeStack(null, "source.cs", 1);

            Assert.IsFalse(stack.Equals(null));
        }
    }
}

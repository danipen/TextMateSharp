using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Reflection;
using TextMateSharp.Grammars;
using TextMateSharp.Internal.Grammars;
using TextMateSharp.Internal.Grammars.Parser;
using TextMateSharp.Themes;

namespace TextMateSharp.Tests.Internal.Grammars
{
    [TestFixture]
    public class AttributedScopeStackTests
    {
        #region Shared test constants

        private const string AnyScopePath = "any.scope";
        private const int ExistingLanguageId = 7;
        private const int ExistingTokenType = 1;
        private const FontStyle ExistingFontStyle = FontStyle.Bold;
        private const int ExistingForeground = 5;
        private const int ExistingBackground = 6;
        private const int NewLanguageId = 9;
        private const int NewTokenType = 2;

        #endregion

        [Test]
        public void Equals_ShouldMatchEquivalentStacks()
        {
            AttributedScopeStack stack1 = new AttributedScopeStack(new AttributedScopeStack(null, "source.cs", 1), "meta.test", 2);
            AttributedScopeStack stack2 = new AttributedScopeStack(new AttributedScopeStack(null, "source.cs", 1), "meta.test", 2);

            Assert.IsTrue(stack1.Equals(stack2));
            Assert.IsTrue(stack2.Equals(stack1));
        }

        [Test]
        public void Equals_ShouldReturnFalseForDifferentStacks()
        {
            AttributedScopeStack stack1 = new AttributedScopeStack(new AttributedScopeStack(null, "source.cs", 1), "meta.test", 2);
            AttributedScopeStack stack2 = new AttributedScopeStack(new AttributedScopeStack(null, "source.cs", 1), "meta.other", 2);

            Assert.IsFalse(stack1.Equals(stack2));
            Assert.IsFalse(stack2.Equals(stack1));
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

        #region Constructor tests

        [Test]
        public void Constructor_AssignsProperties()
        {
            // arrange
            const string parentScopePath = "parent.scope";
            const int parentTokenAttributes = 123;
            AttributedScopeStack parent = new AttributedScopeStack(null, parentScopePath, parentTokenAttributes);

            const string childScopePath = "child.scope";
            const int childTokenAttributes = 456;

            // act
            AttributedScopeStack stack = new AttributedScopeStack(parent, childScopePath, childTokenAttributes);

            // assert
            Assert.AreSame(parent, stack.Parent);
            Assert.AreEqual(childScopePath, stack.ScopePath);
            Assert.AreEqual(childTokenAttributes, stack.TokenAttributes);
        }

        [Test]
        public void Constructor_AllowsNullScopePath()
        {
            // arrange
            AttributedScopeStack parent = null;
            string scopePath = null;
            const int tokenAttributes = 0;

            // act
            AttributedScopeStack stack = new AttributedScopeStack(parent, scopePath, tokenAttributes);

            // assert
            Assert.IsNull(stack.ScopePath);
            Assert.AreEqual(tokenAttributes, stack.TokenAttributes);
        }

        #endregion Constructor tests

        #region Equals tests

        [Test]
        public void Equals_Null_ReturnsFalse()
        {
            // arrange
            AttributedScopeStack stack = new AttributedScopeStack(null, "source.cs", 1);

            // act
            bool result = stack.Equals(null);

            // assert
            Assert.IsFalse(result);
        }

        [Test]
        public void Equals_DifferentType_ReturnsFalse()
        {
            // arrange
            AttributedScopeStack stack = new AttributedScopeStack(null, "source.cs", 1);
            object other = 42;

            // act
            bool result = stack.Equals(other);

            // assert
            Assert.IsFalse(result);
        }

        [Test]
        public void Equals_StructurallyEquivalentStacks_ReturnTrue()
        {
            // arrange
            AttributedScopeStack stack1 = new AttributedScopeStack(new AttributedScopeStack(null, "source.cs", 1), "meta.test", 2);
            AttributedScopeStack stack2 = new AttributedScopeStack(new AttributedScopeStack(null, "source.cs", 1), "meta.test", 2);

            // act
            bool stack1EqualsStack2 = stack1.Equals(stack2);
            bool stack2EqualsStack1 = stack2.Equals(stack1);

            // assert
            Assert.IsTrue(stack1EqualsStack2);
            Assert.IsTrue(stack2EqualsStack1);
        }

        [Test]
        public void Equals_DifferentStacks_ReturnFalse()
        {
            // arrange
            AttributedScopeStack stack1 = new AttributedScopeStack(new AttributedScopeStack(null, "source.cs", 1), "meta.test", 2);
            AttributedScopeStack stack2 = new AttributedScopeStack(new AttributedScopeStack(null, "source.cs", 1), "meta.other", 2);

            // act
            bool stack1EqualsStack2 = stack1.Equals(stack2);
            bool stack2EqualsStack1 = stack2.Equals(stack1);

            // assert
            Assert.IsFalse(stack1EqualsStack2);
            Assert.IsFalse(stack2EqualsStack1);
        }

        [Test]
        public void Equals_SameReference_ReturnsTrue()
        {
            // arrange
            AttributedScopeStack stack = CreateStack(("a", 1), ("b", 2));

            // act
            bool result = stack.Equals((object)stack);

            // assert
            Assert.IsTrue(result);
        }

        [Test]
        public void Equals_DifferentLengths_ReturnsFalse()
        {
            // arrange
            AttributedScopeStack shorter = CreateStack(("a", 1), ("b", 2));
            AttributedScopeStack longer = CreateStack(("a", 1), ("b", 2), ("c", 3));

            // act
            bool shorterEqualsLonger = shorter.Equals((object)longer);
            bool longerEqualsShorter = longer.Equals((object)shorter);

            // assert
            Assert.IsFalse(shorterEqualsLonger);
            Assert.IsFalse(longerEqualsShorter);
        }

        [Test]
        public void Equals_ScopePathNullInBothStacksAtSamePosition_ReturnsTrue()
        {
            // arrange
            AttributedScopeStack left = CreateStack((null, 1), ("b", 2));
            AttributedScopeStack right = CreateStack((null, 1), ("b", 2));

            // act
            bool result = left.Equals((object)right);

            // assert
            Assert.IsTrue(result);
        }

        [Test]
        public void Equals_NullScopePathInOneStack_ReturnsFalse()
        {
            // arrange
            AttributedScopeStack left = CreateStack((null, 1), ("b", 2));
            AttributedScopeStack right = CreateStack(("a", 1), ("b", 2));

            // act
            bool result = left.Equals((object)right);

            // assert
            Assert.IsFalse(result);
        }

        [Test]
        public void Equals_IsReflexive()
        {
            // arrange
            AttributedScopeStack stack = CreateStack(("a", 1), ("b", 2));

            // act
            bool result = stack.Equals((object)stack);

            // assert
            Assert.IsTrue(result);
        }

        [Test]
        public void Equals_IsSymmetric()
        {
            // arrange
            AttributedScopeStack left = CreateStack(("a", 1), ("b", 2));
            AttributedScopeStack right = CreateStack(("a", 1), ("b", 2));

            // act
            bool leftEqualsRight = left.Equals((object)right);
            bool rightEqualsLeft = right.Equals((object)left);

            // assert
            Assert.IsTrue(leftEqualsRight);
            Assert.IsTrue(rightEqualsLeft);
        }

        [Test]
        public void Equals_EquivalentDeepStacks_ReturnsTrue()
        {
            // arrange
            const int depth = 50;
            AttributedScopeStack left = null;
            AttributedScopeStack right = null;

            for (int i = 0; i < depth; i++)
            {
                left = new AttributedScopeStack(left, "s" + i, i);
                right = new AttributedScopeStack(right, "s" + i, i);
            }

            // act & assert
            Assert.IsTrue(left!.Equals(right));
        }

        [Test]
        public void Equals_EquivalentButDistinctChains_ReturnsTrue()
        {
            // arrange
            AttributedScopeStack left = new AttributedScopeStack(
                new AttributedScopeStack(null, "a", 1),
                "b",
                2);

            AttributedScopeStack right = new AttributedScopeStack(
                new AttributedScopeStack(null, "a", 1),
                "b",
                2);

            // act
            bool result = left.Equals(right);

            // assert
            Assert.IsTrue(result);
        }

        [Test]
        public void Equals_SameLeafButOneStackHasExtraParent_ReturnsFalse()
        {
            // arrange
            const string sharedLeafScope = "b";
            const int sharedLeafTokenAttributes = 2;

            AttributedScopeStack longer = new AttributedScopeStack(
                new AttributedScopeStack(null, "a", 1),
                sharedLeafScope,
                sharedLeafTokenAttributes);

            AttributedScopeStack shorter = new AttributedScopeStack(
                null,
                sharedLeafScope,
                sharedLeafTokenAttributes);

            // act
            bool result = longer.Equals(shorter);

            // assert
            Assert.IsFalse(result);
        }

        [Test]
        public void Equals_SameLeafAndSameParentInstance_ReturnsTrue()
        {
            // arrange
            const string parentScope = "shared.parent";
            const int parentTokenAttributes = 1;

            const string leafScope = "leaf";
            const int leafTokenAttributes = 2;

            AttributedScopeStack sharedParent = new AttributedScopeStack(null, parentScope, parentTokenAttributes);

            AttributedScopeStack left = new AttributedScopeStack(sharedParent, leafScope, leafTokenAttributes);
            AttributedScopeStack right = new AttributedScopeStack(sharedParent, leafScope, leafTokenAttributes);

            // act
            bool result = left.Equals(right);

            // assert
            Assert.IsTrue(result);
        }

        [Test]
        public void Equals_SameLeafButDifferentParentScope_ReturnsFalse()
        {
            // arrange
            const string leafScope = "leaf";
            const int leafTokenAttributes = 2;

            AttributedScopeStack left = new AttributedScopeStack(
                new AttributedScopeStack(null, "parent.a", 1),
                leafScope,
                leafTokenAttributes);

            AttributedScopeStack right = new AttributedScopeStack(
                new AttributedScopeStack(null, "parent.b", 1),
                leafScope,
                leafTokenAttributes);

            // act
            bool result = left.Equals(right);

            // assert
            Assert.IsFalse(result);
        }

        [Test]
        public void Equals_SameLeafButDifferentParentTokenAttributes_ReturnsFalse()
        {
            // arrange
            const string parentScope = "parent";
            const string leafScope = "leaf";
            const int leafTokenAttributes = 2;

            AttributedScopeStack left = new AttributedScopeStack(
                new AttributedScopeStack(null, parentScope, 111),
                leafScope,
                leafTokenAttributes);

            AttributedScopeStack right = new AttributedScopeStack(
                new AttributedScopeStack(null, parentScope, 222),
                leafScope,
                leafTokenAttributes);

            // act
            bool result = left.Equals(right);

            // assert
            Assert.IsFalse(result);
        }

        [Test]
        public void Equals_LeftChainEndsFirst_ReturnsFalse()
        {
            // arrange
            const string leafScopePath = "leaf";
            const int leafTokenAttributes = 2;

            const string parentScopePath = "parent";
            const int parentTokenAttributes = 1;

            AttributedScopeStack shorter = new AttributedScopeStack(null, leafScopePath, leafTokenAttributes);
            AttributedScopeStack longer = new AttributedScopeStack(
                new AttributedScopeStack(null, parentScopePath, parentTokenAttributes),
                leafScopePath,
                leafTokenAttributes);

            // act
            bool result = shorter.Equals(longer);

            // assert
            Assert.IsFalse(result);
        }

        [Test]
        public void Equals_RightChainEndsFirst_ReturnsFalse()
        {
            // arrange
            const string leafScopePath = "leaf";
            const int leafTokenAttributes = 2;

            const string parentScopePath = "parent";
            const int parentTokenAttributes = 1;

            AttributedScopeStack longer = new AttributedScopeStack(
                new AttributedScopeStack(null, parentScopePath, parentTokenAttributes),
                leafScopePath,
                leafTokenAttributes);

            AttributedScopeStack shorter = new AttributedScopeStack(null, leafScopePath, leafTokenAttributes);

            // act
            bool result = longer.Equals(shorter);

            // assert
            Assert.IsFalse(result);
        }

        [Test]
        public void Equals_PrivateStatic_FirstArgumentNull_ReturnsFalse()
        {
            // arrange
            MethodInfo equalsMethod = GetPrivateStaticEqualsMethod();
            AttributedScopeStack b = new AttributedScopeStack(null, "x", 1);

            // act
            object result = equalsMethod.Invoke(null, [null, b]);

            // assert
            Assert.NotNull(result);
            Assert.IsFalse((bool)result);
        }

        [Test]
        public void Equals_PrivateStatic_SecondArgumentNull_ReturnsFalse()
        {
            // arrange
            MethodInfo equalsMethod = GetPrivateStaticEqualsMethod();
            AttributedScopeStack a = new AttributedScopeStack(null, "x", 1);

            // act
            object result = equalsMethod.Invoke(null, [a, null]);

            // assert
            Assert.NotNull(result);
            Assert.IsFalse((bool)result);
        }

        [Test]
        public void Equals_PrivateStatic_BothArgumentsNull_ReturnsTrue()
        {
            // arrange
            MethodInfo equalsMethod = GetPrivateStaticEqualsMethod();

            // act
            object result = equalsMethod.Invoke(null, [null, null]);

            // assert
            Assert.NotNull(result);
            Assert.IsTrue((bool)result);
        }

        #endregion Equals tests

        #region GetHashCode tests

        [Test]
        public void GetHashCode_WhenParentIsNull_DoesNotThrow_AndIsDeterministic()
        {
            // arrange
            AttributedScopeStack root = new AttributedScopeStack(null, "root", 1);

            // act
            int first = root.GetHashCode();
            int second = root.GetHashCode();

            // assert
            Assert.AreEqual(first, second);
        }

        [Test]
        public void GetHashCode_WhenScopePathIsNull_DoesNotThrow_AndIsDeterministic()
        {
            // arrange
            AttributedScopeStack stack = new AttributedScopeStack(null, null, 1);

            // act
            int first = stack.GetHashCode();
            int second = stack.GetHashCode();

            // assert
            Assert.AreEqual(first, second);
        }

        [Test]
        public void GetHashCode_EqualObjects_ReturnSameValue()
        {
            // arrange
            AttributedScopeStack left = CreateStack(("a", 1), ("b", 2));
            AttributedScopeStack right = CreateStack(("a", 1), ("b", 2));

            // act
            int leftHash = left.GetHashCode();
            int rightHash = right.GetHashCode();

            // assert
            Assert.IsTrue(left.Equals(right));
            Assert.AreEqual(leftHash, rightHash);
        }

        [Test]
        public void GetHashCode_WhenStacksAreEqual_ReturnsSameValue()
        {
            // arrange
            AttributedScopeStack left = CreateStack(("a", 1), ("b", 2), ("c", 3));
            AttributedScopeStack right = CreateStack(("a", 1), ("b", 2), ("c", 3));

            // act
            bool isEqual = left.Equals((object)right);
            int leftHashCode = left.GetHashCode();
            int rightHashCode = right.GetHashCode();

            // assert
            Assert.IsTrue(isEqual);
            Assert.AreEqual(leftHashCode, rightHashCode);
        }

        [Test]
        public void GetHashCode_WhenUsedAsDictionaryKey_AllowsLookupUsingEqualStack()
        {
            // arrange
            AttributedScopeStack key1 = CreateStack(("a", 1), ("b", 2));
            AttributedScopeStack key2 = CreateStack(("a", 1), ("b", 2));

            Dictionary<AttributedScopeStack, string> dictionary = new Dictionary<AttributedScopeStack, string>
            {
                [key1] = "VALUE"
            };

            // act
            bool found = dictionary.TryGetValue(key2, out string value);

            // assert
            Assert.IsTrue(found);
            Assert.AreEqual("VALUE", value);
        }
        [Test]
        public void GetHashCode_WhenStackIsDeep_DoesNotThrow_AndIsDeterministic()
        {
            // arrange
            const int depth = 10_000;

            AttributedScopeStack current = null;
            for (int i = 0; i < depth; i++)
            {
                current = new AttributedScopeStack(current, "s" + i, i);
            }

            // act
            int first = current!.GetHashCode();
            int second = current.GetHashCode();

            // assert
            Assert.AreEqual(first, second);
        }

        #endregion GetHashCode tests

        #region IEquatable<AttributedScopeStack> tests

        [Test]
        public void IEquatable_Equals_StructurallyEqualStacks_ReturnsTrue()
        {
            // arrange
            AttributedScopeStack left = CreateStack(("a", 1), ("b", 2));
            AttributedScopeStack right = CreateStack(("a", 1), ("b", 2));

            // act - calls IEquatable<AttributedScopeStack>.Equals directly
            bool result = left.Equals(right);

            // assert
            Assert.IsTrue(result);
        }

        [Test]
        public void IEquatable_Equals_StacksWithDifferentDepths_ReturnsFalse()
        {
            // arrange
            AttributedScopeStack left = CreateStack(("a", 1), ("b", 2));
            AttributedScopeStack right = CreateStack(("a", 1), ("b", 2), ("c", 3));

            // act
            bool result = left.Equals(right);

            // assert
            Assert.IsFalse(result);
        }

        [Test]
        public void IEquatable_Equals_DifferentStacks_ReturnsFalse()
        {
            // arrange
            AttributedScopeStack left = CreateStack(("a", 1), ("b", 2));
            AttributedScopeStack right = CreateStack(("a", 1), ("x", 2));

            // act
            bool result = left.Equals(right);

            // assert
            Assert.IsFalse(result);
        }

        [Test]
        public void IEquatable_Equals_NullArgument_ReturnsFalse()
        {
            // arrange
            AttributedScopeStack stack = CreateStack(("a", 1));

            // act
            bool result = stack.Equals((AttributedScopeStack)null);

            // assert
            Assert.IsFalse(result);
        }

        [Test]
        public void IEquatable_Equals_SameReference_ReturnsTrue()
        {
            // arrange
            AttributedScopeStack stack = CreateStack(("a", 1), ("b", 2));

            // act
            bool result = stack.Equals(stack);

            // assert
            Assert.IsTrue(result);
        }

        [Test]
        public void IEquatable_IsReflexive()
        {
            // arrange
            AttributedScopeStack stack = CreateStack(("a", 1), ("b", 2));

            // act
            bool result = stack.Equals(stack);

            // assert
            Assert.IsTrue(result);
        }

        [Test]
        public void IEquatable_IsSymmetric()
        {
            // arrange
            AttributedScopeStack left = CreateStack(("a", 1));
            AttributedScopeStack right = CreateStack(("a", 1));

            // act
            bool leftEqualsRight = left.Equals(right);
            bool rightEqualsLeft = right.Equals(left);

            // assert
            Assert.IsTrue(leftEqualsRight);
            Assert.IsTrue(rightEqualsLeft);
        }

        [Test]
        public void IEquatable_Equals_UsedByEqualityComparerDefault()
        {
            // arrange
            AttributedScopeStack key1 = CreateStack(("a", 1), ("b", 2));
            AttributedScopeStack key2 = CreateStack(("a", 1), ("b", 2));

            EqualityComparer<AttributedScopeStack> comparer = EqualityComparer<AttributedScopeStack>.Default;

            // act & assert
            Assert.IsTrue(comparer.Equals(key1, key2));
            Assert.AreEqual(comparer.GetHashCode(key1), comparer.GetHashCode(key2));
        }

        #endregion IEquatable<AttributedScopeStack> tests

        #region Operator == and != tests

        [Test]
        public void OperatorEquals_IsReflexive()
        {
            // arrange
            AttributedScopeStack stack = CreateStack(("a", 1));

            // act & assert
#pragma warning disable CS1718
            Assert.IsTrue(stack == stack);
#pragma warning restore CS1718
        }

        [Test]
        public void OperatorEquals_IsSymmetric()
        {
            // arrange
            AttributedScopeStack left = CreateStack(("a", 1));
            AttributedScopeStack right = CreateStack(("a", 1));

            // act & assert
            Assert.IsTrue(left == right);
            Assert.IsTrue(right == left);
        }

        [Test]
        public void OperatorNotEquals_IsSymmetric()
        {
            // arrange
            AttributedScopeStack left = CreateStack(("a", 1));
            AttributedScopeStack right = CreateStack(("a", 1));

            // act & assert
            Assert.IsFalse(left != right);
            Assert.IsFalse(right != left);
        }

        [Test]
        public void OperatorEquals_StructurallyEqualStacks_ReturnsTrue()
        {
            // arrange
            AttributedScopeStack left = CreateStack(("a", 1), ("b", 2));
            AttributedScopeStack right = CreateStack(("a", 1), ("b", 2));

            // act & assert
            Assert.IsTrue(left == right);
            Assert.IsFalse(left != right);
        }

        [Test]
        public void OperatorEquals_DifferentStacks_ReturnsFalse()
        {
            // arrange
            AttributedScopeStack left = CreateStack(("a", 1), ("b", 2));
            AttributedScopeStack right = CreateStack(("a", 1), ("x", 2));

            // act & assert
            Assert.IsFalse(left == right);
            Assert.IsTrue(left != right);
        }

        [Test]
        public void OperatorEquals_BothNull_ReturnsTrue()
        {
            // arrange
            AttributedScopeStack left = null;
            AttributedScopeStack right = null;

            // act & assert
            Assert.IsTrue(left == right);
            Assert.IsFalse(left != right);
        }

        [Test]
        public void OperatorEquals_LeftNull_ReturnsFalse()
        {
            // arrange
            AttributedScopeStack left = null;
            AttributedScopeStack right = CreateStack(("a", 1));

            // act & assert
            Assert.IsFalse(left == right);
            Assert.IsTrue(left != right);
        }

        [Test]
        public void OperatorEquals_RightNull_ReturnsFalse()
        {
            // arrange
            AttributedScopeStack left = CreateStack(("a", 1));
            AttributedScopeStack right = null;

            // act & assert
            Assert.IsFalse(left == right);
            Assert.IsTrue(left != right);
        }

        [Test]
        public void OperatorEquals_SameReference_ReturnsTrue()
        {
            // arrange
            AttributedScopeStack stack = CreateStack(("a", 1), ("b", 2));

            // act & assert - deliberately comparing to itself via ==
#pragma warning disable CS1718 // Comparison made to same variable
            Assert.IsTrue(stack == stack);
            Assert.IsFalse(stack != stack);
#pragma warning restore CS1718
        }

        [Test]
        public void OperatorEquals_SameScopePathsDifferentTokenAttributes_ReturnsFalse()
        {
            // arrange
            AttributedScopeStack left = CreateStack(("a", 1), ("b", 2));
            AttributedScopeStack right = CreateStack(("a", 1), ("b", 99)); // only token attributes differ

            // act & assert
            Assert.IsFalse(left == right);
            Assert.IsTrue(left != right);
        }

        #endregion Operator == and != tests

        #region GetScopeNames tests

        [Test]
        public void GetScopeNames_ReturnsRootToLeaf()
        {
            // arrange
            AttributedScopeStack stack = CreateStack(("a", 1), ("b", 2), ("c", 3));

            // act
            List<string> scopes = stack.GetScopeNames();

            // assert
            Assert.AreEqual(3, scopes.Count);
            Assert.AreEqual("a", scopes[0]);
            Assert.AreEqual("b", scopes[1]);
            Assert.AreEqual("c", scopes[2]);
        }

        [Test]
        public void GetScopeNames_IsCached_ReturnsSameListInstance()
        {
            // arrange
            AttributedScopeStack stack = CreateStack(("a", 1), ("b", 2));

            // act
            List<string> first = stack.GetScopeNames();
            List<string> second = stack.GetScopeNames();

            // assert
            Assert.AreSame(first, second);
        }

        [Test]
        public void GetScopeNames_CacheIsMutable_MutationsPersistAcrossCalls()
        {
            // arrange
            AttributedScopeStack stack = CreateStack(("a", 1), ("b", 2));
            List<string> first = stack.GetScopeNames();

            // act
            first.Add("MUTATION");
            List<string> second = stack.GetScopeNames();

            // assert
            Assert.AreSame(first, second);
            Assert.AreEqual(3, second.Count);
            Assert.AreEqual("MUTATION", second[2]);
        }

        [Test]
        public void GetScopeNames_LongChain_ReturnsCorrectCountAndEndpoints()
        {
            // arrange
            const int count = 2_000;
            AttributedScopeStack current = null;
            for (int i = 0; i < count; i++)
            {
                current = new AttributedScopeStack(current, "s" + i, i);
            }

            // act
            List<string> scopes = current!.GetScopeNames();

            // assert
            Assert.AreEqual(count, scopes.Count);
            Assert.AreEqual("s0", scopes[0]);
            Assert.AreEqual("s" + (count - 1), scopes[count - 1]);
        }

        #endregion GetScopeNames tests

        #region MergeAttributes tests

        [Test]
        public void MergeAttributes_NullBasicScopeAttributes_ReturnsExisting()
        {
            // arrange
            int existing = CreateNonDefaultEncodedMetadata();

            // act
            int result = AttributedScopeStack.MergeAttributes(existing, null, null);

            // assert
            Assert.AreEqual(existing, result);
        }

        [Test]
        public void MergeAttributes_ThemeDataNull_PreservesStyleAndColors_ButUpdatesLanguageAndTokenType()
        {
            // arrange
            int existing = CreateNonDefaultEncodedMetadata();
            AttributedScopeStack scopesList = new AttributedScopeStack(null, AnyScopePath, existing);
            BasicScopeAttributes attrs = new BasicScopeAttributes(NewLanguageId, NewTokenType, null);

            // act
            int result = AttributedScopeStack.MergeAttributes(existing, scopesList, attrs);

            // assert
            Assert.AreEqual(NewLanguageId, EncodedTokenAttributes.GetLanguageId(result));
            Assert.AreEqual(NewTokenType, EncodedTokenAttributes.GetTokenType(result));
            Assert.AreEqual(ExistingFontStyle, EncodedTokenAttributes.GetFontStyle(result));
            Assert.AreEqual(ExistingForeground, EncodedTokenAttributes.GetForeground(result));
            Assert.AreEqual(ExistingBackground, EncodedTokenAttributes.GetBackground(result));
        }

        [Test]
        public void MergeAttributes_ThemeDataEmpty_PreservesStyleAndColors_ButUpdatesLanguageAndTokenType()
        {
            // arrange
            int existing = CreateNonDefaultEncodedMetadata();
            AttributedScopeStack scopesList = new AttributedScopeStack(null, AnyScopePath, existing);

            List<ThemeTrieElementRule> themeData = [];
            BasicScopeAttributes attrs = new BasicScopeAttributes(NewLanguageId, NewTokenType, themeData);

            // act
            int result = AttributedScopeStack.MergeAttributes(existing, scopesList, attrs);

            // assert
            Assert.AreEqual(NewLanguageId, EncodedTokenAttributes.GetLanguageId(result));
            Assert.AreEqual(NewTokenType, EncodedTokenAttributes.GetTokenType(result));
            Assert.AreEqual(ExistingFontStyle, EncodedTokenAttributes.GetFontStyle(result));
            Assert.AreEqual(ExistingForeground, EncodedTokenAttributes.GetForeground(result));
            Assert.AreEqual(ExistingBackground, EncodedTokenAttributes.GetBackground(result));
        }

        [Test]
        public void MergeAttributes_FirstRuleWithNullParentScopes_IsAlwaysSelected()
        {
            // arrange
            int existing = CreateNonDefaultEncodedMetadata();

            AttributedScopeStack scopesList = CreateStack(
                ("source.csharp", existing),
                ("meta.using", existing));

            const int rule1ScopeDepth = 1;
            const int rule1Foreground = 11;
            const int rule1Background = 12;
            const FontStyle rule1FontStyle = FontStyle.Italic;

            ThemeTrieElementRule rule1 = new ThemeTrieElementRule(
                "r1",
                rule1ScopeDepth,
                null,
                rule1FontStyle,
                rule1Foreground,
                rule1Background);

            List<string> rule2ParentScopes = ["nonexistent"];
            ThemeTrieElementRule rule2 = new ThemeTrieElementRule(
                "r2",
                1,
                rule2ParentScopes,
                FontStyle.Underline,
                99,
                98);

            List<ThemeTrieElementRule> themeData = [rule1, rule2];
            BasicScopeAttributes attrs = new BasicScopeAttributes(NewLanguageId, NewTokenType, themeData);

            // act
            int result = AttributedScopeStack.MergeAttributes(existing, scopesList, attrs);

            // assert
            Assert.AreEqual(rule1FontStyle, EncodedTokenAttributes.GetFontStyle(result));
            Assert.AreEqual(rule1Foreground, EncodedTokenAttributes.GetForeground(result));
            Assert.AreEqual(rule1Background, EncodedTokenAttributes.GetBackground(result));
        }

        [Test]
        public void MergeAttributes_FirstRuleDoesNotMatch_SecondRuleMatchesByOrderedParentScopes()
        {
            // arrange
            int existing = CreateNonDefaultEncodedMetadata();

            AttributedScopeStack scopesList = CreateStack(
                ("source.csharp", existing),
                ("meta.using", existing),
                ("keyword.control", existing));

            // rule1 should NOT match
            List<string> rule1ParentScopes = ["source.csharp", "meta.using"];
            ThemeTrieElementRule rule1 = new ThemeTrieElementRule("r1", 1, rule1ParentScopes, FontStyle.Italic, 11, 12);

            const int rule2Foreground = 21;
            const int rule2Background = 22;
            const FontStyle rule2FontStyle = FontStyle.Underline;

            // rule2 SHOULD match
            List<string> rule2ParentScopes = ["meta.using", "source.csharp"];
            ThemeTrieElementRule rule2 = new ThemeTrieElementRule(
                "r2",
                1,
                rule2ParentScopes,
                rule2FontStyle,
                rule2Foreground,
                rule2Background);

            List<ThemeTrieElementRule> themeData = [rule1, rule2];
            BasicScopeAttributes attrs = new BasicScopeAttributes(NewLanguageId, NewTokenType, themeData);

            // act
            int result = AttributedScopeStack.MergeAttributes(existing, scopesList, attrs);

            // assert
            Assert.AreEqual(rule2FontStyle, EncodedTokenAttributes.GetFontStyle(result));
            Assert.AreEqual(rule2Foreground, EncodedTokenAttributes.GetForeground(result));
            Assert.AreEqual(rule2Background, EncodedTokenAttributes.GetBackground(result));
        }

        [Test]
        public void MergeAttributes_FirstMatchingRuleWins_WhenMultipleRulesMatch()
        {
            // Arrange
            int existing = CreateNonDefaultEncodedMetadata();

            AttributedScopeStack scopesList = CreateStack(
                ("source.csharp", existing),
                ("meta.using", existing),
                ("keyword.control", existing));

            ThemeTrieElementRule rule1 = new ThemeTrieElementRule("r1", 1, null, FontStyle.Italic, 11, 12);
            ThemeTrieElementRule rule2 = new ThemeTrieElementRule("r2", 1, null, FontStyle.Underline, 21, 22);

            List<ThemeTrieElementRule> themeData = [rule1, rule2];
            BasicScopeAttributes attrs = new BasicScopeAttributes(9, 2, themeData);

            // Act
            int result = AttributedScopeStack.MergeAttributes(existing, scopesList, attrs);

            // Assert
            Assert.AreEqual(FontStyle.Italic, EncodedTokenAttributes.GetFontStyle(result));
            Assert.AreEqual(11, EncodedTokenAttributes.GetForeground(result));
            Assert.AreEqual(12, EncodedTokenAttributes.GetBackground(result));
        }

        [Test]
        public void MergeAttributes_ParentScopeSelectorPrefix_MatchesDotSeparatedScope()
        {
            // arrange
            int existing = CreateNonDefaultEncodedMetadata();

            AttributedScopeStack scopesList = CreateStack(
                ("source.csharp", existing),
                ("meta.block", existing),
                ("keyword.control", existing));

            const int expectedForeground = 31;
            const int expectedBackground = 32;
            const FontStyle expectedFontStyle = FontStyle.Italic;

            List<string> parentScopes = ["meta"];
            ThemeTrieElementRule rule = new ThemeTrieElementRule(
                "prefix-parent",
                1,
                parentScopes,
                expectedFontStyle,
                expectedForeground,
                expectedBackground);

            List<ThemeTrieElementRule> themeData = [rule];
            BasicScopeAttributes attrs = new BasicScopeAttributes(NewLanguageId, NewTokenType, themeData);

            // act
            int result = AttributedScopeStack.MergeAttributes(existing, scopesList, attrs);

            // assert
            Assert.AreEqual(expectedFontStyle, EncodedTokenAttributes.GetFontStyle(result));
            Assert.AreEqual(expectedForeground, EncodedTokenAttributes.GetForeground(result));
            Assert.AreEqual(expectedBackground, EncodedTokenAttributes.GetBackground(result));
        }

        // WARNING: BREAKING CHANGE. currently this throws ArgumentOutOfRangeException, but I'm changing it to allow
        // empty parent scopes lists and treat them as "always matches" (similar to null parent scopes)
        // in order to be more resilient to malformed theme data. If we want to maintain the old behavior
        // of throwing on empty parent scopes, we should add an explicit check for that and throw
        // before we get to the point of trying to match against the scopes list.
        [Test]
        public void MergeAttributes_EmptyParentScopesList_PreservesExistingStyle()
        {
            // arrange
            int existing = CreateNonDefaultEncodedMetadata();
            AttributedScopeStack scopesList = new AttributedScopeStack(null, AnyScopePath, existing);

            const int expectedForeground = 123;
            const int expectedBackground = 124;

            List<string> emptyParentScopes = [];
            ThemeTrieElementRule rule = new ThemeTrieElementRule(
                "empty-parents",
                1,
                emptyParentScopes,
                ExistingFontStyle,
                expectedForeground,
                expectedBackground);

            List<ThemeTrieElementRule> themeData = [rule];
            BasicScopeAttributes attrs = new BasicScopeAttributes(NewLanguageId, NewTokenType, themeData);

            // act
            int result = AttributedScopeStack.MergeAttributes(existing, scopesList, attrs);

            // assert
            Assert.AreEqual(ExistingFontStyle, EncodedTokenAttributes.GetFontStyle(result));
            Assert.AreEqual(expectedForeground, EncodedTokenAttributes.GetForeground(result));
            Assert.AreEqual(expectedBackground, EncodedTokenAttributes.GetBackground(result));
        }

        [Test]
        public void MergeAttributes_RuleFontStyleNotSet_PreservesExistingFontStyle()
        {
            // arrange
            int existing = CreateNonDefaultEncodedMetadata();
            AttributedScopeStack scopesList = new AttributedScopeStack(null, AnyScopePath, existing);

            const int expectedForeground = 123;
            const int expectedBackground = 124;

            List<string> parentScopes = [AnyScopePath];
            ThemeTrieElementRule rule = new ThemeTrieElementRule(
                "preserve-style",
                1,
                parentScopes,
                FontStyle.NotSet,
                expectedForeground,
                expectedBackground);

            List<ThemeTrieElementRule> themeData = [rule];
            BasicScopeAttributes attrs = new BasicScopeAttributes(NewLanguageId, NewTokenType, themeData);

            // act
            int result = AttributedScopeStack.MergeAttributes(existing, scopesList, attrs);

            // assert
            Assert.AreEqual(ExistingFontStyle, EncodedTokenAttributes.GetFontStyle(result));
            Assert.AreEqual(expectedForeground, EncodedTokenAttributes.GetForeground(result));
            Assert.AreEqual(expectedBackground, EncodedTokenAttributes.GetBackground(result));
        }

        [Test]
        public void MergeAttributes_RuleForegroundZero_PreservesExistingForeground()
        {
            // arrange
            int existing = CreateNonDefaultEncodedMetadata();
            AttributedScopeStack scopesList = new AttributedScopeStack(null, AnyScopePath, existing);

            const int expectedBackground = 124;
            const FontStyle expectedFontStyle = FontStyle.Italic;

            List<string> parentScopes = [AnyScopePath];
            ThemeTrieElementRule rule = new ThemeTrieElementRule(
                "preserve-fg",
                1,
                parentScopes,
                expectedFontStyle,
                0,
                expectedBackground);

            List<ThemeTrieElementRule> themeData = [rule];
            BasicScopeAttributes attrs = new BasicScopeAttributes(NewLanguageId, NewTokenType, themeData);

            // act
            int result = AttributedScopeStack.MergeAttributes(existing, scopesList, attrs);

            // assert
            Assert.AreEqual(expectedFontStyle, EncodedTokenAttributes.GetFontStyle(result));
            Assert.AreEqual(ExistingForeground, EncodedTokenAttributes.GetForeground(result));
            Assert.AreEqual(expectedBackground, EncodedTokenAttributes.GetBackground(result));
        }

        [Test]
        public void MergeAttributes_RuleBackgroundZero_PreservesExistingBackground()
        {
            // arrange
            int existing = CreateNonDefaultEncodedMetadata();
            AttributedScopeStack scopesList = new AttributedScopeStack(null, AnyScopePath, existing);

            const int expectedForeground = 123;
            const FontStyle expectedFontStyle = FontStyle.Italic;

            ThemeTrieElementRule rule = new ThemeTrieElementRule(
                "preserve-bg",
                1,
                null,
                expectedFontStyle,
                expectedForeground,
                0);

            List<ThemeTrieElementRule> themeData = [rule];
            BasicScopeAttributes attrs = new BasicScopeAttributes(NewLanguageId, NewTokenType, themeData);

            // act
            int result = AttributedScopeStack.MergeAttributes(existing, scopesList, attrs);

            // assert
            Assert.AreEqual(expectedFontStyle, EncodedTokenAttributes.GetFontStyle(result));
            Assert.AreEqual(expectedForeground, EncodedTokenAttributes.GetForeground(result));
            Assert.AreEqual(ExistingBackground, EncodedTokenAttributes.GetBackground(result));
        }

        [Test]
        public void MergeAttributes_LanguageIdZero_PreservesExistingLanguageId()
        {
            // arrange
            int existing = CreateNonDefaultEncodedMetadata();
            AttributedScopeStack scopesList = new AttributedScopeStack(null, AnyScopePath, existing);
            BasicScopeAttributes attrs = new BasicScopeAttributes(0, NewTokenType, null);

            // act
            int result = AttributedScopeStack.MergeAttributes(existing, scopesList, attrs);

            // assert
            Assert.AreEqual(ExistingLanguageId, EncodedTokenAttributes.GetLanguageId(result));
        }

        [Test]
        public void MergeAttributes_NoRuleMatches_PreservesExistingStyleAndColors_ButUpdatesLanguageAndTokenType()
        {
            // arrange
            int existing = CreateNonDefaultEncodedMetadata();
            AttributedScopeStack scopesList = CreateStack(
                ("source.csharp", existing),
                ("meta.using", existing),
                ("keyword.control", existing));

            List<string> nonMatchingParentScopes = ["does.not.exist"];
            ThemeTrieElementRule nonMatchingRule = new ThemeTrieElementRule(
                "non-match",
                1,
                nonMatchingParentScopes,
                FontStyle.Italic,
                200,
                201);

            List<ThemeTrieElementRule> themeData = [nonMatchingRule];
            BasicScopeAttributes attrs = new BasicScopeAttributes(NewLanguageId, NewTokenType, themeData);

            // act
            int result = AttributedScopeStack.MergeAttributes(existing, scopesList, attrs);

            // assert
            Assert.AreEqual(NewLanguageId, EncodedTokenAttributes.GetLanguageId(result));
            Assert.AreEqual(NewTokenType, EncodedTokenAttributes.GetTokenType(result));
            Assert.AreEqual(ExistingFontStyle, EncodedTokenAttributes.GetFontStyle(result));
            Assert.AreEqual(ExistingForeground, EncodedTokenAttributes.GetForeground(result));
            Assert.AreEqual(ExistingBackground, EncodedTokenAttributes.GetBackground(result));
        }

        [Test]
        public void MergeAttributes_ScopesListNull_RuleWithParentScopes_DoesNotMatch()
        {
            // arrange
            int existing = CreateNonDefaultEncodedMetadata();

            List<string> parentScopes = ["source.csharp"];
            ThemeTrieElementRule rule = new ThemeTrieElementRule(
                "requires-parent",
                1,
                parentScopes,
                FontStyle.Italic,
                200,
                201);

            List<ThemeTrieElementRule> themeData = [rule];
            BasicScopeAttributes attrs = new BasicScopeAttributes(NewLanguageId, NewTokenType, themeData);

            // act
            int result = AttributedScopeStack.MergeAttributes(existing, null, attrs);

            // assert
            Assert.AreEqual(NewLanguageId, EncodedTokenAttributes.GetLanguageId(result));
            Assert.AreEqual(NewTokenType, EncodedTokenAttributes.GetTokenType(result));
            Assert.AreEqual(ExistingFontStyle, EncodedTokenAttributes.GetFontStyle(result));
            Assert.AreEqual(ExistingForeground, EncodedTokenAttributes.GetForeground(result));
            Assert.AreEqual(ExistingBackground, EncodedTokenAttributes.GetBackground(result));
        }

        [Test]
        public void MergeAttributes_PrefixSelector_DoesNotMatch_WhenScopeDoesNotHaveDotBoundary()
        {
            // arrange
            int existing = CreateNonDefaultEncodedMetadata();

            AttributedScopeStack scopesList = CreateStack(
                ("source.csharp", existing),
                ("metadata.block", existing));

            // selector "meta" should match "meta.something" but NOT "metadata.something"
            List<string> parentScopes = ["meta"];
            ThemeTrieElementRule rule = new ThemeTrieElementRule(
                "prefix-dot-boundary",
                1,
                parentScopes,
                FontStyle.Italic,
                200,
                201);

            List<ThemeTrieElementRule> themeData = [rule];
            BasicScopeAttributes attrs = new BasicScopeAttributes(NewLanguageId, NewTokenType, themeData);

            // act
            int result = AttributedScopeStack.MergeAttributes(existing, scopesList, attrs);

            // assert
            Assert.AreEqual(NewLanguageId, EncodedTokenAttributes.GetLanguageId(result));
            Assert.AreEqual(NewTokenType, EncodedTokenAttributes.GetTokenType(result));
            Assert.AreEqual(ExistingFontStyle, EncodedTokenAttributes.GetFontStyle(result));
            Assert.AreEqual(ExistingForeground, EncodedTokenAttributes.GetForeground(result));
            Assert.AreEqual(ExistingBackground, EncodedTokenAttributes.GetBackground(result));
        }

        [Test]
        public void MergeAttributes_ParentScopesMatchNonContiguously_Works()
        {
            // arrange
            int existing = CreateNonDefaultEncodedMetadata();

            // leaf -> root traversal will be: c, x, b, y, a
            AttributedScopeStack scopesList = CreateStack(
                ("a", existing),
                ("y", existing),
                ("b", existing),
                ("x", existing),
                ("c", existing));

            // match "b" then later "a" (non-contiguous)
            List<string> parentScopes = ["b", "a"];
            const int expectedForeground = 210;
            const int expectedBackground = 211;
            const FontStyle expectedFontStyle = FontStyle.Underline;

            ThemeTrieElementRule rule = new ThemeTrieElementRule(
                "non-contiguous",
                1,
                parentScopes,
                expectedFontStyle,
                expectedForeground,
                expectedBackground);

            List<ThemeTrieElementRule> themeData = [rule];
            BasicScopeAttributes attrs = new BasicScopeAttributes(NewLanguageId, NewTokenType, themeData);

            // act
            int result = AttributedScopeStack.MergeAttributes(existing, scopesList, attrs);

            // assert
            Assert.AreEqual(expectedFontStyle, EncodedTokenAttributes.GetFontStyle(result));
            Assert.AreEqual(expectedForeground, EncodedTokenAttributes.GetForeground(result));
            Assert.AreEqual(expectedBackground, EncodedTokenAttributes.GetBackground(result));
        }

        [Test]
        public void MergeAttributes_PreservesBalancedBracketsBit_WhenContainsBalancedBracketsIsNull()
        {
            // arrange
            int existing = EncodedTokenAttributes.Set(
                0,
                ExistingLanguageId,
                ExistingTokenType,
                true,
                ExistingFontStyle,
                ExistingForeground,
                ExistingBackground);

            Assert.IsTrue(EncodedTokenAttributes.ContainsBalancedBrackets(existing));

            AttributedScopeStack scopesList = new AttributedScopeStack(null, AnyScopePath, existing);

            // ThemeData null => MergeAttributes passes containsBalancedBrackets as null into EncodedTokenAttributes.Set (preserve existing).
            BasicScopeAttributes attrs = new BasicScopeAttributes(NewLanguageId, NewTokenType, null);

            // act
            int result = AttributedScopeStack.MergeAttributes(existing, scopesList, attrs);

            // assert
            Assert.IsTrue(EncodedTokenAttributes.ContainsBalancedBrackets(result));
        }

        [Test]
        public void MergeAttributes_WhenScopesListContainsNullScopePath_DoesNotThrow_AndRuleDoesNotMatch()
        {
            // arrange
            int existing = CreateNonDefaultEncodedMetadata();
            AttributedScopeStack scopesList = CreateStack(
                ("source.csharp", existing),
                (null, existing),
                ("keyword.control", existing));

            List<string> parentScopes = ["meta"];
            ThemeTrieElementRule rule = new ThemeTrieElementRule("null-scopepath", 1, parentScopes, FontStyle.Italic, 11, 12);

            List<ThemeTrieElementRule> themeData = [rule];
            BasicScopeAttributes attrs = new BasicScopeAttributes(NewLanguageId, NewTokenType, themeData);

            // act
            int result = AttributedScopeStack.MergeAttributes(existing, scopesList, attrs);

            // assert
            Assert.AreEqual(NewLanguageId, EncodedTokenAttributes.GetLanguageId(result));
            Assert.AreEqual(NewTokenType, EncodedTokenAttributes.GetTokenType(result));
            Assert.AreEqual(ExistingFontStyle, EncodedTokenAttributes.GetFontStyle(result));
            Assert.AreEqual(ExistingForeground, EncodedTokenAttributes.GetForeground(result));
            Assert.AreEqual(ExistingBackground, EncodedTokenAttributes.GetBackground(result));
        }

        #endregion MergeAttributes tests

        #region MatchesScope tests

        [Test]
        public void MergeAttributes_MatchesScope_NullScope_DoesNotMatch()
        {
            // arrange
            int existing = CreateNonDefaultEncodedMetadata();
            AttributedScopeStack scopesList = new AttributedScopeStack(null, null, existing);

            List<string> parentScopes = ["source"];
            ThemeTrieElementRule rule = new ThemeTrieElementRule("null-scope", 1, parentScopes, FontStyle.Italic, 101, 102);
            BasicScopeAttributes attrs = new BasicScopeAttributes(NewLanguageId, NewTokenType, [rule]);

            // act
            int result = AttributedScopeStack.MergeAttributes(existing, scopesList, attrs);

            // assert
            Assert.AreEqual(ExistingFontStyle, EncodedTokenAttributes.GetFontStyle(result));
            Assert.AreEqual(ExistingForeground, EncodedTokenAttributes.GetForeground(result));
            Assert.AreEqual(ExistingBackground, EncodedTokenAttributes.GetBackground(result));
        }

        [Test]
        public void MergeAttributes_MatchesScope_NullSelector_DoesNotMatch()
        {
            // arrange
            int existing = CreateNonDefaultEncodedMetadata();
            AttributedScopeStack scopesList = new AttributedScopeStack(null, "source.js", existing);

            List<string> parentScopes = [null];
            ThemeTrieElementRule rule = new ThemeTrieElementRule("null-selector", 1, parentScopes, FontStyle.Italic, 111, 112);
            BasicScopeAttributes attrs = new BasicScopeAttributes(NewLanguageId, NewTokenType, [rule]);

            // act
            int result = AttributedScopeStack.MergeAttributes(existing, scopesList, attrs);

            // assert
            Assert.AreEqual(ExistingFontStyle, EncodedTokenAttributes.GetFontStyle(result));
            Assert.AreEqual(ExistingForeground, EncodedTokenAttributes.GetForeground(result));
            Assert.AreEqual(ExistingBackground, EncodedTokenAttributes.GetBackground(result));
        }

        [Test]
        public void MergeAttributes_MatchesScope_ExactMatch_AppliesRule()
        {
            // arrange
            int existing = CreateNonDefaultEncodedMetadata();
            AttributedScopeStack scopesList = new AttributedScopeStack(null, "source.js", existing);

            const int expectedForeground = 201;
            const int expectedBackground = 202;
            const FontStyle expectedFontStyle = FontStyle.Underline;

            List<string> parentScopes = ["source.js"];
            ThemeTrieElementRule rule = new ThemeTrieElementRule("exact-match", 1, parentScopes, expectedFontStyle, expectedForeground, expectedBackground);
            BasicScopeAttributes attrs = new BasicScopeAttributes(NewLanguageId, NewTokenType, [rule]);

            // act
            int result = AttributedScopeStack.MergeAttributes(existing, scopesList, attrs);

            // assert
            Assert.AreEqual(expectedFontStyle, EncodedTokenAttributes.GetFontStyle(result));
            Assert.AreEqual(expectedForeground, EncodedTokenAttributes.GetForeground(result));
            Assert.AreEqual(expectedBackground, EncodedTokenAttributes.GetBackground(result));
        }

        [Test]
        public void MergeAttributes_MatchesScope_PrefixWithDot_AppliesRule()
        {
            // arrange
            int existing = CreateNonDefaultEncodedMetadata();
            AttributedScopeStack scopesList = new AttributedScopeStack(null, "source.js", existing);

            const int expectedForeground = 211;
            const int expectedBackground = 212;
            const FontStyle expectedFontStyle = FontStyle.Italic;

            List<string> parentScopes = ["source"];
            ThemeTrieElementRule rule = new ThemeTrieElementRule("prefix-dot", 1, parentScopes, expectedFontStyle, expectedForeground, expectedBackground);
            BasicScopeAttributes attrs = new BasicScopeAttributes(NewLanguageId, NewTokenType, [rule]);

            // act
            int result = AttributedScopeStack.MergeAttributes(existing, scopesList, attrs);

            // assert
            Assert.AreEqual(expectedFontStyle, EncodedTokenAttributes.GetFontStyle(result));
            Assert.AreEqual(expectedForeground, EncodedTokenAttributes.GetForeground(result));
            Assert.AreEqual(expectedBackground, EncodedTokenAttributes.GetBackground(result));
        }

        [Test]
        public void MergeAttributes_MatchesScope_PrefixWithoutDot_DoesNotMatch()
        {
            // arrange
            int existing = CreateNonDefaultEncodedMetadata();
            AttributedScopeStack scopesList = new AttributedScopeStack(null, "sourcejs", existing);

            List<string> parentScopes = ["source"];
            ThemeTrieElementRule rule = new ThemeTrieElementRule("prefix-no-dot", 1, parentScopes, FontStyle.Italic, 221, 222);
            BasicScopeAttributes attrs = new BasicScopeAttributes(NewLanguageId, NewTokenType, [rule]);

            // act
            int result = AttributedScopeStack.MergeAttributes(existing, scopesList, attrs);

            // assert
            Assert.AreEqual(ExistingFontStyle, EncodedTokenAttributes.GetFontStyle(result));
            Assert.AreEqual(ExistingForeground, EncodedTokenAttributes.GetForeground(result));
            Assert.AreEqual(ExistingBackground, EncodedTokenAttributes.GetBackground(result));
        }

        #endregion MatchesScope tests

        #region PushAttributed tests

        [Test]
        public void PushAttributed_NullScopePath_ReturnsSameInstance()
        {
            // arrange
            AttributedScopeStack stack = CreateStack(("a", 1), ("b", 2));

            // act
            AttributedScopeStack result = stack.PushAttributed(null, null);

            // assert
            Assert.AreSame(stack, result);
        }

        [Test]
        public void PushAttributed_NonNullScope_WithNullGrammar_ThrowsArgumentNullException()
        {
            // arrange
            AttributedScopeStack stack = CreateStack(("a", 1));

            // act/assert
            Assert.Throws<ArgumentNullException>(() => stack.PushAttributed("b", null));
        }

        [Test]
        public void PushAttributed_MultiScope_WithNullGrammar_ThrowsArgumentNullException()
        {
            // arrange
            AttributedScopeStack stack = CreateStack(("a", 1));

            // act/assert
            Assert.Throws<ArgumentNullException>(() => stack.PushAttributed("b c", null));
        }

        [Test]
        public void PushAttributed_EmptyStringScope_WithNullGrammar_ThrowsArgumentNullException()
        {
            // arrange
            AttributedScopeStack stack = CreateStack(("a", 1));

            // act/assert
            Assert.Throws<ArgumentNullException>(() => stack.PushAttributed("", null));
        }

        [Test]
        public void PushAttributed_ShouldHandleTrailingSpacesAndProduceEmptySegment()
        {
            // Arrange
            TextMateSharp.Internal.Grammars.Grammar grammar = CreateTestGrammar();
            AttributedScopeStack initial = new AttributedScopeStack(null, "root", 0);

            // Act
            AttributedScopeStack result = initial.PushAttributed("a b ", grammar);
            List<string> scopes = result.GetScopeNames();

            // Assert
            CollectionAssert.AreEqual(new List<string> { "root", "a", "b", "" }, scopes);
        }

        [Test]
        public void PushAttributed_MultiScope_ProducesSegments()
        {
            // Arrange
            TextMateSharp.Internal.Grammars.Grammar grammar = CreateTestGrammar();
            AttributedScopeStack initial = new AttributedScopeStack(null, "root", 0);

            // Act
            AttributedScopeStack result = initial.PushAttributed("a b", grammar);
            List<string> scopes = result.GetScopeNames();

            // Assert
            CollectionAssert.AreEqual(new List<string> { "root", "a", "b" }, scopes);
        }

        [Test]
        public void PushAttributed_SingleScope_PreservesScopeStringInstance()
        {
            // Arrange
            TextMateSharp.Internal.Grammars.Grammar grammar = CreateTestGrammar();
            AttributedScopeStack initial = new AttributedScopeStack(null, "root", 0);
            const string scopePath = "single.scope";

            // Act
            AttributedScopeStack result = initial.PushAttributed(scopePath, grammar);

            // Assert
            Assert.AreSame(scopePath, result.ScopePath);
        }

        #endregion PushAttributed tests

        #region ToString tests

        [Test]
        public void ToString_SingleDepthStack_ReturnsFormattedString()
        {
            // arrange
            AttributedScopeStack stack = new AttributedScopeStack(null, "source.cs", 1);

            // act
            string result = stack.ToString();

            // assert
            Assert.AreEqual("source.cs", result);
        }

        [Test]
        public void ToString_MultiDepthStack_ReturnsSpaceSeparatedScopes()
        {
            // arrange
            AttributedScopeStack stack = CreateStack(("source.cs", 1), ("meta.test", 2));

            // act
            string result = stack.ToString();

            // assert
            Assert.AreEqual("source.cs meta.test", result);
        }

        [Test]
        public void ToString_ThreeDepthStack_ReturnsCorrectOrder()
        {
            // arrange
            AttributedScopeStack stack = CreateStack(("a", 1), ("b", 2), ("c", 3));

            // act
            string result = stack.ToString();

            // assert
            Assert.AreEqual("a b c", result);
        }

        [Test]
        public void ToString_CalledMultipleTimes_ReturnsSameResult()
        {
            // arrange
            AttributedScopeStack stack = CreateStack(("a", 1), ("b", 2));

            // act
            string result1 = stack.ToString();
            string result2 = stack.ToString();
            string result3 = stack.ToString();

            // assert
            Assert.AreEqual(result1, result2);
            Assert.AreEqual(result2, result3);
            Assert.AreEqual("a b", result1);
        }

        [Test]
        public void ToString_BoundaryVeryLargeDepth_ReturnsCorrectFormat()
        {
            // arrange
            const int veryLargeDepth = 100;
            AttributedScopeStack current = null;
            for (int i = 0; i < veryLargeDepth; i++)
            {
                current = new AttributedScopeStack(current, "s" + i, i);
            }

            // act
            string result = current!.ToString();

            // assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.StartsWith("s0 "));
            Assert.IsTrue(result.EndsWith(" s99"));
            Assert.AreEqual(100, result.Split(' ').Length); // 100 scopes
        }

        [Test]
        public void ToString_WithNullScopePath_IncludesEmptyString()
        {
            // arrange
            AttributedScopeStack stack = CreateStack(("a", 1), (null, 2), ("c", 3));

            // act
            string result = stack.ToString();

            // assert
            // null scope path should appear as empty string in output
            Assert.AreEqual("a  c", result); // note: two spaces (empty string between)
        }

        #endregion ToString tests

        #region Helpers

        private static TextMateSharp.Internal.Grammars.Grammar CreateTestGrammar()
        {
            const string scopeName = "source.test";
            Raw rawGrammar = new Raw
            {
                ["scopeName"] = scopeName
            };

            ThemeTrieElementRule defaults = new ThemeTrieElementRule(
                "defaults",
                0,
                null,
                ExistingFontStyle,
                ExistingForeground,
                ExistingBackground);

            Mock<IThemeProvider> themeProvider = new Mock<IThemeProvider>();
            themeProvider.Setup(provider => provider.GetDefaults()).Returns(defaults);
            themeProvider
                .Setup(provider => provider.ThemeMatch(It.IsAny<IList<string>>()))
                .Returns([]);

            return new TextMateSharp.Internal.Grammars.Grammar(
                scopeName,
                rawGrammar,
                0,
                null,
                null,
                new BalancedBracketSelectors([], []),
                new Mock<IGrammarRepository>().Object,
                themeProvider.Object);
        }

        private static AttributedScopeStack CreateStack(params (string ScopePath, int TokenAttributes)[] frames)
        {
            AttributedScopeStack current = null;
            for (int i = 0; i < frames.Length; i++)
            {
                (string ScopePath, int TokenAttributes) frame = frames[i];
                current = new AttributedScopeStack(current, frame.ScopePath, frame.TokenAttributes);
            }

            return current;
        }

        private static int CreateNonDefaultEncodedMetadata()
        {
            // Choose non-default values so EncodedTokenAttributes.Set "preserve existing" behavior is observable.
            return EncodedTokenAttributes.Set(
                0,
                ExistingLanguageId,
                ExistingTokenType,
                null,
                ExistingFontStyle,
                ExistingForeground,
                ExistingBackground);
        }

        // Normal call paths cannot hit the branches of the static Equals impl, so I'm adding this reflection-based
        // helper to help improve the test coverage and cover the branches that cannot otherwise be executed.
        private static MethodInfo GetPrivateStaticEqualsMethod()
        {
            Type type = typeof(AttributedScopeStack);
            Type[] parameterTypes = [typeof(AttributedScopeStack), typeof(AttributedScopeStack)];

            MethodInfo methodInfo = type.GetMethod(
                nameof(Equals),
                BindingFlags.NonPublic | BindingFlags.Static,
                null,
                parameterTypes,
                null);

            Assert.IsNotNull(methodInfo);
            return methodInfo;
        }

        #endregion Helpers
    }
}

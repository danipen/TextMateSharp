using NUnit.Framework;
using System;
using System.Collections.Generic;
using TextMateSharp.Grammars;
using TextMateSharp.Internal.Grammars;
using TextMateSharp.Internal.Rules;

namespace TextMateSharp.Tests.Grammar
{
    [TestFixture]
    public class StateStackTests
    {
        private const int RuleIdSingleDepth = 42;
        private const int RuleIdDepthTwo = 100;
        private const int RuleIdDepthThree = 200;
        private const int EnterPosition = 0;
        private const int AnchorPosition = 0;

        #region ToString tests

        [Test]
        public void ToString_SingleDepthState_ReturnsFormattedString()
        {
            // Arrange
            StateStack stack = new StateStack(
                null,
                RuleId.Of(RuleIdSingleDepth),
                EnterPosition,
                AnchorPosition,
                false,
                null,
                CreateTestScopeStack(),
                CreateTestScopeStack());

            const string expectedOutput = "[(42)]";

            // Act
            string result = stack.ToString();

            // Assert
            Assert.AreEqual(expectedOutput, result);
        }

        [Test]
        public void ToString_TwoDepthState_ReturnsFormattedStringWithBothRules()
        {
            // Arrange
            StateStack parent = new StateStack(
                null,
                RuleId.Of(RuleIdSingleDepth),
                EnterPosition,
                AnchorPosition,
                false,
                null,
                CreateTestScopeStack(),
                CreateTestScopeStack());

            StateStack stack = parent.Push(
                RuleId.Of(RuleIdDepthTwo),
                EnterPosition,
                AnchorPosition,
                false,
                null,
                CreateTestScopeStack(),
                CreateTestScopeStack());

            const string expectedOutput = "[(42), (100)]";

            // Act
            string result = stack.ToString();

            // Assert
            Assert.AreEqual(expectedOutput, result);
        }

        [Test]
        public void ToString_ThreeDepthState_ReturnsFormattedStringWithAllRules()
        {
            // Arrange
            StateStack level1 = new StateStack(
                null,
                RuleId.Of(RuleIdSingleDepth),
                EnterPosition,
                AnchorPosition,
                false,
                null,
                CreateTestScopeStack(),
                CreateTestScopeStack());

            StateStack level2 = level1.Push(
                RuleId.Of(RuleIdDepthTwo),
                EnterPosition,
                AnchorPosition,
                false,
                null,
                CreateTestScopeStack(),
                CreateTestScopeStack());

            StateStack level3 = level2.Push(
                RuleId.Of(RuleIdDepthThree),
                EnterPosition,
                AnchorPosition,
                false,
                null,
                CreateTestScopeStack(),
                CreateTestScopeStack());

            const string expectedOutput = "[(42), (100), (200)]";

            // Act
            string result = level3.ToString();

            // Assert
            Assert.AreEqual(expectedOutput, result);
        }

        [Test]
        public void ToString_NullStaticInstance_ReturnsFormattedNoRuleString()
        {
            // Arrange
            StateStack stack = StateStack.NULL;
            const string expectedOutput = "[(0)]";

            // Act
            string result = stack.ToString();

            // Assert
            Assert.AreEqual(expectedOutput, result);
        }

        [Test]
        public void ToString_StateWithNoRuleId_ReturnsFormattedNoRuleString()
        {
            // Arrange
            StateStack stack = new StateStack(
                null,
                RuleId.NO_RULE,
                EnterPosition,
                AnchorPosition,
                false,
                null,
                CreateTestScopeStack(),
                CreateTestScopeStack());

            const string expectedOutput = "[(0)]";

            // Act
            string result = stack.ToString();

            // Assert
            Assert.AreEqual(expectedOutput, result);
        }

        [Test]
        public void ToString_StateWithEndRuleId_ReturnsFormattedEndRuleString()
        {
            // Arrange
            StateStack stack = new StateStack(
                null,
                RuleId.END_RULE,
                EnterPosition,
                AnchorPosition,
                false,
                null,
                CreateTestScopeStack(),
                CreateTestScopeStack());

            const string expectedOutput = "[(-1)]";

            // Act
            string result = stack.ToString();

            // Assert
            Assert.AreEqual(expectedOutput, result);
        }

        [Test]
        public void ToString_StateWithWhileRuleId_ReturnsFormattedWhileRuleString()
        {
            // Arrange
            StateStack stack = new StateStack(
                null,
                RuleId.WHILE_RULE,
                EnterPosition,
                AnchorPosition,
                false,
                null,
                CreateTestScopeStack(),
                CreateTestScopeStack());

            const string expectedOutput = "[(-2)]";

            // Act
            string result = stack.ToString();

            // Assert
            Assert.AreEqual(expectedOutput, result);
        }

        [Test]
        public void ToString_BoundaryDepthZero_ReturnsNullStackString()
        {
            // Arrange - depth 0 returns StateStack.NULL
            const int depthZero = 0;
            StateStack stack = CreateStateStackWithDepth(depthZero);
            const string expectedOutput = "[(0)]";

            // Act
            string result = stack.ToString();

            // Assert
            Assert.AreEqual(expectedOutput, result);
            Assert.AreSame(StateStack.NULL, stack);
        }

        [Test]
        public void ToString_BoundaryDepthOne_ReturnsSinglePushString()
        {
            // Arrange - depth 1 is one push on NULL
            const int depthOne = 1;
            StateStack stack = CreateStateStackWithDepth(depthOne);
            const string expectedOutput = "[(0), (0)]";

            // Act
            string result = stack.ToString();

            // Assert
            Assert.AreEqual(expectedOutput, result);
        }

        [Test]
        public void ToString_BoundaryVeryLargeDepth_ReturnsFormattedStringWithAllLevels()
        {
            // Arrange - test large depth to verify performance and correctness
            const int veryLargeDepth = 100;
            StateStack stack = CreateStateStackWithDepth(veryLargeDepth);

            // Act
            string result = stack.ToString();

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.StartsWith("[(0)"));
            Assert.IsTrue(result.EndsWith("(9900)]"));

            // Verify correct number of elements: NULL (1) + 100 pushes = 101 elements
            const int expectedCommaCount = 100;
            int actualCommaCount = result.Split(',').Length - 1;
            Assert.AreEqual(expectedCommaCount, actualCommaCount);
        }

        [Test]
        public void ToString_CalledMultipleTimes_ReturnsSameResult()
        {
            // Arrange
            StateStack stack = new StateStack(
                null,
                RuleId.Of(RuleIdSingleDepth),
                EnterPosition,
                AnchorPosition,
                false,
                null,
                CreateTestScopeStack(),
                CreateTestScopeStack());

            // Act
            string result1 = stack.ToString();
            string result2 = stack.ToString();
            string result3 = stack.ToString();

            // Assert
            Assert.AreEqual(result1, result2);
            Assert.AreEqual(result2, result3);
        }

        [Test]
        public void ToString_StackWithMixedRuleIds_ReturnsCorrectOrderFromRootToCurrent()
        {
            // Arrange
            StateStack root = new StateStack(
                null,
                RuleId.Of(RuleIdSingleDepth),
                EnterPosition,
                AnchorPosition,
                false,
                null,
                CreateTestScopeStack(),
                CreateTestScopeStack());

            StateStack middle = root.Push(
                RuleId.Of(RuleIdDepthTwo),
                EnterPosition,
                AnchorPosition,
                false,
                null,
                CreateTestScopeStack(),
                CreateTestScopeStack());

            StateStack current = middle.Push(
                RuleId.Of(RuleIdDepthThree),
                EnterPosition,
                AnchorPosition,
                false,
                null,
                CreateTestScopeStack(),
                CreateTestScopeStack());

            const string expectedOutput = "[(42), (100), (200)]";

            // Act
            string result = current.ToString();

            // Assert
            Assert.AreEqual(expectedOutput, result);
        }

        #endregion ToString tests

        #region GetHashCode tests

        [Test]
        public void GetHashCode_NullSentinel_DoesNotThrow()
        {
            // Arrange
            StateStack stack = StateStack.NULL;

            // Act & Assert
            // GetHashCode is only guaranteed to be deterministic within a single application run and may vary across runs,
            // so this test only verifies that calling it does not throw, without asserting on a specific hash value.
            Assert.DoesNotThrow(() => _ = stack.GetHashCode());
        }

        [Test]
        public void GetHashCode_NullSentinel_IsDeterministic()
        {
            // Arrange
            StateStack stack = StateStack.NULL;

            // Act
            int first = stack.GetHashCode();
            int second = stack.GetHashCode();

            // Assert
            Assert.AreEqual(first, second);
        }

        [Test]
        public void GetHashCode_NullEndRule_DoesNotThrow()
        {
            // Arrange
            StateStack stack = new StateStack(
                null,
                RuleId.Of(RuleIdSingleDepth),
                EnterPosition,
                AnchorPosition,
                false,
                null,
                CreateTestScopeStack(),
                CreateTestScopeStack());

            // Act & Assert
            // GetHashCode doesn't produce a deterministic value across application runs, so we can't assert on specific values
            Assert.DoesNotThrow(() => _ = stack.GetHashCode());
        }

        [Test]
        public void GetHashCode_NullContentNameScopesList_DoesNotThrow()
        {
            // Arrange
            StateStack stack = new StateStack(
                null,
                RuleId.Of(RuleIdSingleDepth),
                EnterPosition,
                AnchorPosition,
                false,
                "endRule",
                CreateTestScopeStack(),
                null);

            // Act & Assert
            // GetHashCode doesn't produce a deterministic value across application runs, so we can't assert on specific values
            Assert.DoesNotThrow(() => _ = stack.GetHashCode());
        }

        [Test]
        public void GetHashCode_NullParent_DoesNotThrow()
        {
            // Arrange
            StateStack stack = new StateStack(
                null,
                RuleId.Of(RuleIdSingleDepth),
                EnterPosition,
                AnchorPosition,
                false,
                "endRule",
                CreateTestScopeStack(),
                CreateTestScopeStack());

            // Act & Assert
            // GetHashCode doesn't produce a deterministic value across application runs, so we can't assert on specific values
            Assert.DoesNotThrow(() => _ = stack.GetHashCode());
        }

        [Test]
        public void GetHashCode_AllFieldsNull_DoesNotThrow()
        {
            // Arrange - worst-case null scenario
            StateStack stack = new StateStack(
                null,
                RuleId.NO_RULE,
                EnterPosition,
                AnchorPosition,
                false,
                null,
                null,
                null);

            // Act & Assert
            // GetHashCode doesn't produce a deterministic value across application runs, so we can't assert on specific values
            Assert.DoesNotThrow(() => _ = stack.GetHashCode());
        }

        [Test]
        public void GetHashCode_EqualStacks_ReturnSameValue()
        {
            // Arrange
            AttributedScopeStack scopeStack = CreateTestScopeStack();

            StateStack left = new StateStack(
                null,
                RuleId.Of(RuleIdSingleDepth),
                EnterPosition,
                AnchorPosition,
                false,
                "endRule",
                scopeStack,
                scopeStack);

            StateStack right = new StateStack(
                null,
                RuleId.Of(RuleIdSingleDepth),
                EnterPosition,
                AnchorPosition,
                false,
                "endRule",
                scopeStack,
                scopeStack);

            // Act
            int leftHash = left.GetHashCode();
            int rightHash = right.GetHashCode();

            // Assert - equal objects must have the same hash code
            Assert.AreEqual(leftHash, rightHash);
        }

        [Test]
        public void GetHashCode_EqualObjects_ReturnSameValue()
        {
            // Arrange
            AttributedScopeStack scopeStack = CreateTestScopeStack();

            StateStack left = new StateStack(
                null,
                RuleId.Of(RuleIdSingleDepth),
                EnterPosition,
                AnchorPosition,
                true,
                "endRule",
                scopeStack,
                scopeStack);

            StateStack right = new StateStack(
                null,
                RuleId.Of(RuleIdSingleDepth),
                EnterPosition,
                AnchorPosition,
                true,
                "endRule",
                scopeStack,
                scopeStack);

            // Act
            int leftHash = left.GetHashCode();
            int rightHash = right.GetHashCode();

            // Assert - equal objects must have the same hash code
            Assert.AreEqual(leftHash, rightHash);
        }

        [Test]
        public void GetHashCode_UsedAsDictionaryKey_AllowsLookupWithEqualStack()
        {
            // Arrange
            AttributedScopeStack scopeStack = CreateTestScopeStack();

            StateStack key1 = new StateStack(
                null,
                RuleId.Of(RuleIdSingleDepth),
                EnterPosition,
                AnchorPosition,
                false,
                "endRule",
                scopeStack,
                scopeStack);

            StateStack key2 = new StateStack(
                null,
                RuleId.Of(RuleIdSingleDepth),
                EnterPosition,
                AnchorPosition,
                false,
                "endRule",
                scopeStack,
                scopeStack);

            Dictionary<StateStack, string> dictionary = new Dictionary<StateStack, string>
            {
                [key1] = "VALUE"
            };

            // Act
            bool found = dictionary.TryGetValue(key2, out string value);

            // Assert
            Assert.IsTrue(found);
            Assert.AreEqual("VALUE", value);
        }

        [Test]
        public void GetHashCode_DeepStack_DoesNotThrow_AndIsDeterministic()
        {
            // Arrange
            const int depth = 250;
            StateStack stack = CreateStateStackWithDepth(depth);

            // Act
            int first = stack.GetHashCode();
            int second = stack.GetHashCode();

            // Assert
            Assert.AreEqual(first, second);
        }

        [Test]
        public void GetHashCode_DifferentEnterPos_ReturnsSameValue()
        {
            // Arrange - _enterPos is NOT part of hash/equality
            StateStack left = new StateStack(
                null,
                RuleId.Of(RuleIdSingleDepth),
                0,
                AnchorPosition,
                false,
                null,
                CreateTestScopeStack(),
                CreateTestScopeStack());

            StateStack right = new StateStack(
                null,
                RuleId.Of(RuleIdSingleDepth),
                999,
                AnchorPosition,
                false,
                null,
                CreateTestScopeStack(),
                CreateTestScopeStack());

            // Act & Assert - _enterPos should not affect hash
            Assert.AreEqual(left.GetHashCode(), right.GetHashCode());
        }

        [Test]
        public void GetHashCode_DifferentAnchorPos_ReturnsSameValue()
        {
            // Arrange - _anchorPos is NOT part of hash/equality
            StateStack left = new StateStack(
                null,
                RuleId.Of(RuleIdSingleDepth),
                EnterPosition,
                0,
                false,
                null,
                CreateTestScopeStack(),
                CreateTestScopeStack());

            StateStack right = new StateStack(
                null,
                RuleId.Of(RuleIdSingleDepth),
                EnterPosition,
                999,
                false,
                null,
                CreateTestScopeStack(),
                CreateTestScopeStack());

            // Act & Assert - _anchorPos should not affect hash
            Assert.AreEqual(left.GetHashCode(), right.GetHashCode());
        }

        [Test]
        public void GetHashCode_DifferentBeginRuleCapturedEOL_ReturnsSameValue()
        {
            // Arrange - BeginRuleCapturedEOL is NOT part of hash/equality (matches upstream)
            StateStack left = new StateStack(
                null,
                RuleId.Of(RuleIdSingleDepth),
                EnterPosition,
                AnchorPosition,
                false,
                null,
                CreateTestScopeStack(),
                CreateTestScopeStack());

            StateStack right = new StateStack(
                null,
                RuleId.Of(RuleIdSingleDepth),
                EnterPosition,
                AnchorPosition,
                true,
                null,
                CreateTestScopeStack(),
                CreateTestScopeStack());

            // Act & Assert - BeginRuleCapturedEOL should not affect hash
            Assert.AreEqual(left.GetHashCode(), right.GetHashCode());
        }

        #endregion GetHashCode tests

        #region Equals (object) tests

        [Test]
        public void Equals_SameReference_ReturnsTrue()
        {
            // Arrange
            StateStack stack = new StateStack(
                null,
                RuleId.Of(RuleIdSingleDepth),
                EnterPosition,
                AnchorPosition,
                false,
                null,
                CreateTestScopeStack(),
                CreateTestScopeStack());

            // Act
            bool result = stack.Equals((object)stack);

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public void Equals_EquivalentObjects_ReturnsTrue()
        {
            // Arrange
            AttributedScopeStack scopeStack = CreateTestScopeStack();

            StateStack left = new StateStack(
                null,
                RuleId.Of(RuleIdSingleDepth),
                EnterPosition,
                AnchorPosition,
                true,
                "endRule",
                scopeStack,
                scopeStack);

            StateStack right = new StateStack(
                null,
                RuleId.Of(RuleIdSingleDepth),
                EnterPosition,
                AnchorPosition,
                true,
                "endRule",
                scopeStack,
                scopeStack);

            // Act
            bool result = left.Equals(right);

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public void Equals_IsReflexive()
        {
            // Arrange
            StateStack stack = new StateStack(
                null,
                RuleId.Of(RuleIdSingleDepth),
                EnterPosition,
                AnchorPosition,
                false,
                null,
                CreateTestScopeStack(),
                CreateTestScopeStack());

            // Act
            bool result = stack.Equals((object)stack);

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public void Equals_IsSymmetric()
        {
            // Arrange
            AttributedScopeStack scopeStack = CreateTestScopeStack();
            StateStack left = new StateStack(
                null,
                RuleId.Of(RuleIdSingleDepth),
                EnterPosition,
                AnchorPosition,
                false,
                "endRule",
                scopeStack,
                scopeStack);

            StateStack right = new StateStack(
                null,
                RuleId.Of(RuleIdSingleDepth),
                EnterPosition,
                AnchorPosition,
                false,
                "endRule",
                scopeStack,
                scopeStack);
            // Act
            bool leftEqualsRight = left.Equals((object)right);
            bool rightEqualsLeft = right.Equals((object)left);

            // Assert
            Assert.IsTrue(leftEqualsRight);
            Assert.IsTrue(rightEqualsLeft);
        }

        [Test]
        public void Equals_Null_ReturnsFalse()
        {
            // Arrange
            StateStack stack = new StateStack(
                null,
                RuleId.Of(RuleIdSingleDepth),
                EnterPosition,
                AnchorPosition,
                false,
                null,
                CreateTestScopeStack(),
                CreateTestScopeStack());

            // Act
            bool result = stack.Equals((object)null);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void Equals_DifferentType_ReturnsFalse()
        {
            // Arrange
            StateStack stack = new StateStack(
                null,
                RuleId.Of(RuleIdSingleDepth),
                EnterPosition,
                AnchorPosition,
                false,
                null,
                CreateTestScopeStack(),
                CreateTestScopeStack());

            // Act
            bool result = stack.Equals(42);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void Equals_StructurallyEqualStacks_ReturnsTrue()
        {
            // Arrange
            AttributedScopeStack scopeStack = CreateTestScopeStack();

            StateStack left = new StateStack(
                null,
                RuleId.Of(RuleIdSingleDepth),
                EnterPosition,
                AnchorPosition,
                false,
                "endRule",
                scopeStack,
                scopeStack);

            StateStack right = new StateStack(
                null,
                RuleId.Of(RuleIdSingleDepth),
                EnterPosition,
                AnchorPosition,
                false,
                "endRule",
                scopeStack,
                scopeStack);

            // Act
            bool leftEqualsRight = left.Equals((object)right);
            bool rightEqualsLeft = right.Equals((object)left);

            // Assert
            Assert.IsTrue(leftEqualsRight);
            Assert.IsTrue(rightEqualsLeft);
        }

        [Test]
        public void Equals_DifferentRuleId_ReturnsFalse()
        {
            // Arrange
            StateStack left = new StateStack(
                null,
                RuleId.Of(1),
                EnterPosition,
                AnchorPosition,
                false,
                null,
                CreateTestScopeStack(),
                CreateTestScopeStack());

            StateStack right = new StateStack(
                null,
                RuleId.Of(2),
                EnterPosition,
                AnchorPosition,
                false,
                null,
                CreateTestScopeStack(),
                CreateTestScopeStack());

            // Act & Assert
            Assert.IsFalse(left.Equals(right));
        }

        [Test]
        public void Equals_DifferentEndRule_ReturnsFalse()
        {
            // Arrange
            StateStack left = new StateStack(
                null,
                RuleId.Of(RuleIdSingleDepth),
                EnterPosition,
                AnchorPosition,
                false,
                "endRuleA",
                CreateTestScopeStack(),
                CreateTestScopeStack());

            StateStack right = new StateStack(
                null,
                RuleId.Of(RuleIdSingleDepth),
                EnterPosition,
                AnchorPosition,
                false,
                "endRuleB",
                CreateTestScopeStack(),
                CreateTestScopeStack());

            // Act & Assert
            Assert.IsFalse(left.Equals(right));
        }

        [Test]
        public void Equals_DifferentDepth_ReturnsFalse()
        {
            // Arrange
            StateStack shallow = new StateStack(
                null,
                RuleId.Of(RuleIdSingleDepth),
                EnterPosition,
                AnchorPosition,
                false,
                null,
                CreateTestScopeStack(),
                CreateTestScopeStack());

            StateStack deep = shallow.Push(
                RuleId.Of(RuleIdSingleDepth),
                EnterPosition,
                AnchorPosition,
                false,
                null,
                CreateTestScopeStack(),
                CreateTestScopeStack());

            // Act & Assert
            Assert.IsFalse(shallow.Equals(deep));
            Assert.IsFalse(deep.Equals(shallow));
        }

        [Test]
        public void Equals_DifferentContentNameScopesList_ReturnsFalse()
        {
            // Arrange
            AttributedScopeStack contentA = new AttributedScopeStack(null, "scope.a", 1);
            AttributedScopeStack contentB = new AttributedScopeStack(null, "scope.b", 2);

            StateStack left = new StateStack(
                null,
                RuleId.Of(RuleIdSingleDepth),
                EnterPosition,
                AnchorPosition,
                false,
                null,
                CreateTestScopeStack(),
                contentA);

            StateStack right = new StateStack(
                null,
                RuleId.Of(RuleIdSingleDepth),
                EnterPosition,
                AnchorPosition,
                false,
                null,
                CreateTestScopeStack(),
                contentB);

            // Act & Assert
            Assert.IsFalse(left.Equals(right));
        }

        [Test]
        public void Equals_NullContentNameScopesListOnBothSides_ReturnsTrue()
        {
            // Arrange - previously would throw NullReferenceException
            StateStack left = new StateStack(
                null,
                RuleId.Of(RuleIdSingleDepth),
                EnterPosition,
                AnchorPosition,
                false,
                null,
                CreateTestScopeStack(),
                null);

            StateStack right = new StateStack(
                null,
                RuleId.Of(RuleIdSingleDepth),
                EnterPosition,
                AnchorPosition,
                false,
                null,
                CreateTestScopeStack(),
                null);

            // Act & Assert
            Assert.IsTrue(left.Equals(right));
        }

        [Test]
        public void Equals_NullContentNameScopesListOnOneSide_ReturnsFalse()
        {
            // Arrange
            StateStack left = new StateStack(
                null,
                RuleId.Of(RuleIdSingleDepth),
                EnterPosition,
                AnchorPosition,
                false,
                null,
                CreateTestScopeStack(),
                null);

            StateStack right = new StateStack(
                null,
                RuleId.Of(RuleIdSingleDepth),
                EnterPosition,
                AnchorPosition,
                false,
                null,
                CreateTestScopeStack(),
                CreateTestScopeStack());

            // Act & Assert
            Assert.IsFalse(left.Equals(right));
            Assert.IsFalse(right.Equals(left));
        }

        [Test]
        public void Equals_NullEndRuleOnBothSides_ReturnsTrue()
        {
            // Arrange
            StateStack left = new StateStack(
                null,
                RuleId.Of(RuleIdSingleDepth),
                EnterPosition,
                AnchorPosition,
                false,
                null,
                CreateTestScopeStack(),
                CreateTestScopeStack());

            StateStack right = new StateStack(
                null,
                RuleId.Of(RuleIdSingleDepth),
                EnterPosition,
                AnchorPosition,
                false,
                null,
                CreateTestScopeStack(),
                CreateTestScopeStack());

            // Act & Assert
            Assert.IsTrue(left.Equals(right));
        }

        [Test]
        public void Equals_NullEndRuleOnOneSide_ReturnsFalse()
        {
            // Arrange
            StateStack left = new StateStack(
                null,
                RuleId.Of(RuleIdSingleDepth),
                EnterPosition,
                AnchorPosition,
                false,
                null,
                CreateTestScopeStack(),
                CreateTestScopeStack());

            StateStack right = new StateStack(
                null,
                RuleId.Of(RuleIdSingleDepth),
                EnterPosition,
                AnchorPosition,
                false,
                "someEndRule",
                CreateTestScopeStack(),
                CreateTestScopeStack());

            // Act & Assert
            Assert.IsFalse(left.Equals(right));
        }

        [Test]
        public void Equals_NullSentinelToItself_ReturnsTrue()
        {
            // Arrange
            StateStack stack = StateStack.NULL;

            // Act
            bool result = stack.Equals((object)stack);

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public void Equals_DifferentParentChains_ReturnsFalse()
        {
            // Arrange - same leaf but different parent structure
            StateStack parentA = new StateStack(
                null,
                RuleId.Of(1),
                EnterPosition,
                AnchorPosition,
                false,
                null,
                CreateTestScopeStack(),
                CreateTestScopeStack());

            StateStack parentB = new StateStack(
                null,
                RuleId.Of(2),
                EnterPosition,
                AnchorPosition,
                false,
                null,
                CreateTestScopeStack(),
                CreateTestScopeStack());

            StateStack left = parentA.Push(
                RuleId.Of(RuleIdSingleDepth),
                EnterPosition,
                AnchorPosition,
                false,
                null,
                CreateTestScopeStack(),
                CreateTestScopeStack());

            StateStack right = parentB.Push(
                RuleId.Of(RuleIdSingleDepth),
                EnterPosition,
                AnchorPosition,
                false,
                null,
                CreateTestScopeStack(),
                CreateTestScopeStack());

            // Act & Assert
            Assert.IsFalse(left.Equals(right));
        }

        [Test]
        public void Equals_EquivalentDeepStacks_ReturnsTrue()
        {
            // Arrange
            const int depth = 250;
            StateStack left = CreateStateStackWithDepth(depth);
            StateStack right = CreateStateStackWithDepth(depth);

            // Act & Assert
            Assert.IsTrue(left.Equals(right));
        }

        [Test]
        public void Equals_DifferentEnterPos_StillReturnsTrue()
        {
            // Arrange - _enterPos is NOT part of structural equality (matches upstream)
            StateStack left = new StateStack(
                null,
                RuleId.Of(RuleIdSingleDepth),
                0,
                AnchorPosition,
                false,
                null,
                CreateTestScopeStack(),
                CreateTestScopeStack());

            StateStack right = new StateStack(
                null,
                RuleId.Of(RuleIdSingleDepth),
                999,
                AnchorPosition,
                false,
                null,
                CreateTestScopeStack(),
                CreateTestScopeStack());

            // Act & Assert
            Assert.IsTrue(left.Equals(right));
        }

        #endregion Equals (object) tests

        #region IEquatable<StateStack> tests

        [Test]
        public void IEquatable_Equals_IsReflexive()
        {
            // Arrange
            StateStack stack = new StateStack(
                null,
                RuleId.Of(RuleIdSingleDepth),
                EnterPosition,
                AnchorPosition,
                false,
                null,
                CreateTestScopeStack(),
                CreateTestScopeStack());

            // Act
            bool result = stack.Equals(stack);

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public void IEquatable_IsSymmetric()
        {
            // Arrange
            AttributedScopeStack scopeStack = CreateTestScopeStack();
            StateStack left = new StateStack(
                null,
                RuleId.Of(RuleIdSingleDepth),
                EnterPosition,
                AnchorPosition,
                false,
                null,
                scopeStack,
                scopeStack);
            StateStack right = new StateStack(
                null,
                RuleId.Of(RuleIdSingleDepth),
                EnterPosition,
                AnchorPosition,
                false,
                null,
                scopeStack,
                scopeStack);

            // Act
            bool leftEqualsRight = left.Equals(right);
            bool rightEqualsLeft = right.Equals(left);

            // Assert
            Assert.IsTrue(leftEqualsRight);
            Assert.IsTrue(rightEqualsLeft);
        }

        [Test]
        public void IEquatable_Equals_StructurallyEqualStacks_ReturnsTrue()
        {
            // Arrange
            AttributedScopeStack scopeStack = CreateTestScopeStack();

            StateStack left = new StateStack(
                null,
                RuleId.Of(RuleIdSingleDepth),
                EnterPosition,
                AnchorPosition,
                false,
                null,
                scopeStack,
                scopeStack);

            StateStack right = new StateStack(
                null,
                RuleId.Of(RuleIdSingleDepth),
                EnterPosition,
                AnchorPosition,
                false,
                null,
                scopeStack,
                scopeStack);

            // Act - calls IEquatable<StateStack>.Equals directly
            bool result = left.Equals(right);

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public void IEquatable_Equals_Null_ReturnsFalse()
        {
            // Arrange
            StateStack stack = new StateStack(
                null,
                RuleId.Of(RuleIdSingleDepth),
                EnterPosition,
                AnchorPosition,
                false,
                null,
                CreateTestScopeStack(),
                CreateTestScopeStack());

            // Act
            bool result = stack.Equals((StateStack)null);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void IEquatable_Equals_DifferentStack_ReturnsFalse()
        {
            // Arrange
            StateStack left = new StateStack(
                null,
                RuleId.Of(1),
                EnterPosition,
                AnchorPosition,
                false,
                null,
                CreateTestScopeStack(),
                CreateTestScopeStack());

            StateStack right = new StateStack(
                null,
                RuleId.Of(2),
                EnterPosition,
                AnchorPosition,
                false,
                null,
                CreateTestScopeStack(),
                CreateTestScopeStack());

            // Act
            bool result = left.Equals(right);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void IEquatable_Equals_UsedByEqualityComparerDefault()
        {
            // Arrange
            AttributedScopeStack scopeStack = CreateTestScopeStack();

            StateStack key1 = new StateStack(
                null,
                RuleId.Of(RuleIdSingleDepth),
                EnterPosition,
                AnchorPosition,
                false,
                null,
                scopeStack,
                scopeStack);

            StateStack key2 = new StateStack(
                null,
                RuleId.Of(RuleIdSingleDepth),
                EnterPosition,
                AnchorPosition,
                false,
                null,
                scopeStack,
                scopeStack);

            EqualityComparer<StateStack> comparer =
                EqualityComparer<StateStack>.Default;

            // Act & Assert
            Assert.IsTrue(comparer.Equals(key1, key2));
            Assert.AreEqual(comparer.GetHashCode(key1), comparer.GetHashCode(key2));
        }

        #endregion IEquatable<StateStack> tests

        #region Operator == and != tests

        [Test]
        public void OperatorEquals_SameInstance_ReturnsTrue()
        {
            // Arrange
            StateStack stack = new StateStack(
                null,
                RuleId.Of(RuleIdSingleDepth),
                EnterPosition,
                AnchorPosition,
                false,
                null,
                CreateTestScopeStack(),
                CreateTestScopeStack());

            // Act & Assert
#pragma warning disable CS1718 // Comparison made to same variable
            Assert.IsTrue(stack == stack);
            Assert.IsFalse(stack != stack);
#pragma warning restore CS1718 // Comparison made to same variable
        }

        [Test]
        public void OperatorEquals_StructurallyEqualStacks_ReturnsTrue()
        {
            // Arrange
            AttributedScopeStack scopeStack = CreateTestScopeStack();

            StateStack left = new StateStack(
                null,
                RuleId.Of(RuleIdSingleDepth),
                EnterPosition,
                AnchorPosition,
                false,
                null,
                scopeStack,
                scopeStack);

            StateStack right = new StateStack(
                null,
                RuleId.Of(RuleIdSingleDepth),
                EnterPosition,
                AnchorPosition,
                false,
                null,
                scopeStack,
                scopeStack);

            // Act & Assert
            Assert.IsTrue(left == right);
            Assert.IsFalse(left != right);
        }

        [Test]
        public void OperatorEquals_DifferentStacks_ReturnsFalse()
        {
            // Arrange
            StateStack left = new StateStack(
                null,
                RuleId.Of(1),
                EnterPosition,
                AnchorPosition,
                false,
                null,
                CreateTestScopeStack(),
                CreateTestScopeStack());

            StateStack right = new StateStack(
                null,
                RuleId.Of(2),
                EnterPosition,
                AnchorPosition,
                false,
                null,
                CreateTestScopeStack(),
                CreateTestScopeStack());

            // Act & Assert
            Assert.IsFalse(left == right);
            Assert.IsTrue(left != right);
        }

        [Test]
        public void OperatorEquals_BothNull_ReturnsTrue()
        {
            // Arrange
            StateStack left = null;
            StateStack right = null;

            // Act & Assert
            Assert.IsTrue(left == right);
            Assert.IsFalse(left != right);
        }

        [Test]
        public void OperatorEquals_LeftNull_ReturnsFalse()
        {
            // Arrange
            StateStack left = null;
            StateStack right = new StateStack(
                null,
                RuleId.Of(RuleIdSingleDepth),
                EnterPosition,
                AnchorPosition,
                false,
                null,
                CreateTestScopeStack(),
                CreateTestScopeStack());

            // Act & Assert
            Assert.IsFalse(left == right);
            Assert.IsTrue(left != right);
        }

        [Test]
        public void OperatorEquals_RightNull_ReturnsFalse()
        {
            // Arrange
            StateStack left = new StateStack(
                null,
                RuleId.Of(RuleIdSingleDepth),
                EnterPosition,
                AnchorPosition,
                false,
                null,
                CreateTestScopeStack(),
                CreateTestScopeStack());
            StateStack right = null;

            // Act & Assert
            Assert.IsFalse(left == right);
            Assert.IsTrue(left != right);
        }

        [Test]
        public void OperatorEquals_IsReflexive()
        {
            // Arrange
            StateStack stack = new StateStack(
                null,
                RuleId.Of(RuleIdSingleDepth),
                EnterPosition,
                AnchorPosition,
                false,
                null,
                CreateTestScopeStack(),
                CreateTestScopeStack());

            // Act & Assert
#pragma warning disable CS1718 // Comparison made to same variable
            Assert.IsTrue(stack == stack);
#pragma warning restore CS1718 // Comparison made to same variable
        }

        [Test]
        public void OperatorEquals_IsSymmetric()
        {
            // Arrange
            AttributedScopeStack scopeStack = CreateTestScopeStack();
            StateStack left = new StateStack(
                null,
                RuleId.Of(RuleIdSingleDepth),
                EnterPosition,
                AnchorPosition,
                false,
                null,
                scopeStack,
                scopeStack);
            StateStack right = new StateStack(
                null,
                RuleId.Of(RuleIdSingleDepth),
                EnterPosition,
                AnchorPosition,
                false,
                null,
                scopeStack,
                scopeStack);

            // Act & Assert
            Assert.IsTrue(left == right);
            Assert.IsTrue(right == left);
        }

        [Test]
        public void OperatorNotEquals_IsSymmetric()
        {
            // Arrange
            AttributedScopeStack scopeStack = CreateTestScopeStack();
            StateStack left = new StateStack(
                null,
                RuleId.Of(RuleIdSingleDepth),
                EnterPosition,
                AnchorPosition,
                false,
                null,
                scopeStack,
                scopeStack);
            StateStack right = new StateStack(
                null,
                RuleId.Of(RuleIdSingleDepth),
                EnterPosition,
                AnchorPosition,
                false,
                null,
                scopeStack,
                scopeStack);

            // Act & Assert
            Assert.IsFalse(left != right);
            Assert.IsFalse(right != left);
        }

        #endregion Operator == and != tests

        #region HasSameRuleAs tests

        [Test]
        public void HasSameRuleAs_ThrowsArgumentNullException_WhenOtherIsNull()
        {
            // Arrange
            StateStack stack = new StateStack(
                null,
                RuleId.Of(RuleIdSingleDepth),
                EnterPosition,
                AnchorPosition,
                false,
                null,
                CreateTestScopeStack(),
                CreateTestScopeStack());

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => stack.HasSameRuleAs(null));
        }

        [Test]
        public void HasSameRuleAs_SameRuleAtTop_ReturnsTrue()
        {
            // Arrange - share the RuleId instance to match production behavior,
            // where the grammar engine reuses the same RuleId object
            RuleId sharedRuleId = RuleId.Of(RuleIdSingleDepth);

            StateStack left = new StateStack(
                null,
                sharedRuleId,
                EnterPosition,
                AnchorPosition,
                false,
                null,
                CreateTestScopeStack(),
                CreateTestScopeStack());

            StateStack right = new StateStack(
                null,
                sharedRuleId,
                EnterPosition,
                AnchorPosition,
                false,
                null,
                CreateTestScopeStack(),
                CreateTestScopeStack());

            // Act & Assert
            Assert.IsTrue(left.HasSameRuleAs(right));
        }

        [Test]
        public void HasSameRuleAs_DifferentRuleAtTop_DifferentEnterPos_ReturnsFalse()
        {
            // Arrange - different enterPos stops the parent walk immediately
            StateStack left = new StateStack(
                null,
                RuleId.Of(1),
                10,
                AnchorPosition,
                false,
                null,
                CreateTestScopeStack(),
                CreateTestScopeStack());

            StateStack right = new StateStack(
                null,
                RuleId.Of(2),
                20,
                AnchorPosition,
                false,
                null,
                CreateTestScopeStack(),
                CreateTestScopeStack());

            // Act & Assert
            Assert.IsFalse(left.HasSameRuleAs(right));
        }

        [Test]
        public void HasSameRuleAs_MatchingAncestorRule_WithSameEnterPos_ReturnsTrue()
        {
            // Arrange - the matching rule is in the parent, not the top
            // Share the RuleId instance to match production behavior,
            // where the grammar engine reuses the same RuleId object
            RuleId sharedRuleIdOfSingleDepth = RuleId.Of(RuleIdSingleDepth);
            const int sharedEnterPos = 5;

            StateStack grandparent = new StateStack(
                null,
                sharedRuleIdOfSingleDepth,
                sharedEnterPos,
                AnchorPosition,
                false,
                null,
                CreateTestScopeStack(),
                CreateTestScopeStack());

            StateStack parent = grandparent.Push(
                RuleId.Of(RuleIdDepthTwo),
                sharedEnterPos,
                AnchorPosition,
                false,
                null,
                CreateTestScopeStack(),
                CreateTestScopeStack());

            StateStack other = new StateStack(
                null,
                sharedRuleIdOfSingleDepth,
                sharedEnterPos,
                AnchorPosition,
                false,
                null,
                CreateTestScopeStack(),
                CreateTestScopeStack());

            // Act - parent's top rule (100) != other's rule (42), but parent's
            //       grandparent rule (42) == other's rule (42), and enterPos matches
            bool result = parent.HasSameRuleAs(other);

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public void HasSameRuleAs_AncestorHasMatchingRule_ButDifferentEnterPos_StopsWalking()
        {
            // Arrange - ancestor has matching rule but different enterPos
            StateStack grandparent = new StateStack(
                null,
                RuleId.Of(RuleIdSingleDepth),
                10,
                AnchorPosition,
                false,
                null,
                CreateTestScopeStack(),
                CreateTestScopeStack());

            StateStack parent = grandparent.Push(
                RuleId.Of(RuleIdDepthTwo),
                5,
                AnchorPosition,
                false,
                null,
                CreateTestScopeStack(),
                CreateTestScopeStack());

            StateStack other = new StateStack(
                null,
                RuleId.Of(RuleIdSingleDepth),
                5,
                AnchorPosition,
                false,
                null,
                CreateTestScopeStack(),
                CreateTestScopeStack());

            // Act - parent's top (enterPos=5) matches other's enterPos (5), but rule differs (100 vs 42).
            //       grandparent's enterPos (10) != other's enterPos (5), so walk stops before checking.
            bool result = parent.HasSameRuleAs(other);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void HasSameRuleAs_NullParent_StopsWalkingGracefully()
        {
            // Arrange - single-node stack, no parent to walk to
            StateStack left = new StateStack(
                null,
                RuleId.Of(1),
                EnterPosition,
                AnchorPosition,
                false,
                null,
                CreateTestScopeStack(),
                CreateTestScopeStack());

            StateStack right = new StateStack(
                null,
                RuleId.Of(2),
                EnterPosition,
                AnchorPosition,
                false,
                null,
                CreateTestScopeStack(),
                CreateTestScopeStack());

            // Act & Assert - walks to null parent, returns false
            Assert.IsFalse(left.HasSameRuleAs(right));
        }

        #endregion HasSameRuleAs tests

        #region WithContentNameScopesList tests

        [Test]
        public void WithContentNameScopesList_SameValue_ReturnsSameInstance()
        {
            // Arrange
            AttributedScopeStack scopeStack = CreateTestScopeStack();

            StateStack stack = new StateStack(
                StateStack.NULL,
                RuleId.Of(RuleIdSingleDepth),
                EnterPosition,
                AnchorPosition,
                false,
                null,
                scopeStack,
                scopeStack);

            // Act
            StateStack result = stack.WithContentNameScopesList(scopeStack);

            // Assert
            Assert.AreSame(stack, result);
        }

        [Test]
        public void WithContentNameScopesList_DifferentValue_ReturnsNewInstance()
        {
            // Arrange
            AttributedScopeStack original = new AttributedScopeStack(null, "original", 1);
            AttributedScopeStack replacement = new AttributedScopeStack(null, "replacement", 2);

            StateStack stack = new StateStack(
                StateStack.NULL,
                RuleId.Of(RuleIdSingleDepth),
                EnterPosition,
                AnchorPosition,
                false,
                null,
                CreateTestScopeStack(),
                original);

            // Act
            StateStack result = stack.WithContentNameScopesList(replacement);

            // Assert
            Assert.AreNotSame(stack, result);
            Assert.AreSame(replacement, result.ContentNameScopesList);
        }

        [Test]
        public void WithContentNameScopesList_NullOnBothSides_ReturnsSameInstance()
        {
            // Arrange - previously would throw NullReferenceException
            StateStack stack = new StateStack(
                StateStack.NULL,
                RuleId.Of(RuleIdSingleDepth),
                EnterPosition,
                AnchorPosition,
                false,
                null,
                CreateTestScopeStack(),
                null);

            // Act
            StateStack result = stack.WithContentNameScopesList(null);

            // Assert
            Assert.AreSame(stack, result);
        }

        [Test]
        public void WithContentNameScopesList_NullToNonNull_ReturnsNewInstance()
        {
            // Arrange
            AttributedScopeStack replacement = new AttributedScopeStack(null, "new", 1);

            StateStack stack = new StateStack(
                StateStack.NULL,
                RuleId.Of(RuleIdSingleDepth),
                EnterPosition,
                AnchorPosition,
                false,
                null,
                CreateTestScopeStack(),
                null);

            // Act
            StateStack result = stack.WithContentNameScopesList(replacement);

            // Assert
            Assert.AreNotSame(stack, result);
            Assert.AreSame(replacement, result.ContentNameScopesList);
        }

        #endregion WithContentNameScopesList tests

        #region WithEndRule tests

        [Test]
        public void WithEndRule_SameValue_ReturnsSameInstance()
        {
            // Arrange
            StateStack stack = new StateStack(
                null,
                RuleId.Of(RuleIdSingleDepth),
                EnterPosition,
                AnchorPosition,
                false,
                "endRule",
                CreateTestScopeStack(),
                CreateTestScopeStack());

            // Act
            StateStack result = stack.WithEndRule("endRule");

            // Assert
            Assert.AreSame(stack, result);
        }

        [Test]
        public void WithEndRule_DifferentValue_ReturnsNewInstance()
        {
            // Arrange
            StateStack stack = new StateStack(
                null,
                RuleId.Of(RuleIdSingleDepth),
                EnterPosition,
                AnchorPosition,
                false,
                "endRuleA",
                CreateTestScopeStack(),
                CreateTestScopeStack());

            // Act
            StateStack result = stack.WithEndRule("endRuleB");

            // Assert
            Assert.AreNotSame(stack, result);
            Assert.AreEqual("endRuleB", result.EndRule);
        }

        [Test]
        public void WithEndRule_NullToNonNull_ReturnsNewInstance()
        {
            // Arrange
            StateStack stack = new StateStack(
                null,
                RuleId.Of(RuleIdSingleDepth),
                EnterPosition,
                AnchorPosition,
                false,
                null,
                CreateTestScopeStack(),
                CreateTestScopeStack());

            // Act
            StateStack result = stack.WithEndRule("newEndRule");

            // Assert
            Assert.AreNotSame(stack, result);
            Assert.AreEqual("newEndRule", result.EndRule);
        }

        [Test]
        public void WithEndRule_NullToBothNull_ReturnsNewInstance_MatchesUpstreamBehavior()
        {
            // Arrange - note: upstream Java also returns a new instance when both are null,
            // because the guard only fires when this.EndRule != null
            StateStack stack = new StateStack(
                null,
                RuleId.Of(RuleIdSingleDepth),
                EnterPosition,
                AnchorPosition,
                false,
                null,
                CreateTestScopeStack(),
                CreateTestScopeStack());

            // Act
            StateStack result = stack.WithEndRule(null);

            // Assert - upstream returns new instance in this case
            Assert.AreNotSame(stack, result);
        }

        #endregion WithEndRule tests

        #region Pop and SafePop tests

        [Test]
        public void Pop_ReturnsParent()
        {
            // Arrange
            StateStack parent = new StateStack(
                null,
                RuleId.Of(RuleIdSingleDepth),
                EnterPosition,
                AnchorPosition,
                false,
                null,
                CreateTestScopeStack(),
                CreateTestScopeStack());

            StateStack child = parent.Push(
                RuleId.Of(RuleIdDepthTwo),
                EnterPosition,
                AnchorPosition,
                false,
                null,
                CreateTestScopeStack(),
                CreateTestScopeStack());

            // Act
            StateStack result = child.Pop();

            // Assert
            Assert.AreSame(parent, result);
        }

        [Test]
        public void Pop_RootReturnsNull()
        {
            // Arrange
            StateStack root = new StateStack(
                null,
                RuleId.Of(RuleIdSingleDepth),
                EnterPosition,
                AnchorPosition,
                false,
                null,
                CreateTestScopeStack(),
                CreateTestScopeStack());

            // Act
            StateStack result = root.Pop();

            // Assert
            Assert.IsNull(result);
        }

        [Test]
        public void SafePop_RootReturnsSelf()
        {
            // Arrange
            StateStack root = new StateStack(
                null,
                RuleId.Of(RuleIdSingleDepth),
                EnterPosition,
                AnchorPosition,
                false,
                null,
                CreateTestScopeStack(),
                CreateTestScopeStack());

            // Act
            StateStack result = root.SafePop();

            // Assert
            Assert.AreSame(root, result);
        }

        [Test]
        public void SafePop_NonRootReturnsParent()
        {
            // Arrange
            StateStack parent = new StateStack(
                null,
                RuleId.Of(RuleIdSingleDepth),
                EnterPosition,
                AnchorPosition,
                false,
                null,
                CreateTestScopeStack(),
                CreateTestScopeStack());

            StateStack child = parent.Push(
                RuleId.Of(RuleIdDepthTwo),
                EnterPosition,
                AnchorPosition,
                false,
                null,
                CreateTestScopeStack(),
                CreateTestScopeStack());

            // Act
            StateStack result = child.SafePop();

            // Assert
            Assert.AreSame(parent, result);
        }

        #endregion Pop and SafePop tests

        #region Reset tests

        [Test]
        public void Reset_ClearsEnterPosAndAnchorPos()
        {
            // Arrange
            const int initialEnterPos = 10;
            const int initialAnchorPos = 20;

            StateStack stack = new StateStack(
                null,
                RuleId.Of(RuleIdSingleDepth),
                initialEnterPos,
                initialAnchorPos,
                false,
                null,
                CreateTestScopeStack(),
                CreateTestScopeStack());

            // Act
            stack.Reset();

            // Assert
            Assert.AreEqual(-1, stack.GetEnterPos());
            Assert.AreEqual(-1, stack.GetAnchorPos());
        }

        [Test]
        public void Reset_ResetsEntireParentChain()
        {
            // Arrange
            StateStack parent = new StateStack(
                null,
                RuleId.Of(RuleIdSingleDepth),
                10,
                20,
                false,
                null,
                CreateTestScopeStack(),
                CreateTestScopeStack());

            StateStack child = parent.Push(
                RuleId.Of(RuleIdDepthTwo),
                30,
                40,
                false,
                null,
                CreateTestScopeStack(),
                CreateTestScopeStack());

            // Act
            child.Reset();

            // Assert
            Assert.AreEqual(-1, child.GetEnterPos());
            Assert.AreEqual(-1, child.GetAnchorPos());
            Assert.AreEqual(-1, parent.GetEnterPos());
            Assert.AreEqual(-1, parent.GetAnchorPos());
        }

        #endregion Reset tests

        #region Helper Methods

        private static AttributedScopeStack CreateTestScopeStack()
        {
            return new AttributedScopeStack(null, "test.scope", 0);
        }

        private static StateStack CreateStateStackWithDepth(int depth)
        {
            StateStack stack = StateStack.NULL;
            const int ruleIdDepthMultiplier = 100;
            for (int depthIndex = 0; depthIndex < depth; depthIndex++)
            {
                int ruleId = depthIndex * ruleIdDepthMultiplier;
                stack = stack.Push(
                    RuleId.Of(ruleId),
                    EnterPosition,
                    AnchorPosition,
                    false,
                    null,
                    CreateTestScopeStack(),
                    CreateTestScopeStack());
            }

            return stack;
        }

        #endregion
    }
}
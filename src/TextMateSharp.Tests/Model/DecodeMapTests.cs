using NUnit.Framework;

using System.Collections.Generic;

using TextMateSharp.Model;

namespace TextMateSharp.Tests.Model
{
    [TestFixture]
    internal class DecodeMapTests
    {
        [Test]
        public void DecodeMap_Should_Initialize_PrevToken()
        {
            // arrange
            DecodeMap decodeMap = new DecodeMap();

            // act
            TMTokenDecodeData prevToken = decodeMap.PrevToken;

            // assert
            Assert.IsNotNull(prevToken);
        }

        [Test]
        public void DecodeMap_GetToken_Should_Return_Empty_String_When_No_Tokens_Were_Assigned()
        {
            // arrange
            DecodeMap decodeMap = new DecodeMap();
            Dictionary<int, bool> tokenMap = new Dictionary<int, bool>();

            // act
            string token = decodeMap.GetToken(tokenMap);

            // assert
            Assert.AreEqual(string.Empty, token);
        }

        [Test]
        public void DecodeMap_GetToken_Should_Return_Empty_String_When_TokenMap_Is_Empty_Even_After_Assigning_Tokens()
        {
            // arrange
            DecodeMap decodeMap = new DecodeMap();
            decodeMap.getTokenIds("a.b.c");
            Dictionary<int, bool> tokenMap = new Dictionary<int, bool>();

            // act
            string token = decodeMap.GetToken(tokenMap);

            // assert
            Assert.AreEqual(string.Empty, token);
        }

        [Test]
        public void DecodeMap_getTokenIds_Should_Return_Stable_TokenIds_For_Same_Scope()
        {
            // arrange
            DecodeMap decodeMap = new DecodeMap();

            // act
            int[] ids1 = decodeMap.getTokenIds("source.cs");
            int[] ids2 = decodeMap.getTokenIds("source.cs");

            // assert
            Assert.AreEqual(ids1.Length, ids2.Length);
            CollectionAssert.AreEqual(ids1, ids2);
        }

        [Test]
        public void DecodeMap_getTokenIds_Should_Assign_And_Reuse_TokenIds_Across_Scopes()
        {
            // arrange
            DecodeMap decodeMap = new DecodeMap();

            // act
            int[] idsAbc = decodeMap.getTokenIds("a.b.c");
            int[] idsA = decodeMap.getTokenIds("a");
            int[] idsB = decodeMap.getTokenIds("b");
            int[] idsC = decodeMap.getTokenIds("c");

            // assert
            Assert.AreEqual(3, idsAbc.Length);

            Assert.AreEqual(1, idsA.Length);
            Assert.AreEqual(1, idsB.Length);
            Assert.AreEqual(1, idsC.Length);

            Assert.AreEqual(idsAbc[0], idsA[0]);
            Assert.AreEqual(idsAbc[1], idsB[0]);
            Assert.AreEqual(idsAbc[2], idsC[0]);
        }

        [Test]
        public void DecodeMap_GetToken_Should_Return_Selected_Tokens_Joined_With_Dots()
        {
            // arrange
            DecodeMap decodeMap = new DecodeMap();
            int[] idsAbc = decodeMap.getTokenIds("a.b.c");

            Dictionary<int, bool> tokenMap = new Dictionary<int, bool>
            {
                [idsAbc[0]] = true, // a
                [idsAbc[2]] = true // c
            };

            // act
            string token = decodeMap.GetToken(tokenMap);

            // assert
            Assert.AreEqual("a.c", token);
        }

        [Test]
        public void DecodeMap_GetToken_Should_Return_Tokens_In_AssignedId_Order()
        {
            // arrange
            DecodeMap decodeMap = new DecodeMap();

            // First assignment order matters because IDs are allocated incrementally.
            int[] ids = decodeMap.getTokenIds("c.a"); // c => id1, a => id2

            Dictionary<int, bool> tokenMap = new Dictionary<int, bool>
            {
                [ids[1]] = true, // a (higher id)
                [ids[0]] = true // c (lower id)
            };

            // act
            string token = decodeMap.GetToken(tokenMap);

            // assert
            Assert.AreEqual("c.a", token);
        }

        [Test]
        public void DecodeMap_GetToken_Should_Ignore_Keys_Outside_AssignedId_Range()
        {
            // arrange
            DecodeMap decodeMap = new DecodeMap();
            int[] idsAbc = decodeMap.getTokenIds("a.b.c");

            Dictionary<int, bool> tokenMap1 = new Dictionary<int, bool>
            {
                [idsAbc[0]] = true,
                [idsAbc[2]] = true
            };

            Dictionary<int, bool> tokenMap2 = new Dictionary<int, bool>
            {
                [idsAbc[0]] = true,
                [idsAbc[2]] = true,
                [idsAbc[2] + 1000] = true
            };

            // act
            string token1 = decodeMap.GetToken(tokenMap1);
            string token2 = decodeMap.GetToken(tokenMap2);

            // assert
            Assert.AreEqual(token1, token2);
            Assert.AreEqual("a.c", token2);
        }

        [Test]
        public void DecodeMap_getTokenIds_Should_Handle_Empty_Segments_And_RoundTrip_Via_GetToken()
        {
            // arrange
            DecodeMap decodeMap = new DecodeMap();

            // act
            int[] ids = decodeMap.getTokenIds("a..b");

            Dictionary<int, bool> tokenMap = new Dictionary<int, bool>
            {
                [ids[0]] = true,
                [ids[1]] = true, // empty segment token
                [ids[2]] = true
            };

            string token = decodeMap.GetToken(tokenMap);

            // assert
            Assert.AreEqual(3, ids.Length);
            Assert.AreEqual("a..b", token);
        }

        [Test]
        public void DecodeMap_getTokenIds_Should_Reuse_Cached_IntArray_For_Identical_Scope()
        {
            // arrange
            DecodeMap decodeMap = new DecodeMap();

            // act
            int[] ids1 = decodeMap.getTokenIds("a.b.c");
            int[] ids2 = decodeMap.getTokenIds("a.b.c");

            // assert
            Assert.AreSame(ids1, ids2);
            Assert.AreEqual(ids1.Length, ids2.Length);
        }

        [Test]
        public void DecodeMap_getTokenIds_Should_Reuse_TokenIds_When_Scope_Is_Extended()
        {
            // arrange
            DecodeMap decodeMap = new DecodeMap();

            // act
            int[] idsAbc = decodeMap.getTokenIds("a.b.c");
            int[] idsAbcd = decodeMap.getTokenIds("a.b.c.d");

            // assert
            Assert.AreEqual(3, idsAbc.Length);
            Assert.AreEqual(4, idsAbcd.Length);

            Assert.AreEqual(idsAbc[0], idsAbcd[0]);
            Assert.AreEqual(idsAbc[1], idsAbcd[1]);
            Assert.AreEqual(idsAbc[2], idsAbcd[2]);

            Assert.AreNotEqual(idsAbc[0], idsAbcd[3]);
            Assert.AreNotEqual(idsAbc[1], idsAbcd[3]);
            Assert.AreNotEqual(idsAbc[2], idsAbcd[3]);
        }

        [Test]
        public void DecodeMap_getTokenIds_Should_Handle_Empty_Scope_And_RoundTrip()
        {
            // arrange
            DecodeMap decodeMap = new DecodeMap();

            // act
            int[] ids = decodeMap.getTokenIds(string.Empty);

            Dictionary<int, bool> tokenMap = new Dictionary<int, bool>
            {
                [ids[0]] = true
            };

            string token = decodeMap.GetToken(tokenMap);

            // assert
            Assert.AreEqual(1, ids.Length);
            Assert.AreEqual(string.Empty, token);
        }

        [Test]
        public void DecodeMap_getTokenIds_Should_Handle_Trailing_Separator_And_RoundTrip()
        {
            // arrange
            DecodeMap decodeMap = new DecodeMap();

            // act
            int[] ids = decodeMap.getTokenIds("a.");

            Dictionary<int, bool> tokenMap = new Dictionary<int, bool>
            {
                [ids[0]] = true,
                [ids[1]] = true
            };

            string token = decodeMap.GetToken(tokenMap);

            // assert
            Assert.AreEqual(2, ids.Length);
            Assert.AreEqual("a.", token);
        }

        [Test]
        public void DecodeMap_getTokenIds_Should_Handle_Leading_Separator_And_RoundTrip()
        {
            // arrange
            DecodeMap decodeMap = new DecodeMap();

            // act
            int[] ids = decodeMap.getTokenIds(".a");

            Dictionary<int, bool> tokenMap = new Dictionary<int, bool>
            {
                [ids[0]] = true,
                [ids[1]] = true
            };

            string token = decodeMap.GetToken(tokenMap);

            // assert
            Assert.AreEqual(2, ids.Length);
            Assert.AreEqual(".a", token);
        }

        [Test]
        public void DecodeMap_GetToken_Should_Ignore_False_Values_In_TokenMap()
        {
            // arrange
            DecodeMap decodeMap = new DecodeMap();

            int[] ids = decodeMap.getTokenIds("a.b.c");

            Dictionary<int, bool> tokenMap = new Dictionary<int, bool>
            {
                [ids[0]] = true,
                [ids[1]] = false,
                [ids[2]] = true
            };

            // act
            string token = decodeMap.GetToken(tokenMap);

            // assert
            Assert.AreEqual("a.c", token);
        }
    }
}

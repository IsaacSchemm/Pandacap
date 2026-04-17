namespace Pandacap.ATProto.Models.Tests
{
    [TestClass]
    public sealed class ATProtoRefUriTests
    {
        [TestMethod]
        public void Components_DIDOnly()
        {
            ATProtoRefUri refUri = new("at://did:comptest1");
            Assert.AreEqual("did:comptest1", refUri.Components.DID);
            Assert.IsNull(refUri.Components.Collection);
            Assert.IsNull(refUri.Components.RecordKey);
        }

        [TestMethod]
        public void Components_CollectionOnly()
        {
            ATProtoRefUri refUri = new("at://did:comptest2/com.whtwnd.blog.entry");
            Assert.AreEqual("did:comptest2", refUri.Components.DID);
            Assert.AreEqual("com.whtwnd.blog.entry", refUri.Components.Collection);
            Assert.IsNull(refUri.Components.RecordKey);
        }

        [TestMethod]
        public void Components_BlueskyPost()
        {
            ATProtoRefUri refUri = new("at://did:abcdefg/app.bsky.feed.post/12345");
            Assert.AreEqual("did:abcdefg", refUri.Components.DID);
            Assert.AreEqual("app.bsky.feed.post", refUri.Components.Collection);
            Assert.AreEqual("12345", refUri.Components.RecordKey);
        }

        [TestMethod]
        public void Components_BlueskyProfile()
        {
            ATProtoRefUri refUri = new("at://did:qwertyuiop/app.bsky.actor.profile/self");
            Assert.AreEqual("did:qwertyuiop", refUri.Components.DID);
            Assert.AreEqual("app.bsky.actor.profile", refUri.Components.Collection);
            Assert.AreEqual("self", refUri.Components.RecordKey);
        }

        [TestMethod]
        public void Components_NonATProtoUri()
        {
            ATProtoRefUri refUri = new("https://www.example.com/app.bsky.actor.profile/self");
            Assert.IsNull(refUri.Components.DID);
            Assert.IsNull(refUri.Components.Collection);
            Assert.IsNull(refUri.Components.RecordKey);
        }

        [TestMethod]
        public void Components_Null()
        {
            ATProtoRefUri refUri = new(null);
            Assert.IsNull(refUri.Components.DID);
            Assert.IsNull(refUri.Components.Collection);
            Assert.IsNull(refUri.Components.RecordKey);
        }
    }
}

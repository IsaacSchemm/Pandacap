using JsonLD.Core;
using Moq;
using Newtonsoft.Json.Linq;
using Pandacap.ActivityPub.JsonLd.Interfaces;
using Pandacap.ActivityPub.RemoteObjects.Interfaces;
using Pandacap.ActivityPub.RemoteObjects.Models;
using Pandacap.ActivityPub.Services.Interfaces;

namespace Pandacap.ActivityPub.RemoteObjects.Tests
{
    [TestClass]
    public sealed class ActivityPubRemotePostServiceTests
    {
        [TestMethod]
        public async Task FetchPostAsync_ParsesMastodonPost()
        {
            using var cts = new CancellationTokenSource();
            var cancellationToken = cts.Token;

            var requestHandlerMock = new Mock<IActivityPubRequestHandler>(MockBehavior.Strict);
            requestHandlerMock
                .Setup(handler => handler.GetJsonAsync(
                    new Uri("https://bitbang.social/@ActionRetro/113387877839785016"),
                    cancellationToken))
                .ReturnsAsync(@"{""@context"":[""https://www.w3.org/ns/activitystreams"",{""ostatus"":""http://ostatus.org#"",""atomUri"":""ostatus:atomUri"",""inReplyToAtomUri"":""ostatus:inReplyToAtomUri"",""conversation"":""ostatus:conversation"",""sensitive"":""as:sensitive"",""toot"":""http://joinmastodon.org/ns#"",""votersCount"":""toot:votersCount"",""blurhash"":""toot:blurhash"",""focalPoint"":{""@container"":""@list"",""@id"":""toot:focalPoint""}}],""id"":""https://bitbang.social/users/ActionRetro/statuses/113387877839785016"",""type"":""Note"",""summary"":null,""inReplyTo"":null,""published"":""2024-10-29T00:28:30Z"",""url"":""https://bitbang.social/@ActionRetro/113387877839785016"",""attributedTo"":""https://bitbang.social/users/ActionRetro"",""to"":[""https://www.w3.org/ns/activitystreams#Public""],""cc"":[""https://bitbang.social/users/ActionRetro/followers""],""sensitive"":false,""atomUri"":""https://bitbang.social/users/ActionRetro/statuses/113387877839785016"",""inReplyToAtomUri"":null,""conversation"":""tag:bitbang.social,2024-10-29:objectId=28314788:objectType=Conversation"",""content"":""\u003cp\u003eGuess what I\u0026#39;m trying to make\u003c/p\u003e"",""contentMap"":{""en"":""\u003cp\u003eGuess what I\u0026#39;m trying to make\u003c/p\u003e""},""attachment"":[{""type"":""Document"",""mediaType"":""image/png"",""url"":""https://files.bitbang.social/media_attachments/files/113/387/875/618/736/512/original/ed02f0a54910052b.png"",""name"":""3D mockup of a very long, thin oval"",""blurhash"":""URQc;us:~VWB-ooeNGae^jWBIVkCaeRkW=t7"",""focalPoint"":[0.0,0.0],""width"":1162,""height"":811}],""tag"":[],""replies"":{""id"":""https://bitbang.social/users/ActionRetro/statuses/113387877839785016/replies"",""type"":""Collection"",""first"":{""type"":""CollectionPage"",""next"":""https://bitbang.social/users/ActionRetro/statuses/113387877839785016/replies?only_other_accounts=true\u0026page=true"",""partOf"":""https://bitbang.social/users/ActionRetro/statuses/113387877839785016/replies"",""items"":[]}}}");

            var actor = new RemoteActor(
                type: "https://www.w3.org/ns/activitystreams#Person",
                id: "https://bitbang.social/users/ActionRetro",
                inbox: "",
                sharedInbox: "",
                preferredUsername: "",
                name: "",
                summary: "",
                url: "",
                iconUrl: "",
                keyId: "",
                keyPem: "");

            var followers = RemoteAddressee.NewCollection("https://bitbang.social/users/ActionRetro/followers");

            var addressees = new Dictionary<string, RemoteAddressee>
            {
                ["https://bitbang.social/users/ActionRetro"] = RemoteAddressee.NewActor(actor),
                ["https://bitbang.social/users/ActionRetro/followers"] = followers
            };

            var expectedActor = new RemoteActor(
                type: "https://www.w3.org/ns/activitystreams#Person",
                id: "https://bitbang.social/users/ActionRetro",
                inbox: "",
                sharedInbox: "",
                preferredUsername: "",
                name: "",
                summary: "",
                url: "",
                iconUrl: "",
                keyId: "",
                keyPem: "");

            var service = GetService(
                requestHandlerMock.Object,
                addressees);

            var expectedPost = new RemotePost(
                id: "https://bitbang.social/users/ActionRetro/statuses/113387877839785016",
                attributedTo: actor,
                to: [RemoteAddressee.PublicCollection],
                cc: [followers],
                inReplyTo: [],
                type: "https://www.w3.org/ns/activitystreams#Note",
                postedAt: DateTimeOffset.Parse("2024-10-29T00:28:30Z"),
                sensitive: false,
                name: null,
                summary: null,
                sanitizedContent: "<p>Guess what I'm trying to make</p>",
                url: "https://bitbang.social/@ActionRetro/113387877839785016",
                audience: null,
                attachments: [new RemoteAttachment(
                    mediaType: "image/png",
                    name: "3D mockup of a very long, thin oval",
                    url: "https://files.bitbang.social/media_attachments/files/113/387/875/618/736/512/original/ed02f0a54910052b.png")],
                isBridgyFed: false);

            var result = await service.FetchPostAsync("https://bitbang.social/@ActionRetro/113387877839785016", cancellationToken);
            AssertAreEqual(expectedPost, result);
        }

        [TestMethod]
        public async Task FetchPostAsync_ParsesLemmyPost()
        {
            using var cts = new CancellationTokenSource();
            var cancellationToken = cts.Token;

            var requestHandlerMock = new Mock<IActivityPubRequestHandler>(MockBehavior.Strict);
            requestHandlerMock
                .Setup(handler => handler.GetJsonAsync(
                    new Uri("https://startrek.website/post/37414786"),
                    cancellationToken))
                .ReturnsAsync(@"{
  ""@context"": [
    ""https://join-lemmy.org/context.json"",
    ""https://www.w3.org/ns/activitystreams""
  ],
  ""type"": ""Page"",
  ""id"": ""https://startrek.website/post/37414786"",
  ""attributedTo"": ""https://startrek.website/u/hopesdead"",
  ""to"": [
    ""https://startrek.website/c/startrek"",
    ""https://www.w3.org/ns/activitystreams#Public""
  ],
  ""name"": ""Guess I'm going to school."",
  ""cc"": [],
  ""content"": ""<p><img src=\""https://startrek.website/pictrs/image/8b68f8a1-9179-4af8-81ee-371e74531726.jpeg\"" alt=\""\"" /></p>\n<p>This is the Cadet comadge magnet from Fansets. They only have one Cadet variant, so no other colors. I wanted Engineering. I’ve spoken to the people at Fansets at STLV in the past and they said there is a strict review from CBS Studios.</p>\n"",
  ""mediaType"": ""text/html"",
  ""source"": {
    ""content"": ""![](https://startrek.website/pictrs/image/8b68f8a1-9179-4af8-81ee-371e74531726.jpeg)\n\nThis is the Cadet comadge magnet from Fansets. They only have one Cadet variant, so no other colors. I wanted Engineering. I’ve spoken to the people at Fansets at STLV in the past and they said there is a strict review from CBS Studios."",
    ""mediaType"": ""text/markdown""
  },
  ""attachment"": [
    {
      ""href"": ""https://startrek.website/pictrs/image/7a9091a3-ef3f-4606-8398-73b3a6d83c0a.jpeg"",
      ""mediaType"": null,
      ""type"": ""Link""
    }
  ],
  ""image"": {
    ""type"": ""Image"",
    ""url"": ""https://startrek.website/pictrs/image/a655339b-0d35-4b19-8fc6-a7e9c8e598ea.jpeg""
  },
  ""sensitive"": false,
  ""published"": ""2026-03-28T05:54:27.817022Z"",
  ""updated"": ""2026-03-28T07:25:24.502101Z"",
  ""language"": {
    ""identifier"": ""en"",
    ""name"": ""English""
  },
  ""audience"": ""https://startrek.website/c/startrek"",
  ""tag"": [
    {
      ""href"": ""https://startrek.website/post/37414786"",
      ""name"": ""#startrek"",
      ""type"": ""Hashtag""
    }
  ]
}");

            var actor = new RemoteActor(
                type: "https://www.w3.org/ns/activitystreams#Person",
                id: "https://startrek.website/u/hopesdead",
                inbox: "",
                sharedInbox: "",
                preferredUsername: "",
                name: "",
                summary: "",
                url: "",
                iconUrl: "",
                keyId: "",
                keyPem: "");

            var community = new RemoteActor(
                type: "https://www.w3.org/ns/activitystreams#Group",
                id: "https://startrek.website/c/startrek",
                inbox: "",
                sharedInbox: "",
                preferredUsername: "",
                name: "",
                summary: "",
                url: "",
                iconUrl: "",
                keyId: "",
                keyPem: "");

            var addressees = new Dictionary<string, RemoteAddressee>
            {
                [actor.Id] = RemoteAddressee.NewActor(actor),
                [community.Id] = RemoteAddressee.NewActor(community)
            };

            var service = GetService(
                requestHandlerMock.Object,
                addressees);

            var result = await service.FetchPostAsync("https://startrek.website/post/37414786", cancellationToken);

            AssertAreEqual(
                new RemotePost(
                    id: "https://startrek.website/post/37414786",
                    attributedTo: actor,
                    to: [RemoteAddressee.NewActor(community), RemoteAddressee.PublicCollection],
                    cc: [],
                    inReplyTo: [],
                    type: "https://www.w3.org/ns/activitystreams#Page",
                    postedAt: DateTimeOffset.Parse("2026-03-28T05:54:27.817022Z"),
                    sensitive: false,
                    name: "Guess I'm going to school.",
                    summary: null,
                    sanitizedContent: "<p><img src=\"https://startrek.website/pictrs/image/8b68f8a1-9179-4af8-81ee-371e74531726.jpeg\" alt=\"\"></p>\n<p>This is the Cadet comadge magnet from Fansets. They only have one Cadet variant, so no other colors. I wanted Engineering. I’ve spoken to the people at Fansets at STLV in the past and they said there is a strict review from CBS Studios.</p>\n",
                    url: null,
                    audience: community.Id,
                    attachments: [new RemoteAttachment(
                        mediaType: null,
                        name: null,
                        url: null)],
                    isBridgyFed: false),
                result);
        }

        [TestMethod]
        public async Task FetchPostAsync_ParsesLemmyReply()
        {
            using var cts = new CancellationTokenSource();
            var cancellationToken = cts.Token;

            var requestHandlerMock = new Mock<IActivityPubRequestHandler>(MockBehavior.Strict);
            requestHandlerMock
                .Setup(handler => handler.GetJsonAsync(
                    new Uri("https://lemmy.world/comment/22911665"),
                    cancellationToken))
                .ReturnsAsync(@"{
  ""@context"": [
    ""https://join-lemmy.org/context.json"",
    ""https://www.w3.org/ns/activitystreams""
  ],
  ""type"": ""Note"",
  ""id"": ""https://lemmy.world/comment/22911665"",
  ""attributedTo"": ""https://lemmy.world/u/alphabites"",
  ""to"": [
    ""https://www.w3.org/ns/activitystreams#Public""
  ],
  ""cc"": [
    ""https://startrek.website/c/startrek"",
    ""https://startrek.website/u/hopesdead""
  ],
  ""content"": ""<p>Do you have to pass an exam to get the non-cadet variants?</p>\n"",
  ""inReplyTo"": ""https://startrek.website/post/37414786"",
  ""mediaType"": ""text/html"",
  ""source"": {
    ""content"": ""Do you have to pass an exam to get the non-cadet variants?"",
    ""mediaType"": ""text/markdown""
  },
  ""published"": ""2026-03-28T06:54:04.633290Z"",
  ""tag"": [
    {
      ""href"": ""https://startrek.website/u/hopesdead"",
      ""name"": ""@hopesdead@startrek.website"",
      ""type"": ""Mention""
    }
  ],
  ""distinguished"": false,
  ""language"": {
    ""identifier"": ""en"",
    ""name"": ""English""
  },
  ""audience"": ""https://startrek.website/c/startrek"",
  ""attachment"": []
}");

            var replyingActor = new RemoteActor(
                type: "https://www.w3.org/ns/activitystreams#Person",
                id: "https://lemmy.world/u/alphabites",
                inbox: "",
                sharedInbox: "",
                preferredUsername: "",
                name: "",
                summary: "",
                url: "",
                iconUrl: "",
                keyId: "",
                keyPem: "");

            var originalActor = new RemoteActor(
                type: "https://www.w3.org/ns/activitystreams#Person",
                id: "https://startrek.website/u/hopesdead",
                inbox: "",
                sharedInbox: "",
                preferredUsername: "",
                name: "",
                summary: "",
                url: "",
                iconUrl: "",
                keyId: "",
                keyPem: "");

            var community = new RemoteActor(
                type: "https://www.w3.org/ns/activitystreams#Group",
                id: "https://startrek.website/c/startrek",
                inbox: "",
                sharedInbox: "",
                preferredUsername: "",
                name: "",
                summary: "",
                url: "",
                iconUrl: "",
                keyId: "",
                keyPem: "");

            var addressees = new Dictionary<string, RemoteAddressee>
            {
                [replyingActor.Id] = RemoteAddressee.NewActor(replyingActor),
                [originalActor.Id] = RemoteAddressee.NewActor(originalActor),
                [community.Id] = RemoteAddressee.NewActor(community)
            };

            var service = GetService(
                requestHandlerMock.Object,
                addressees);

            var result = await service.FetchPostAsync("https://lemmy.world/comment/22911665", cancellationToken);

            AssertAreEqual(
                new RemotePost(
                    id: "https://lemmy.world/comment/22911665",
                    attributedTo: replyingActor,
                    to: [RemoteAddressee.PublicCollection],
                    cc: [RemoteAddressee.NewActor(community), RemoteAddressee.NewActor(originalActor)],
                    inReplyTo: ["https://startrek.website/post/37414786"],
                    type: "https://www.w3.org/ns/activitystreams#Note",
                    postedAt: DateTimeOffset.Parse("2026-03-28T06:54:04.633290Z"),
                    sensitive: false,
                    name: null,
                    summary: null,
                    sanitizedContent: "<p>Do you have to pass an exam to get the non-cadet variants?</p>\n",
                    url: null,
                    audience: community.Id,
                    attachments: [],
                    isBridgyFed: false),
                result);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1859:Use concrete types when possible for improved performance", Justification = "Interface is implemented explicitly")]
        private static IActivityPubRemotePostService GetService(
            IActivityPubRequestHandler activityPubRequestHandler,
            Dictionary<string, RemoteAddressee> addressees)
        {
            var activityPubRemoteActorServiceMock = new Mock<IActivityPubRemoteActorService>(MockBehavior.Strict);
            foreach (var addressee in addressees)
            {
                activityPubRemoteActorServiceMock
                    .Setup(svc => svc.FetchAddresseeAsync(
                        addressee.Key,
                        It.IsAny<CancellationToken>()))
                    .ReturnsAsync(addressee.Value);

                if (addressee.Value is RemoteAddressee.Actor actor)
                {
                    activityPubRemoteActorServiceMock
                        .Setup(svc => svc.FetchActorAsync(
                            addressee.Key,
                            It.IsAny<CancellationToken>()))
                        .ReturnsAsync(actor.Item);
                }
            }

            activityPubRemoteActorServiceMock
                .Setup(svc => svc.FetchAddresseeAsync(
                    RemoteAddressee.PublicCollection.Id,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(RemoteAddressee.PublicCollection);

            return new ActivityPubRemotePostService(
                new JsonLdExpansionService(),
                activityPubRemoteActorServiceMock.Object,
                activityPubRequestHandler);
        }

        private static void AssertAreEqual(RemotePost expected, RemotePost actual)
        {
            Assert.AreEqual($"{expected}", $"{actual}");
            Assert.AreEqual(expected.PostedAt, actual.PostedAt);
            Assert.AreEqual(expected.AttributedTo, actual.AttributedTo);
            Assert.AreEqual(expected.To, actual.To);
            Assert.AreEqual(expected.Cc, actual.Cc);
            Assert.AreEqual(expected, actual);
        }

        private class JsonLdExpansionService : IJsonLdExpansionService
        {
            public JToken ExpandFirst(JObject jObject) =>
                JsonLdProcessor.Expand(jObject).First();
        }
    }
}

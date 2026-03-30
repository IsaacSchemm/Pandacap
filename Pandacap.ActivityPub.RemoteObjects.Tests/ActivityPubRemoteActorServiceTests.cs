using JsonLD.Core;
using Microsoft.FSharp.Collections;
using Moq;
using Newtonsoft.Json.Linq;
using Pandacap.ActivityPub.JsonLd.Interfaces;
using Pandacap.ActivityPub.RemoteObjects.Interfaces;
using Pandacap.ActivityPub.RemoteObjects.Models;
using Pandacap.ActivityPub.Services.Interfaces;
using System.Net;

namespace Pandacap.ActivityPub.RemoteObjects.Tests
{
    [TestClass]
    public sealed class ActivityPubRemoteActorServiceTests
    {
        [TestMethod]
        public async Task FetchActorAsync_ParsesMastodonActor()
        {
            var json = @"{""@context"":[""https://www.w3.org/ns/activitystreams"",""https://w3id.org/security/v1"",{""manuallyApprovesFollowers"":""as:manuallyApprovesFollowers"",""toot"":""http://joinmastodon.org/ns#"",""featured"":{""@id"":""toot:featured"",""@type"":""@id""},""featuredTags"":{""@id"":""toot:featuredTags"",""@type"":""@id""},""alsoKnownAs"":{""@id"":""as:alsoKnownAs"",""@type"":""@id""},""movedTo"":{""@id"":""as:movedTo"",""@type"":""@id""},""schema"":""http://schema.org#"",""PropertyValue"":""schema:PropertyValue"",""value"":""schema:value"",""discoverable"":""toot:discoverable"",""Device"":""toot:Device"",""Ed25519Signature"":""toot:Ed25519Signature"",""Ed25519Key"":""toot:Ed25519Key"",""Curve25519Key"":""toot:Curve25519Key"",""EncryptedMessage"":""toot:EncryptedMessage"",""publicKeyBase64"":""toot:publicKeyBase64"",""deviceId"":""toot:deviceId"",""claim"":{""@type"":""@id"",""@id"":""toot:claim""},""fingerprintKey"":{""@type"":""@id"",""@id"":""toot:fingerprintKey""},""identityKey"":{""@type"":""@id"",""@id"":""toot:identityKey""},""devices"":{""@type"":""@id"",""@id"":""toot:devices""},""messageFranking"":""toot:messageFranking"",""messageType"":""toot:messageType"",""cipherText"":""toot:cipherText"",""suspended"":""toot:suspended"",""Emoji"":""toot:Emoji"",""focalPoint"":{""@container"":""@list"",""@id"":""toot:focalPoint""},""Hashtag"":""as:Hashtag""}],""id"":""https://bitbang.social/users/ActionRetro"",""type"":""Person"",""following"":""https://bitbang.social/users/ActionRetro/following"",""followers"":""https://bitbang.social/users/ActionRetro/followers"",""inbox"":""https://bitbang.social/users/ActionRetro/inbox"",""outbox"":""https://bitbang.social/users/ActionRetro/outbox"",""featured"":""https://bitbang.social/users/ActionRetro/collections/featured"",""featuredTags"":""https://bitbang.social/users/ActionRetro/collections/tags"",""preferredUsername"":""ActionRetro"",""name"":""Action Retro :apple_inc:"",""summary"":""\u003cp\u003eOh hey, it\u0026#39;s me, that Action Retro guy from the \u003ca href=\""https://bitbang.social/tags/YouTube\"" class=\""mention hashtag\"" rel=\""tag\""\u003e#\u003cspan\u003eYouTube\u003c/span\u003e\u003c/a\u003e\u003c/p\u003e\u003cp\u003eBitBang.social admin\u003c/p\u003e\u003cp\u003eWant to help support BitBang? \u003ca href=\""https://patreon.com/ActionRetro\"" target=\""_blank\"" rel=\""nofollow noopener noreferrer\""\u003e\u003cspan class=\""invisible\""\u003ehttps://\u003c/span\u003e\u003cspan class=\""\""\u003epatreon.com/ActionRetro\u003c/span\u003e\u003cspan class=\""invisible\""\u003e\u003c/span\u003e\u003c/a\u003e\u003c/p\u003e\u003cp\u003e\u003ca href=\""https://bitbang.social/tags/RetroComputing\"" class=\""mention hashtag\"" rel=\""tag\""\u003e#\u003cspan\u003eRetroComputing\u003c/span\u003e\u003c/a\u003e \u003ca href=\""https://bitbang.social/tags/VintageComputers\"" class=\""mention hashtag\"" rel=\""tag\""\u003e#\u003cspan\u003eVintageComputers\u003c/span\u003e\u003c/a\u003e \u003ca href=\""https://bitbang.social/tags/Macintosh\"" class=\""mention hashtag\"" rel=\""tag\""\u003e#\u003cspan\u003eMacintosh\u003c/span\u003e\u003c/a\u003e \u003ca href=\""https://bitbang.social/tags/Apple\"" class=\""mention hashtag\"" rel=\""tag\""\u003e#\u003cspan\u003eApple\u003c/span\u003e\u003c/a\u003e \u003ca href=\""https://bitbang.social/tags/Commodore\"" class=\""mention hashtag\"" rel=\""tag\""\u003e#\u003cspan\u003eCommodore\u003c/span\u003e\u003c/a\u003e \u003ca href=\""https://bitbang.social/tags/UNIX\"" class=\""mention hashtag\"" rel=\""tag\""\u003e#\u003cspan\u003eUNIX\u003c/span\u003e\u003c/a\u003e \u003ca href=\""https://bitbang.social/tags/Linux\"" class=\""mention hashtag\"" rel=\""tag\""\u003e#\u003cspan\u003eLinux\u003c/span\u003e\u003c/a\u003e \u003ca href=\""https://bitbang.social/tags/DOS\"" class=\""mention hashtag\"" rel=\""tag\""\u003e#\u003cspan\u003eDOS\u003c/span\u003e\u003c/a\u003e \u003ca href=\""https://bitbang.social/tags/CPM\"" class=\""mention hashtag\"" rel=\""tag\""\u003e#\u003cspan\u003eCPM\u003c/span\u003e\u003c/a\u003e\u003c/p\u003e"",""url"":""https://bitbang.social/@ActionRetro"",""manuallyApprovesFollowers"":false,""discoverable"":true,""published"":""2022-11-08T00:00:00Z"",""devices"":""https://bitbang.social/users/ActionRetro/collections/devices"",""publicKey"":{""id"":""https://bitbang.social/users/ActionRetro#main-key"",""owner"":""https://bitbang.social/users/ActionRetro"",""publicKeyPem"":""-----BEGIN PUBLIC KEY-----\nMIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAxUAKglg/Xajr89DxLEci\n9Fj20ZzI55dJ72W4SkYM30PHJ4ugZZ7IpAFmaALdJP2uOy/QbJ2pBMLy+9LAF8GR\nGja5kdOTrrD87DoEre3XAj+EvtMhveNKWyvZA/O6J4Q3Z+e9p+U+DmU3IihBz0Jw\nj+QJaGkqqIa89tkfAjkhHDaG/6rkHDPx5FeP8F2kEmtzxdRBtbHqbwnJnrSZUuBC\nG0Lcav1vkJ7SOUeEzlvLIg8mDNQCAgdeENLa2w1zkWKq4RGdO0bA4PgpxooJDclk\nm+7d5iH38RVnddLMgBawUcscA5Y8nSfbbVAVKwDFFQ2o6QLuzdvjWa6QZFneHekS\nhwIDAQAB\n-----END PUBLIC KEY-----\n""},""tag"":[{""id"":""https://bitbang.social/emojis/3358"",""type"":""Emoji"",""name"":"":apple_inc:"",""updated"":""2022-11-10T17:57:13Z"",""icon"":{""type"":""Image"",""mediaType"":""image/png"",""url"":""https://files.bitbang.social/custom_emojis/images/000/003/358/original/143c72079ec60a88.png""}},{""type"":""Hashtag"",""href"":""https://bitbang.social/tags/apple"",""name"":""#apple""},{""type"":""Hashtag"",""href"":""https://bitbang.social/tags/youtube"",""name"":""#youtube""},{""type"":""Hashtag"",""href"":""https://bitbang.social/tags/linux"",""name"":""#linux""},{""type"":""Hashtag"",""href"":""https://bitbang.social/tags/unix"",""name"":""#unix""},{""type"":""Hashtag"",""href"":""https://bitbang.social/tags/retrocomputing"",""name"":""#retrocomputing""},{""type"":""Hashtag"",""href"":""https://bitbang.social/tags/macintosh"",""name"":""#macintosh""},{""type"":""Hashtag"",""href"":""https://bitbang.social/tags/commodore"",""name"":""#commodore""},{""type"":""Hashtag"",""href"":""https://bitbang.social/tags/dos"",""name"":""#dos""},{""type"":""Hashtag"",""href"":""https://bitbang.social/tags/VintageComputers"",""name"":""#VintageComputers""},{""type"":""Hashtag"",""href"":""https://bitbang.social/tags/CPM"",""name"":""#CPM""}],""attachment"":[{""type"":""PropertyValue"",""name"":""YouTube"",""value"":""\u003ca href=\""https://youtube.com/@ActionRetro\"" target=\""_blank\"" rel=\""nofollow noopener noreferrer me\""\u003e\u003cspan class=\""invisible\""\u003ehttps://\u003c/span\u003e\u003cspan class=\""\""\u003eyoutube.com/@ActionRetro\u003c/span\u003e\u003cspan class=\""invisible\""\u003e\u003c/span\u003e\u003c/a\u003e""},{""type"":""PropertyValue"",""name"":""Patreon"",""value"":""\u003ca href=\""https://www.patreon.com/ActionRetro\"" target=\""_blank\"" rel=\""nofollow noopener noreferrer me\""\u003e\u003cspan class=\""invisible\""\u003ehttps://www.\u003c/span\u003e\u003cspan class=\""\""\u003epatreon.com/ActionRetro\u003c/span\u003e\u003cspan class=\""invisible\""\u003e\u003c/span\u003e\u003c/a\u003e""},{""type"":""PropertyValue"",""name"":""Location"",""value"":""Philly""},{""type"":""PropertyValue"",""name"":""Favorite object"",""value"":""[object Object]""}],""endpoints"":{""sharedInbox"":""https://bitbang.social/inbox""},""icon"":{""type"":""Image"",""mediaType"":""image/png"",""url"":""https://files.bitbang.social/accounts/avatars/000/000/001/original/97a1356135995379.png""},""image"":{""type"":""Image"",""mediaType"":""image/jpeg"",""url"":""https://files.bitbang.social/accounts/headers/000/000/001/original/9c90033a37a50e33.jpg""}}";

            using var cts = new CancellationTokenSource();
            var cancellationToken = cts.Token;

            var requestHandlerMock = new Mock<IActivityPubRequestHandler>(MockBehavior.Strict);
            requestHandlerMock
                .Setup(handler => handler.GetJsonAsync(
                    new Uri("https://bitbang.social/@ActionRetro"),
                    cancellationToken))
                .ReturnsAsync(json);

            var service = GetService(requestHandlerMock.Object);

            var expected = new RemoteActor(
                type: "https://www.w3.org/ns/activitystreams#Person",
                id: "https://bitbang.social/users/ActionRetro",
                inbox: "https://bitbang.social/users/ActionRetro/inbox",
                sharedInbox: "https://bitbang.social/inbox",
                preferredUsername: "ActionRetro",
                name: "Action Retro :apple_inc:",
                summary: "<p>Oh hey, it&#39;s me, that Action Retro guy from the <a href=\"https://bitbang.social/tags/YouTube\" class=\"mention hashtag\" rel=\"tag\">#<span>YouTube</span></a></p><p>BitBang.social admin</p><p>Want to help support BitBang? <a href=\"https://patreon.com/ActionRetro\" target=\"_blank\" rel=\"nofollow noopener noreferrer\"><span class=\"invisible\">https://</span><span class=\"\">patreon.com/ActionRetro</span><span class=\"invisible\"></span></a></p><p><a href=\"https://bitbang.social/tags/RetroComputing\" class=\"mention hashtag\" rel=\"tag\">#<span>RetroComputing</span></a> <a href=\"https://bitbang.social/tags/VintageComputers\" class=\"mention hashtag\" rel=\"tag\">#<span>VintageComputers</span></a> <a href=\"https://bitbang.social/tags/Macintosh\" class=\"mention hashtag\" rel=\"tag\">#<span>Macintosh</span></a> <a href=\"https://bitbang.social/tags/Apple\" class=\"mention hashtag\" rel=\"tag\">#<span>Apple</span></a> <a href=\"https://bitbang.social/tags/Commodore\" class=\"mention hashtag\" rel=\"tag\">#<span>Commodore</span></a> <a href=\"https://bitbang.social/tags/UNIX\" class=\"mention hashtag\" rel=\"tag\">#<span>UNIX</span></a> <a href=\"https://bitbang.social/tags/Linux\" class=\"mention hashtag\" rel=\"tag\">#<span>Linux</span></a> <a href=\"https://bitbang.social/tags/DOS\" class=\"mention hashtag\" rel=\"tag\">#<span>DOS</span></a> <a href=\"https://bitbang.social/tags/CPM\" class=\"mention hashtag\" rel=\"tag\">#<span>CPM</span></a></p>",
                url: "https://bitbang.social/@ActionRetro",
                iconUrl: "https://files.bitbang.social/accounts/avatars/000/000/001/original/97a1356135995379.png",
                keyId: "https://bitbang.social/users/ActionRetro#main-key",
                keyPem: "-----BEGIN PUBLIC KEY-----\nMIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAxUAKglg/Xajr89DxLEci\n9Fj20ZzI55dJ72W4SkYM30PHJ4ugZZ7IpAFmaALdJP2uOy/QbJ2pBMLy+9LAF8GR\nGja5kdOTrrD87DoEre3XAj+EvtMhveNKWyvZA/O6J4Q3Z+e9p+U+DmU3IihBz0Jw\nj+QJaGkqqIa89tkfAjkhHDaG/6rkHDPx5FeP8F2kEmtzxdRBtbHqbwnJnrSZUuBC\nG0Lcav1vkJ7SOUeEzlvLIg8mDNQCAgdeENLa2w1zkWKq4RGdO0bA4PgpxooJDclk\nm+7d5iH38RVnddLMgBawUcscA5Y8nSfbbVAVKwDFFQ2o6QLuzdvjWa6QZFneHekS\nhwIDAQAB\n-----END PUBLIC KEY-----\n");

            var result = await service.FetchActorAsync("https://bitbang.social/@ActionRetro", cancellationToken);
            Assert.AreEqual(expected, result);

            var addressee = await service.FetchAddresseeAsync("https://bitbang.social/@ActionRetro", cancellationToken);
            Assert.AreEqual(RemoteAddressee.NewActor(expected), addressee);

            var addressees = await service.FetchAddresseesAsync(["https://bitbang.social/@ActionRetro"], cancellationToken);
            Assert.AreEqual(RemoteAddressee.NewActor(expected), addressees.Single());
        }

        [TestMethod]
        public async Task FetchActorAsync_ParsesPixelfedActor()
        {
            var json = @"{""@context"":[""https:\/\/w3id.org\/security\/v1"",""https:\/\/www.w3.org\/ns\/activitystreams"",{""toot"":""http:\/\/joinmastodon.org\/ns#"",""manuallyApprovesFollowers"":""as:manuallyApprovesFollowers"",""alsoKnownAs"":{""@id"":""as:alsoKnownAs"",""@type"":""@id""},""movedTo"":{""@id"":""as:movedTo"",""@type"":""@id""},""indexable"":""toot:indexable"",""suspended"":""toot:suspended""}],""id"":""https:\/\/pixelfed.social\/users\/dansup"",""type"":""Person"",""following"":""https:\/\/pixelfed.social\/users\/dansup\/following"",""followers"":""https:\/\/pixelfed.social\/users\/dansup\/followers"",""inbox"":""https:\/\/pixelfed.social\/users\/dansup\/inbox"",""outbox"":""https:\/\/pixelfed.social\/users\/dansup\/outbox"",""preferredUsername"":""dansup"",""name"":""dansup"",""summary"":""Hi, I'm the developer behind <a class=\""u-url mention\"" href=\""https:\/\/pixelfed.social\/Pixelfed\"" rel=\""external nofollow noopener\"" target=\""_blank\"">@Pixelfed<\/a>!\n\n<a href=\""https:\/\/pixelfed.social\/discover\/tags\/pixelfed?src=hash\"" title=\""#pixelfed\"" class=\""u-url hashtag\"" rel=\""external nofollow noopener\"">#pixelfed<\/a> <a href=\""https:\/\/pixelfed.social\/discover\/tags\/photography?src=hash\"" title=\""#photography\"" class=\""u-url hashtag\"" rel=\""external nofollow noopener\"">#photography<\/a> <a href=\""https:\/\/pixelfed.social\/discover\/tags\/nature?src=hash\"" title=\""#nature\"" class=\""u-url hashtag\"" rel=\""external nofollow noopener\"">#nature<\/a> <a href=\""https:\/\/pixelfed.social\/discover\/tags\/dogs?src=hash\"" title=\""#dogs\"" class=\""u-url hashtag\"" rel=\""external nofollow noopener\"">#dogs<\/a> <a href=\""https:\/\/pixelfed.social\/discover\/tags\/fedi22?src=hash\"" title=\""#fedi22\"" class=\""u-url hashtag\"" rel=\""external nofollow noopener\"">#fedi22<\/a>"",""url"":""https:\/\/pixelfed.social\/dansup"",""manuallyApprovesFollowers"":false,""indexable"":true,""published"":""2018-06-01T00:00:00Z"",""publicKey"":{""id"":""https:\/\/pixelfed.social\/users\/dansup#main-key"",""owner"":""https:\/\/pixelfed.social\/users\/dansup"",""publicKeyPem"":""-----BEGIN PUBLIC KEY-----\nMIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAn9TEqiXOvCKBS7dK8+ZV\ncO\/UmPRejL77hmO74sHIyteJVHUhnhzBz3PAWaQWdv9A4ViL8ghhSV50NzO6HIrk\nzlclmK0GeGrxRFDBLGHpa4KFErqcQgIG3uRjJ5UDhUijEsbusmnVhpLWUFEO7Atw\nbhd\/XVciruF6ea3ryyco6ZES7IHKdUBwM3IKpZqIb\/h2ObXcVVC1I2oggHRxR+eP\nqst0qU31MryUkBqX7DVcNV\/yXdRUuEc+ZiK\/rNkr3RCzE3J7PePH5RNpJDIfj4Jn\n+lW7ErT5Susn1+VHP7NHpAK8pe8atUpXEtogAbgt7KYi0Kq+XCxLv7YZuOqSaX2p\nZwIDAQAB\n-----END PUBLIC KEY-----\n""},""icon"":{""type"":""Image"",""mediaType"":""image\/jpeg"",""url"":""https:\/\/pixelfed.social\/storage\/avatars\/000\/000\/000\/000\/000\/000\/2\/mLZr2R47XEwbmasH2M3P_avatar.jpg?v=57""},""endpoints"":{""sharedInbox"":""https:\/\/pixelfed.social\/f\/inbox""}}";

            using var cts = new CancellationTokenSource();
            var cancellationToken = cts.Token;

            var requestHandlerMock = new Mock<IActivityPubRequestHandler>(MockBehavior.Strict);
            requestHandlerMock
                .Setup(handler => handler.GetJsonAsync(
                    new Uri("https://pixelfed.social/dansup"),
                    cancellationToken))
                .ReturnsAsync(json);

            var service = GetService(requestHandlerMock.Object);

            var expected = new RemoteActor(
                type: "https://www.w3.org/ns/activitystreams#Person",
                id: "https://pixelfed.social/users/dansup",
                inbox: "https://pixelfed.social/users/dansup/inbox",
                sharedInbox: "https://pixelfed.social/f/inbox",
                preferredUsername: "dansup",
                name: "dansup",
                summary: "Hi, I'm the developer behind <a class=\"u-url mention\" href=\"https://pixelfed.social/Pixelfed\" rel=\"external nofollow noopener\" target=\"_blank\">@Pixelfed</a>!\n\n<a href=\"https://pixelfed.social/discover/tags/pixelfed?src=hash\" title=\"#pixelfed\" class=\"u-url hashtag\" rel=\"external nofollow noopener\">#pixelfed</a> <a href=\"https://pixelfed.social/discover/tags/photography?src=hash\" title=\"#photography\" class=\"u-url hashtag\" rel=\"external nofollow noopener\">#photography</a> <a href=\"https://pixelfed.social/discover/tags/nature?src=hash\" title=\"#nature\" class=\"u-url hashtag\" rel=\"external nofollow noopener\">#nature</a> <a href=\"https://pixelfed.social/discover/tags/dogs?src=hash\" title=\"#dogs\" class=\"u-url hashtag\" rel=\"external nofollow noopener\">#dogs</a> <a href=\"https://pixelfed.social/discover/tags/fedi22?src=hash\" title=\"#fedi22\" class=\"u-url hashtag\" rel=\"external nofollow noopener\">#fedi22</a>",
                url: "https://pixelfed.social/dansup",
                iconUrl: "https://pixelfed.social/storage/avatars/000/000/000/000/000/000/2/mLZr2R47XEwbmasH2M3P_avatar.jpg?v=57",
                keyId: "https://pixelfed.social/users/dansup#main-key",
                keyPem: "-----BEGIN PUBLIC KEY-----\nMIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAn9TEqiXOvCKBS7dK8+ZV\ncO/UmPRejL77hmO74sHIyteJVHUhnhzBz3PAWaQWdv9A4ViL8ghhSV50NzO6HIrk\nzlclmK0GeGrxRFDBLGHpa4KFErqcQgIG3uRjJ5UDhUijEsbusmnVhpLWUFEO7Atw\nbhd/XVciruF6ea3ryyco6ZES7IHKdUBwM3IKpZqIb/h2ObXcVVC1I2oggHRxR+eP\nqst0qU31MryUkBqX7DVcNV/yXdRUuEc+ZiK/rNkr3RCzE3J7PePH5RNpJDIfj4Jn\n+lW7ErT5Susn1+VHP7NHpAK8pe8atUpXEtogAbgt7KYi0Kq+XCxLv7YZuOqSaX2p\nZwIDAQAB\n-----END PUBLIC KEY-----\n");

            var result = await service.FetchActorAsync("https://pixelfed.social/dansup", cancellationToken);
            Assert.AreEqual(expected, result);

            var addressee = await service.FetchAddresseeAsync("https://pixelfed.social/dansup", cancellationToken);
            Assert.AreEqual(RemoteAddressee.NewActor(expected), addressee);

            var addressees = await service.FetchAddresseesAsync(["https://pixelfed.social/dansup"], cancellationToken);
            Assert.AreEqual(RemoteAddressee.NewActor(expected), addressees.Single());
        }

        [TestMethod]
        public async Task FetchActorAsync_ParsesLemmyActor()
        {
            var json = @"{
  ""@context"": [
    ""https://join-lemmy.org/context.json"",
    ""https://www.w3.org/ns/activitystreams""
  ],
  ""type"": ""Person"",
  ""id"": ""https://startrek.website/u/Karim"",
  ""preferredUsername"": ""Karim"",
  ""inbox"": ""https://startrek.website/u/Karim/inbox"",
  ""outbox"": ""https://startrek.website/u/Karim/outbox"",
  ""publicKey"": {
    ""id"": ""https://startrek.website/u/Karim#main-key"",
    ""owner"": ""https://startrek.website/u/Karim"",
    ""publicKeyPem"": ""-----BEGIN PUBLIC KEY-----\nMIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAsQgOYf2IM3KKSMj47LDN\ncfQLsGeskPR6jnVQ5JsN5KifmrNgy8T39Vqg952IYm61zfiaBwgwA9MICrYMCWJS\nWNdMjW07XOlOP0ijBnIpuAGzoU6w/xb2sOxd+lPCjBhIp4LpEeQiY86XQq8bvBA/\n2t8ZUfQ3uVF812Uz4uYAZo1zjTizGp7BZF9AeUvrdR6MhKeFRkbHzi09wInrWy5Y\n7qUJaXHkVgJnvHziLrRelcE/bNItpjekrsbEgp3ylUQtmrRgaW+pGBgPSQnUtr9X\n1KYrFyQtEx7oRS1wOIWYdt68tlTk/f+WW1w5rPNo2eSjUuW4KyY6ERgXn9/6QzF0\nPQIDAQAB\n-----END PUBLIC KEY-----\n""
  },
  ""icon"": {
    ""type"": ""Image"",
    ""url"": ""https://startrek.website/pictrs/image/4e71ceb2-cabb-4659-b072-7a9d1e6e079b.jpeg""
  },
  ""image"": {
    ""type"": ""Image"",
    ""url"": ""https://startrek.website/pictrs/image/ddfcdf01-47a8-44da-9200-94feed461c01.jpeg""
  },
  ""endpoints"": {
    ""sharedInbox"": ""https://startrek.website/inbox""
  },
  ""published"": ""2026-02-18T00:36:09.761318Z""
}";

            using var cts = new CancellationTokenSource();
            var cancellationToken = cts.Token;

            var requestHandlerMock = new Mock<IActivityPubRequestHandler>(MockBehavior.Strict);
            requestHandlerMock
                .Setup(handler => handler.GetJsonAsync(
                    new Uri("https://startrek.website/u/Karim"),
                    cancellationToken))
                .ReturnsAsync(json);

            var service = GetService(requestHandlerMock.Object);

            var expected = new RemoteActor(
                type: "https://www.w3.org/ns/activitystreams#Person",
                id: "https://startrek.website/u/Karim",
                inbox: "https://startrek.website/u/Karim/inbox",
                sharedInbox: "https://startrek.website/inbox",
                preferredUsername: "Karim",
                name: null,
                summary: null,
                url: null,
                iconUrl: "https://startrek.website/pictrs/image/4e71ceb2-cabb-4659-b072-7a9d1e6e079b.jpeg",
                keyId: "https://startrek.website/u/Karim#main-key",
                keyPem: "-----BEGIN PUBLIC KEY-----\nMIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAsQgOYf2IM3KKSMj47LDN\ncfQLsGeskPR6jnVQ5JsN5KifmrNgy8T39Vqg952IYm61zfiaBwgwA9MICrYMCWJS\nWNdMjW07XOlOP0ijBnIpuAGzoU6w/xb2sOxd+lPCjBhIp4LpEeQiY86XQq8bvBA/\n2t8ZUfQ3uVF812Uz4uYAZo1zjTizGp7BZF9AeUvrdR6MhKeFRkbHzi09wInrWy5Y\n7qUJaXHkVgJnvHziLrRelcE/bNItpjekrsbEgp3ylUQtmrRgaW+pGBgPSQnUtr9X\n1KYrFyQtEx7oRS1wOIWYdt68tlTk/f+WW1w5rPNo2eSjUuW4KyY6ERgXn9/6QzF0\nPQIDAQAB\n-----END PUBLIC KEY-----\n");

            var result = await service.FetchActorAsync("https://startrek.website/u/Karim", cancellationToken);
            Assert.AreEqual(expected, result);

            var addressee = await service.FetchAddresseeAsync("https://startrek.website/u/Karim", cancellationToken);
            Assert.AreEqual(RemoteAddressee.NewActor(expected), addressee);

            var addressees = await service.FetchAddresseesAsync(["https://startrek.website/u/Karim"], cancellationToken);
            Assert.AreEqual(RemoteAddressee.NewActor(expected), addressees.Single());
        }

        [TestMethod]
        public async Task FetchAddresee_ParsesCollection()
        {
            var json = @"{""@context"":""https://www.w3.org/ns/activitystreams"",""id"":""https://bitbang.social/users/ActionRetro/following"",""type"":""OrderedCollection"",""totalItems"":177,""first"":""https://bitbang.social/users/ActionRetro/following?page=1""}";

            using var cts = new CancellationTokenSource();
            var cancellationToken = cts.Token;

            var requestHandlerMock = new Mock<IActivityPubRequestHandler>(MockBehavior.Strict);
            requestHandlerMock
                .Setup(handler => handler.GetJsonAsync(
                    new Uri("https://bitbang.social/users/ActionRetro/following"),
                    cancellationToken))
                .ReturnsAsync(json);

            var service = GetService(requestHandlerMock.Object);

            var addressee = await service.FetchAddresseeAsync("https://bitbang.social/users/ActionRetro/following", cancellationToken);
            Assert.AreEqual(RemoteAddressee.NewCollection("https://bitbang.social/users/ActionRetro/following"), addressee);
        }

        [TestMethod]
        public async Task FetchAddresee_ParsesPublicCollection()
        {
            using var cts = new CancellationTokenSource();
            var cancellationToken = cts.Token;

            var requestHandlerMock = new Mock<IActivityPubRequestHandler>(MockBehavior.Strict);

            var service = GetService(requestHandlerMock.Object);

            var addressee = await service.FetchAddresseeAsync("https://www.w3.org/ns/activitystreams#Public", cancellationToken);
            Assert.AreEqual(RemoteAddressee.PublicCollection, addressee);
        }

        [TestMethod]
        public async Task FetchAddresee_HandlesExceptions()
        {
            using var cts = new CancellationTokenSource();
            var cancellationToken = cts.Token;

            var requestHandlerMock = new Mock<IActivityPubRequestHandler>(MockBehavior.Strict);
            requestHandlerMock
                .Setup(handler => handler.GetJsonAsync(
                    new Uri("https://www.example.com/unauthorized"),
                    cancellationToken))
                .ThrowsAsync(new HttpRequestException("unit test", null, statusCode: HttpStatusCode.Unauthorized));
            requestHandlerMock
                .Setup(handler => handler.GetJsonAsync(
                    new Uri("https://www.example.com/notfound"),
                    cancellationToken))
                .ThrowsAsync(new HttpRequestException("unit test", null, statusCode: HttpStatusCode.NotFound));
            requestHandlerMock
                .Setup(handler => handler.GetJsonAsync(
                    new Uri("https://www.example.com/other"),
                    cancellationToken))
                .ThrowsAsync(new NotImplementedException());

            var service = GetService(requestHandlerMock.Object);

            var unauthorized = await service.FetchAddresseeAsync("https://www.example.com/unauthorized", cancellationToken);
            Assert.AreEqual(
                RemoteAddressee.NewUnauthorizedObject("https://www.example.com/unauthorized"),
                unauthorized);

            var notfound = await service.FetchAddresseeAsync("https://www.example.com/notfound", cancellationToken);
            Assert.AreEqual(
                RemoteAddressee.NewInaccessibleObject("https://www.example.com/notfound"),
                notfound);

            var other = await service.FetchAddresseeAsync("https://www.example.com/other", cancellationToken);
            Assert.AreEqual(
                RemoteAddressee.NewInaccessibleObject("https://www.example.com/other"),
                other);

            var all = await service.FetchAddresseesAsync([
                "https://www.example.com/unauthorized",
                "https://www.example.com/notfound",
                "https://www.example.com/other"
            ], cancellationToken);

            Assert.AreEqual(
                [
                    RemoteAddressee.NewUnauthorizedObject("https://www.example.com/unauthorized"),
                    RemoteAddressee.NewInaccessibleObject("https://www.example.com/notfound"),
                    RemoteAddressee.NewInaccessibleObject("https://www.example.com/other")
                ],
                ListModule.OfArray(all));
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1859:Use concrete types when possible for improved performance", Justification = "Interface is implemented explicitly")]
        private static IActivityPubRemoteActorService GetService(
            IActivityPubRequestHandler activityPubRequestHandler)
        =>
            new ActivityPubRemoteActorService(
                new JsonLdExpansionService(),
                activityPubRequestHandler);

        private class JsonLdExpansionService : IJsonLdExpansionService
        {
            public JToken ExpandFirst(JObject jObject) =>
                JsonLdProcessor.Expand(jObject).First();
        }
    }
}

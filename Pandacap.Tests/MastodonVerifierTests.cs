using DeviantArtFs.ResponseTypes;
using FluentAssertions;
using NSign.Signatures;
using Pandacap.Signatures;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Pandacap.HighLevel.Tests
{
    [TestClass]
    public class MastodonVerifierTests
    {
        [TestMethod]
        public void TestMastodon()
        {
            var val = new MastodonVerifier().ParseSignatureValue([
                "headers = (request-target) host date digest content-type"
            ]);
            val.spec.SignatureParameters.ComponentName.Should().Be("@signature-params");
            val.spec.SignatureParameters.Components.Select(c => new
            {
                c.Type,
                c.ComponentName
            }).Should().BeEquivalentTo([
                new {
                    Type = SignatureComponentType.Derived,
                    ComponentName = "@request-target",
                },
                new {
                    Type = SignatureComponentType.HttpHeader,
                    ComponentName = "host",
                },
                new {
                    Type = SignatureComponentType.HttpHeader,
                    ComponentName = "date",
                },
                new {
                    Type = SignatureComponentType.HttpHeader,
                    ComponentName = "digest",
                },
                new {
                    Type = SignatureComponentType.HttpHeader,
                    ComponentName = "content-type",
                }
            ]);
        }


        [TestMethod]
        public void TestPixelfed()
        {
            var val = new MastodonVerifier().ParseSignatureValue([
                "headers = (request-target) host date digest content-type user-agent"
            ]);
            val.spec.SignatureParameters.ComponentName.Should().Be("@signature-params");
            val.spec.SignatureParameters.Components.Select(c => new
            {
                c.Type,
                c.ComponentName
            }).Should().BeEquivalentTo([
                new {
                    Type = SignatureComponentType.Derived,
                    ComponentName = "@request-target",
                },
                new {
                    Type = SignatureComponentType.HttpHeader,
                    ComponentName = "host",
                },
                new {
                    Type = SignatureComponentType.HttpHeader,
                    ComponentName = "date",
                },
                new {
                    Type = SignatureComponentType.HttpHeader,
                    ComponentName = "digest",
                },
                new {
                    Type = SignatureComponentType.HttpHeader,
                    ComponentName = "content-type",
                },
                new {
                    Type = SignatureComponentType.HttpHeader,
                    ComponentName = "user-agent",
                }
            ]);
        }

        [TestMethod]
        public void TestMicroblogPub()
        {
            var val = new MastodonVerifier().ParseSignatureValue([
                "headers = (request-target) user-agent host date digest content-type"
            ]);
            val.spec.SignatureParameters.ComponentName.Should().Be("@signature-params");
            val.spec.SignatureParameters.Components.Select(c => new
            {
                c.Type,
                c.ComponentName
            }).Should().BeEquivalentTo([
                new {
                    Type = SignatureComponentType.Derived,
                    ComponentName = "@request-target",
                },
                new {
                    Type = SignatureComponentType.HttpHeader,
                    ComponentName = "user-agent",
                },
                new {
                    Type = SignatureComponentType.HttpHeader,
                    ComponentName = "host",
                },
                new {
                    Type = SignatureComponentType.HttpHeader,
                    ComponentName = "date",
                },
                new {
                    Type = SignatureComponentType.HttpHeader,
                    ComponentName = "digest",
                },
                new {
                    Type = SignatureComponentType.HttpHeader,
                    ComponentName = "content-type",
                }
            ]);
        }

        [TestMethod]
        public void TestBridgyFed()
        {
            var val = new MastodonVerifier().ParseSignatureValue([
                "headers = date host digest (request-target)"
            ]);
            val.spec.SignatureParameters.ComponentName.Should().Be("@signature-params");
            val.spec.SignatureParameters.Components.Select(c => new
            {
                c.Type,
                c.ComponentName
            }).Should().BeEquivalentTo([
                new {
                    Type = SignatureComponentType.HttpHeader,
                    ComponentName = "date",
                },
                new {
                    Type = SignatureComponentType.HttpHeader,
                    ComponentName = "host",
                },
                new {
                    Type = SignatureComponentType.HttpHeader,
                    ComponentName = "digest",
                },
                new {
                    Type = SignatureComponentType.Derived,
                    ComponentName = "@request-target",
                }
            ]);
        }
    }
}
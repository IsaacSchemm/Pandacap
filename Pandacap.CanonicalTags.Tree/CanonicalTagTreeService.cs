using Microsoft.EntityFrameworkCore;
using Pandacap.CanonicalTags.Tree.Interfaces;
using Pandacap.CanonicalTags.Tree.Models;
using Pandacap.Database;
using System.Runtime.CompilerServices;

namespace Pandacap.CanonicalTags.Tree
{
    internal class CanonicalTagTreeService(
        PandacapDbContext pandacapDbContext) : ICanonicalTagTreeService
    {
        public async IAsyncEnumerable<CanonicalTagTreeDisplayNode> GetAllTagsAsync()
        {
            await foreach (var x in pandacapDbContext.CanonicalSettings.OrderBy(x => x.Name).AsAsyncEnumerable())
                yield return new CanonicalTagTreeDisplayNode(
                    x.Id,
                    $"[Setting] {x.Name}",
                    CanonicalTagType.Setting,
                    []);

            await foreach (var x in pandacapDbContext.CanonicalCharacters.OrderBy(x => x.Name).AsAsyncEnumerable())
                yield return new CanonicalTagTreeDisplayNode(
                    x.Id,
                    $"[Character] {x.Name}",
                    CanonicalTagType.Character,
                    []);

            await foreach (var x in pandacapDbContext.CanonicalSpecies.OrderBy(x => x.Name).AsAsyncEnumerable())
                yield return new CanonicalTagTreeDisplayNode(
                    x.Id,
                    $"[Species] {x.Name}",
                    CanonicalTagType.Species,
                    []);

            await foreach (var x in pandacapDbContext.CanonicalMediums.OrderBy(x => x.Name).AsAsyncEnumerable())
                yield return new CanonicalTagTreeDisplayNode(
                    x.Id,
                    $"[Medium] {x.Name}",
                    CanonicalTagType.Medium,
                    []);
        }
    }
}

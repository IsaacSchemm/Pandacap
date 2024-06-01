﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pandacap.Data;
using Pandacap.LowLevel;
using System.Text;

namespace Pandacap.Controllers
{
    [Route("Posts")]
    public class PostsController(
        PandacapDbContext context,
        ActivityPubTranslator translator) : Controller
    {
        [Route("{id}")]
        public async Task<IActionResult> Get(Guid id)
        {
            DeviantArtDeviation? post = null;
            post ??= await context.DeviantArtArtworkDeviations.Where(p => p.Id == id).SingleOrDefaultAsync();
            post ??= await context.DeviantArtTextDeviations.Where(p => p.Id == id).SingleOrDefaultAsync();

            if (post == null)
                return NotFound();

            if (Request.IsActivityPub())
                return Content(
                    ActivityPubSerializer.SerializeWithContext(translator.AsObject(post)),
                    "application/activity+json",
                    Encoding.UTF8);

            return Redirect(post?.Url ?? "/");
        }
    }
}

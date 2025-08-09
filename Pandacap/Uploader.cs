using Azure.Storage.Blobs;
using Microsoft.EntityFrameworkCore;
using Pandacap.Clients;
using Pandacap.Data;

namespace Pandacap
{
    public class Uploader(
        BlobServiceClient blobServiceClient,
        ComputerVisionProvider computerVisionProvider,
        PandacapDbContext context,
        SvgRenderer svgRenderer)
    {
        private static async Task<byte[]> ReadFileAsync(
            IFormFile file,
            CancellationToken cancellationToken)
        {
            using var stream = file.OpenReadStream();
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms, cancellationToken);
            return ms.ToArray();
        }

        private async Task<Guid> PerformUploadAsync(
            byte[] buffer,
            CancellationToken cancellationToken)
        {
            Guid blobId = Guid.NewGuid();

            using var bufferStream = new MemoryStream(buffer, writable: false);

            await blobServiceClient
                .GetBlobContainerClient("blobs")
                .UploadBlobAsync($"{blobId}", bufferStream, cancellationToken);

            return blobId;
        }

        private async Task<Upload> TrackUploadAsync(
            byte[] buffer,
            string contentType,
            string? altText,
            CancellationToken cancellationToken)
        {
            Guid blobId = await PerformUploadAsync(buffer, cancellationToken);

            Upload item = new()
            {
                Id = blobId,
                ContentType = contentType,
                AltText = altText,
                UploadedAt = DateTimeOffset.UtcNow
            };

            context.Uploads.Add(item);

            return item;
        }

        public async Task<Guid> UploadAndRenderAsync(
            IFormFile file,
            string? altText,
            CancellationToken cancellationToken)
        {
            byte[] buffer = await ReadFileAsync(
                file,
                cancellationToken);

            var upload = await TrackUploadAsync(
                buffer,
                file.ContentType,
                altText,
                cancellationToken);

            if (file.ContentType == "image/svg+xml")
            {
                using var svgStream = new MemoryStream(buffer, writable: false);

                byte[] png = svgRenderer.RenderPng(svgStream);

                var raster = await TrackUploadAsync(
                    png,
                    "image/png",
                    altText,
                    cancellationToken);

                upload.Raster = raster.Id;
            }

            await context.SaveChangesAsync(cancellationToken);

            return upload.Id;
        }

        public async Task GenerateAltTextAsync(
            Guid id,
            CancellationToken cancellationToken)
        {
            var original = await context.Uploads
                .Where(i => i.Id == id)
                .SingleAsync(cancellationToken);

            var raster = original.Raster is Guid r
                ? await context.Uploads
                    .Where(i => i.Id == r)
                    .SingleAsync(cancellationToken)
                : original;

            var data = await blobServiceClient
               .GetBlobContainerClient("blobs")
               .GetBlobClient($"{raster.Id}")
               .DownloadContentAsync(cancellationToken);

            var altText = await computerVisionProvider.AnalyzeImageAsync(
                data.Value.Content.ToArray(),
                cancellationToken);

            original.AltText = altText;
            raster.AltText = altText;

            await context.SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteIfExistsAsync(
            Guid id,
            CancellationToken cancellationToken)
        {
            var upload = await context.Uploads
                .Where(i => i.Id == id)
                .SingleOrDefaultAsync(cancellationToken);

            if (upload == null)
                return;

            await blobServiceClient
               .GetBlobContainerClient("blobs")
               .DeleteBlobIfExistsAsync($"{upload.Id}", cancellationToken: cancellationToken);

            context.Remove(upload);

            await context.SaveChangesAsync(cancellationToken);
        }
    }
}

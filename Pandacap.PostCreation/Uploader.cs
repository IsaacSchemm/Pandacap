using Azure.Storage.Blobs;
using ExifLibrary;
using Microsoft.EntityFrameworkCore;
using Pandacap.Database;
using Pandacap.ImageConversion.Interfaces;
using Pandacap.PostCreation.Interfaces;

namespace Pandacap.PostCreation
{
    internal class Uploader(
        BlobServiceClient blobServiceClient,
        PandacapDbContext pandacapDbContext,
        ISvgRenderer svgRenderer) : IUploader
    {
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
                AltText = altText ?? "",
                UploadedAt = DateTimeOffset.UtcNow
            };

            pandacapDbContext.Uploads.Add(item);

            return item;
        }

        public async Task<Guid> UploadAndRenderAsync(
            byte[] buffer,
            string contentType,
            string? altText,
            CancellationToken cancellationToken)
        {
            buffer = TryRemoveGPS(buffer);

            var upload = await TrackUploadAsync(
                buffer,
                contentType,
                altText,
                cancellationToken);

            if (contentType == "image/svg+xml")
            {
                using var svgStream = new MemoryStream(buffer, writable: false);

                byte[] png = svgRenderer.RenderToPng(svgStream);

                var raster = await TrackUploadAsync(
                    png,
                    "image/png",
                    altText,
                    cancellationToken);

                upload.Raster = raster.Id;
            }

            await pandacapDbContext.SaveChangesAsync(cancellationToken);

            return upload.Id;
        }

        public async Task DeleteIfExistsAsync(
            Guid id,
            CancellationToken cancellationToken)
        {
            var upload = await pandacapDbContext.Uploads
                .Where(i => i.Id == id)
                .SingleOrDefaultAsync(cancellationToken);

            if (upload == null)
                return;

            await blobServiceClient
               .GetBlobContainerClient("blobs")
               .DeleteBlobIfExistsAsync($"{upload.Id}", cancellationToken: cancellationToken);

            pandacapDbContext.Remove(upload);

            await pandacapDbContext.SaveChangesAsync(cancellationToken);
        }

        private static byte[] TryRemoveGPS(byte[] buffer)
        {
            ImageFile imageFile;

            try
            {
                imageFile = ImageFile.FromBuffer(buffer);
            }
            catch (Exception)
            {
                return buffer;
            }

            var lat = imageFile.Properties.Get<GPSLatitudeLongitude>(ExifTag.GPSLatitude);
            var lng = imageFile.Properties.Get<GPSLatitudeLongitude>(ExifTag.GPSLongitude);
            var alt = imageFile.Properties.Get<ExifURational>(ExifTag.GPSAltitude);

            if (lat == null && lng == null && alt == null)
                return buffer;

            if (lat != null)
            {
                lat.Degrees = new(0, 1);
                lat.Minutes = new(0, 1);
                lat.Seconds = new(0, 1);
                imageFile.Properties.Set(lat);
            }

            if (lng != null)
            {
                lng.Degrees = new(0, 1);
                lng.Minutes = new(0, 1);
                lng.Seconds = new(0, 1);
                imageFile.Properties.Set(lng);
            }

            if (alt != null)
            {
                alt.Value = new(0, 1);
                imageFile.Properties.Set(alt);
            }

            using var ms = new MemoryStream();
            imageFile.Save(ms);
            return ms.ToArray();
        }
    }
}

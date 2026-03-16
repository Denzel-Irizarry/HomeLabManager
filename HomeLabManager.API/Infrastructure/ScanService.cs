// connects to apis togeter
using HomeLabManager.API.ExceptionsAPI;
using HomeLabManager.API.Interfaces;
using HomeLabManager.API.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

//zxing allows for bar code reading
using ZXing;
using ZXing.Common;




namespace HomeLabManager.API.Infrastructure
{
    public class ScanService: ScanServiceInterface
    {
        //still using the stream for a generic implementation 
        public async Task<string> ExtractSerialAsync(ScanRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request), "Scan request cannot be null.");

            // Manual entry path
            if (request.IsManual)
            {
                return request.ManualSerial ?? throw new ArgumentException("Manual serial was selected but no serial was provided.");
            }

            //makes sure an actual image request is sent
            if (request.ImageStream == null)
                throw new FileScanningUploadException("No image provided in the stream.");

            /*
             * take the image from stream and converts it into pixel data 
             * this is where we get width, height, rgba values
             */
            try 
            {
                // Step 1: Copy the stream into a MemoryStream and load it
                using var imageStream = new MemoryStream();
                await request.ImageStream.CopyToAsync(imageStream);
                imageStream.Position = 0;

                //this is where the image is loaded into memory and converted to a format that can be processed by ZXing
                using var image = await Image.LoadAsync<Rgb24>(imageStream);

                // Step 2: Prevent OutOfMemory Exceptions by scaling down large photos
                const int MAX_DIMENSION = 1200;
                if (image.Width > MAX_DIMENSION || image.Height > MAX_DIMENSION)
                {
                    image.Mutate(x => x.Resize(new ResizeOptions
                    {
                        Mode = ResizeMode.Max,
                        Size = new Size(MAX_DIMENSION, MAX_DIMENSION)
                    }));
                }

                // Step 3: Extract raw pixels and prepare them for ZXing
                var pixels = ExtractRgbPixels(image);
                var luminanceSource = new RGBLuminanceSource(
                    pixels, image.Width, image.Height, RGBLuminanceSource.BitmapFormat.RGB24);

                // Step 4: Setup BinaryBitmap
                var binarizer = new HybridBinarizer(luminanceSource);
                var binaryBitmap = new BinaryBitmap(binarizer);

                // Step 5: Configure the barcode reader
                var reader = new MultiFormatReader
                {
                    Hints = new Dictionary<DecodeHintType, object>
                    {
                        [DecodeHintType.TRY_HARDER] = true,
                        [DecodeHintType.POSSIBLE_FORMATS] = new List<BarcodeFormat>
                        {
                            BarcodeFormat.QR_CODE, BarcodeFormat.CODE_128, BarcodeFormat.CODE_39,
                            BarcodeFormat.UPC_A, BarcodeFormat.EAN_13
                        }
                    }
                };

                // Decode the actual image
                var result = reader.decode(binaryBitmap);

                if (result == null || string.IsNullOrWhiteSpace(result.Text))
                    throw new BarcodeNotFoundException();

                return result.Text;
            }
            catch (BarcodeNotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new FileScanningUploadException($"Unable to read the uploaded image: {ex.Message}");
            }

        }

        private static byte[] ExtractRgbPixels(Image<Rgb24> image)
        {
            var rgbBytes = new byte[image.Width * image.Height * 3];
            var offset = 0;

            image.ProcessPixelRows(accessor =>
            {
                for (var y = 0; y < accessor.Height; y++)
                {
                    var row = accessor.GetRowSpan(y);
                    for (var x = 0; x < row.Length; x++)
                    {
                        rgbBytes[offset++] = row[x].R;
                        rgbBytes[offset++] = row[x].G;
                        rgbBytes[offset++] = row[x].B;
                    }
                }
            });

            return rgbBytes;
        }
    }
}

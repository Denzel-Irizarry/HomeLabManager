// connects to apis togeter
using HomeLabManager.API.ExceptionsAPI;
using HomeLabManager.API.Interfaces;
using HomeLabManager.API.Models;

//https://skia.org/ originally developed by google, skia is a 2D graphics library that provides common APIs that work across a variety of hardware and software platforms.
//https://skiasharp.com/
//uses Skiasharp for cross platform compatiability
using SkiaSharp;

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
            // Manual entry path
            if (request.IsManual)
            {
                return request.ManualSerial!;
            }

            //makes sure an actual image request is sent
            if (request.ImageStream == null)
                throw new Exception("No image provided");

            /*
             * take the image from stream and converts it into pixel data 
             * this is where we get width, height, rgba values
             */
            using var skiaBitmap = SKBitmap.Decode(request.ImageStream);

            //makes sure a valid file format is sent for decoding later
            if (skiaBitmap == null)
                throw new Exception("Invalid image file.");

            // Convert skiaBitmap pixels into byte array (RGBA)
            var pixels = skiaBitmap.Bytes;

            //zxing can not decode color images so here we are changing it into grey scale version
            var luminanceSource = new RGBLuminanceSource(pixels, skiaBitmap.Width, skiaBitmap.Height, RGBLuminanceSource.BitmapFormat.RGBA32);

            // changing grey scale into black and white
            var binarizer = new HybridBinarizer(luminanceSource);

            //changing image into format that zxing can process
            var binaryBitmap = new BinaryBitmap(binarizer);

            //reader is so we can process different kinds of barcodes
            var reader = new MultiFormatReader();

            //this is where zxing actually reads the pixels to get information from them
            var result = reader.decode(binaryBitmap);

            //here we can leave room for try catch and finally block incase of different errors
            //after decoding if issues reading and scan = empty
            if (result == null)
                throw new BarcodeNotFoundException();
                

            //here is where we send the information to the device service
            return result.Text;
            
        }
    }
}

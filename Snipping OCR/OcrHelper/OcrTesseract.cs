using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Tesseract;

namespace Snipping_OCR
{
    public class OcrTesseract : IOcr
    {
        public OcrResult Process(string filePath, string language = "eng")
        {
            using (var pix = Pix.LoadFromFile(filePath))
            {
                return ProcessProc(pix, language);
            }
        }

        public OcrResult Process(Image image, string language = "eng")
        {
            if (image == null)
                return new OcrResult { Error = "Image cannot be null.", Success = false };

            try
            {
                using (var ms = new MemoryStream())//SaveJpegHighQuality(ConvertToGrayscale(image))
                {                   
                    ResizeImageX2(image).Save(ms, System.Drawing.Imaging.ImageFormat.Tiff);
                    //image.Save("tmp.jpg", System.Drawing.Imaging.ImageFormat.Jpeg);
                     
                    ms.Position = 0;
                    ms.Seek(0, SeekOrigin.Begin);

                    var array = ms.GetBuffer();//ms.ToArray();
                    var fname = $"{DateTime.Now.ToShortTimeString().Replace("\\", "").Replace(":", "")}.jpg";
                    File.WriteAllBytes("tmp"+fname,array);

                    if (array.Length == 0)
                        return new OcrResult { Error = "Image data is empty.", Success = false };

                    using (var pix = Pix.LoadTiffFromMemory(array)
                        .ConvertRGBToGray(1, 1, 1)
                        .BinarizeOtsuAdaptiveThreshold(150, 150, 10, 10, 0.1F)//colored
                        //.BinarizeSauvola(10, 0.35f, false)//white
                        )
                    {
                        pix.Save("processed" + fname);
                        return ProcessProc(pix, language);
                    }
                }
            }
            catch (Exception e)
            {
                return new OcrResult { Error = e.Message, Success = false };
            }
        }

        private OcrResult ProcessProc(Pix pix, string language = "eng")
        {
            try
            {
                using (var engine = new TesseractEngine(@"./tessdata", language, EngineMode.Default))
                {
                    using (var page = engine.Process(pix))
                    {
                        var text = page.GetText();
                        return new OcrResult()
                        {
                            Text = text,
                            Confidence = page.GetMeanConfidence(),
                            Success = true
                        };
                    }
                }
            }
            catch (Exception e)
            {
                return new OcrResult()
                {
                    Error = e.Message,
                    Success = false
                };
            }
        }

        public void SaveJpegHighQuality(Bitmap bmp, string path)
        {
            // Find the JPEG encoder
            ImageCodecInfo jpgEncoder = GetEncoder(System.Drawing.Imaging.ImageFormat.Jpeg);

            // Set quality to 100
            EncoderParameters encoderParams = new EncoderParameters(1);
            encoderParams.Param[0] = new EncoderParameter(Encoder.Quality, 100L);

            bmp.Save(path, jpgEncoder, encoderParams);
        }
        public MemoryStream SaveJpegHighQuality(Bitmap bmp)
        {
            MemoryStream ms = new MemoryStream();

            // Find the JPEG encoder
            ImageCodecInfo jpgEncoder = GetEncoder(System.Drawing.Imaging.ImageFormat.Tiff);

            // Set quality to 100
            EncoderParameters encoderParams = new EncoderParameters(1);
            encoderParams.Param[0] = new EncoderParameter(Encoder.Quality, 100L);

            // Save to the stream instead of a path
            bmp.Save(ms, jpgEncoder, encoderParams);

            // Reset stream position to the beginning so it is ready to read
            ms.Position = 0;
            return ms;
        }

        private ImageCodecInfo GetEncoder(System.Drawing.Imaging.ImageFormat format)
        {
            return ImageCodecInfo.GetImageEncoders().FirstOrDefault(x => x.FormatID == format.Guid);
        }

public Bitmap ConvertToGrayscale(Image originalImage)
    {
        // Create a blank bitmap with the same dimensions
        Bitmap newBitmap = new Bitmap(originalImage.Width, originalImage.Height);

        using (Graphics g = Graphics.FromImage(newBitmap))
        {
            // Grayscale color matrix values (Standard luminosity weights)
            ColorMatrix colorMatrix = new ColorMatrix(
                new float[][]
                {
                new float[] {.3f, .3f, .3f, 0, 0},
                new float[] {.59f, .59f, .59f, 0, 0},
                new float[] {.11f, .11f, .11f, 0, 0},
                new float[] {0, 0, 0, 1, 0},
                new float[] {0, 0, 0, 0, 1}
                });

            using (ImageAttributes attributes = new ImageAttributes())
            {
                attributes.SetColorMatrix(colorMatrix);

                // Draw the original image onto the new bitmap using the matrix
                g.DrawImage(originalImage,
                    new Rectangle(0, 0, originalImage.Width, originalImage.Height),
                    0, 0, originalImage.Width, originalImage.Height,
                    GraphicsUnit.Pixel, attributes);
            }
        }
        return newBitmap;
    }

        public static Bitmap ResizeImageX2(Image image)=>ResizeImage(image, image.Width *2, image.Height*2);
        public static Bitmap ResizeImage(Image image, int width, int height)
        {
            var destImage = new Bitmap(width, height);
            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);
            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                using (var wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, new Rectangle(0, 0, width, height), 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }
            return destImage;
        }

    }
}

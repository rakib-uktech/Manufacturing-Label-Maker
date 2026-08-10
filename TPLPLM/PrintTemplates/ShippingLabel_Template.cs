using NetSuite;
using System;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using TPLPLM.Pages;
using ZXing;
using ZXing.Rendering;

public class ShippingLabel_Template
{
    public void Print(
        LabelInfo labelinfo,
        AssemblyItem AssemblyItem,
        string SelectedPrinter,
        string Address,
        long labelNoBase)
    {
        try
        {
            int printCount = labelinfo.Label_Qty;

            for (int i = 0; i < printCount; i++)
            {
                long currentLabelNo = labelNoBase - i;
                string labelNoStr =
                    currentLabelNo.ToString().PadLeft(labelinfo.Label_No.Length, '0');

                var printDoc = new PrintDocument();
                var printerSettings = new PrinterSettings
                {
                    PrinterName = SelectedPrinter
                };

                if (!printerSettings.IsValid)
                    throw new Exception($"Selected printer '{SelectedPrinter}' is not valid.");

                printDoc.PrinterSettings = printerSettings;

                var highRes = printerSettings.PrinterResolutions
                    .Cast<PrinterResolution>()
                    .FirstOrDefault(r => r.Kind == PrinterResolutionKind.High);

                if (highRes != null)
                    printDoc.DefaultPageSettings.PrinterResolution = highRes;

                // 4" x 6" (hundredths of an inch)
                var paperSize = new PaperSize("Shipping_Label_4x6", 400, 600);
                printDoc.DefaultPageSettings.PaperSize = paperSize;
                printDoc.DefaultPageSettings.Margins = new Margins(0, 0, 0, 0);

                printDoc.PrintPage += (sender, e) =>
                {
                    e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                    float x = 10f, y = 10f;
                    float rowSpacingStandard = 45f;

                    var font = new Font("Arial", 10);

                    string imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "mono.png");
                    if (System.IO.File.Exists(imagePath))
                    {
                        using var img = Image.FromFile(imagePath);
                        float ratio = img.Height / (float)img.Width;
                        int logoWidth = 200, logoHeight = (int)(logoWidth * ratio);
                        float topY = y;

                        e.Graphics.DrawImage(img, new RectangleF(x, topY, logoWidth, logoHeight));

                        // Barcode
                        var writer = new BarcodeWriter
                        {
                            Format = BarcodeFormat.CODE_128,
                            Options = new ZXing.Common.EncodingOptions
                            {
                                Width = 200,
                                Height = 40,
                                Margin = 0,
                            },
                            Renderer = new BitmapRenderer
                            {
                                TextFont = new Font("Arial", 10, FontStyle.Bold)
                            }
                        };
                        using Bitmap barcodeBitmap = writer.Write(labelNoStr);
                        float barcodeX = x + logoWidth + 20;
                        e.Graphics.DrawImage(barcodeBitmap, new PointF(barcodeX, topY));
                        y = topY + Math.Max(logoHeight, 40) + 10;
                    }
                    // Company Address (Top)
                    string companyAddress =
                        "Ty Dyffryn, Alder Avenue, Dyffryn Business Park, Ystrad Mynach, CF82 7TW";

                    Font companyFont = new Font("Arial", 10, FontStyle.Bold);

                    RectangleF companyRect = new RectangleF(x, y, 380, 100);

                    e.Graphics.DrawString(
                        companyAddress,
                        companyFont,
                        Brushes.Black,
                        companyRect);

                    SizeF companySize =
                        e.Graphics.MeasureString(companyAddress, companyFont, 380);

                    y += companySize.Height + 10;


                    // Line separator
                    using (var thickPen = new Pen(Color.Black, 2))
                    {
                        e.Graphics.DrawLine(thickPen, x, y, x + 400f, y);
                    }

                    y += 15;


                    void PrintTitleRow(string labelType)
                    {
                        var titleFont = new Font("Arial", 16, FontStyle.Bold);
                        string fullTitle = (labelType ?? "Label Title").ToUpper() + " LABEL";

                        float titleWidth = e.Graphics.MeasureString(fullTitle, titleFont).Width;

                        // Assuming printable width is 400; adjust as needed
                        float centerX = x + (400f - titleWidth) / 2;

                        e.Graphics.DrawString(fullTitle, titleFont, Brushes.Black, new PointF(centerX, y));
                        y += titleFont.Height + 10; // space after title
                    }
                    // Usage
                    PrintTitleRow("SHIPPING");

                    using (var thickPen = new Pen(Color.Black, 2)) // You can change 2 to 3 or more if needed
                    {
                        e.Graphics.DrawLine(thickPen, x, y, x + 400f, y);
                    }

                    y += 20; // Add more vertical space after the line




                    // Shipping Address
                    if (!string.IsNullOrWhiteSpace(labelinfo.Comment))
                    {
                        y += 10;

                        e.Graphics.DrawString(
                            "Ship To:",
                            new Font("Arial", 13, FontStyle.Bold),
                            Brushes.Black,
                            new PointF(x, y));

                        y += 30;

                        RectangleF addressRect = new RectangleF(x, y, 370, 200);

                        Font addressFont = new Font("Arial", 14, FontStyle.Bold);

                        e.Graphics.DrawString(
                            labelinfo.Comment,
                            addressFont,
                            Brushes.Black,
                            addressRect);

                        SizeF addressSize = e.Graphics.MeasureString(
                            labelinfo.Comment,
                            addressFont,
                            370);

                        y += addressSize.Height + 15;
                    }
                    // Draw a thicker horizontal line
                    using (var thickPen = new Pen(Color.Black, 2)) // You can change 2 to 3 or more if needed
                    {
                        e.Graphics.DrawLine(thickPen, x, y, x + 400f, y);
                    }

                    y += 10; // Add more vertical space after the line


                    void PrintRowWithGS1BarcodeBelowValue(string label, string aiDataRaw, string aiReadable)
                    {
                        var boldFont = new Font("Arial", 10, FontStyle.Bold);

                        // Draw label
                        e.Graphics.DrawString(label, font, Brushes.Black, new PointF(x, y));

                        // Draw value next to label (human-readable)
                        float labelWidth = e.Graphics.MeasureString(label, font).Width;
                        float valueX = x + labelWidth + 10;
                        e.Graphics.DrawString(aiReadable, boldFont, Brushes.Black, new PointF(valueX, y));

                        // Estimate vertical space and move down for barcode
                        float textHeight = Math.Max(
                            e.Graphics.MeasureString(label, font).Height,
                            e.Graphics.MeasureString(aiReadable, boldFont).Height
                        );

                        y += textHeight + 5;

                        // Prepare GS1-128 data with FNC1 character
                        string gs1EncodedData = "\u00f1" + aiDataRaw;

                        // Generate the GS1-128 barcode
                        var writer = new ZXing.BarcodeWriter
                        {
                            Format = ZXing.BarcodeFormat.CODE_128,
                            Options = new ZXing.Common.EncodingOptions
                            {
                                Width = 300,
                                Height = 50,
                                Margin = 0,
                                PureBarcode = true
                            }
                        };

                        using var barcode = writer.Write(gs1EncodedData);

                        // Position barcode
                        float barcodeX = x + 10; // adjust if needed
                        float barcodeY = y;

                        e.Graphics.DrawImage(barcode, new PointF(barcodeX, barcodeY));

                        y += barcode.Height + rowSpacingStandard;
                    }


                    string salesOrder = labelinfo?.Work_Order ?? "UNKNOWN";

                    PrintRowWithGS1BarcodeBelowValue(
                        "Sales Order:",
                        salesOrder,
                        salesOrder);


                    // Draw horizontal line
                    using (var thickPen = new Pen(Color.Black, 2))
                    {
                        e.Graphics.DrawLine(thickPen, x, y, x + 400f, y);
                    }


                };

                printDoc.Print();
            }
        }
        catch (Exception ex)
        {
            throw new Exception("Shipping Label Template Error: " + ex.Message);
        }
    }
}


using NetSuite;
using System;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using TPLPLM.Pages;
using ZXing;
using ZXing.Rendering;

public class CaseLabel_4x6_Template
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
                var printerSettings = new PrinterSettings { PrinterName = SelectedPrinter };

                if (!printerSettings.IsValid)
                    throw new Exception($"Printer '{SelectedPrinter}' is not valid.");

                printDoc.PrinterSettings = printerSettings;

                var highRes = printerSettings.PrinterResolutions
                    .Cast<PrinterResolution>()
                    .FirstOrDefault(r => r.Kind == PrinterResolutionKind.High);

                if (highRes != null)
                    printDoc.DefaultPageSettings.PrinterResolution = highRes;

                printDoc.PrintPage += (sender, e) =>
                {
                    e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                    float x = 10f, y = 10f;
                    float rowSpacing = 45f;
                    float rowSpacingStandard = 45f;
                    float rowSpacingTight = 25f;
                    float rowSpacingLoose = 55f;
                    float rowSpacingAfterBarcode = 65f;
                    float rowSpacingExtra = 75f;

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
                        Bitmap barcodeBitmap = writer.Write(labelNoStr);
                        barcodeBitmap.Save("barcode_with_bold_text.png");

                        float barcodeX = x + logoWidth + 20;
                        e.Graphics.DrawImage(barcodeBitmap, new PointF(barcodeX, topY));
                        y = topY + Math.Max(logoHeight, 40) + 10;
                    }
                    if (!string.IsNullOrEmpty(Address))
                    {
                        e.Graphics.DrawString(Address, new Font("Arial Narrow", 9), Brushes.Black, new PointF(x, y));
                        y += 25;
                        // Draw a thicker horizontal line
                        using (var thickPen = new Pen(Color.Black, 2)) // You can change 2 to 3 or more if needed
                        {
                            e.Graphics.DrawLine(thickPen, x, y, x + 400f, y);
                        }

                        y += 20; // Add more vertical space after the line

                    }
                    void PrintRow(string label, string value, Font overrideFont = null)
                    {
                        var labelFont = new Font("Arial", 10, FontStyle.Regular);
                        var valueFont = overrideFont ?? new Font("Arial", 10, FontStyle.Bold);

                        e.Graphics.DrawString(label, labelFont, Brushes.Black, new PointF(x, y));
                        e.Graphics.DrawString(value, valueFont, Brushes.Black, new PointF(x + 100, y));
                        y += rowSpacingStandard;
                    }


                    var largeFont = new Font("Arial", 16, FontStyle.Bold);
                    PrintRow("Client:", AssemblyItem?.Custitem13 ?? "N/A", largeFont);
                    PrintRow("Customer SKU:", AssemblyItem?.Custitemproduct_Spec_Sku ?? "N/A");
                    PrintRow("Reference:", AssemblyItem?.Custitem17 ?? "N/A");

                    void PrintWrappedRow(string label, string value, float labelWidth = 100f, float maxWidth = 280f)
                    {
                        var labelFont = new Font("Arial", 10, FontStyle.Regular);
                        var valueFont = new Font("Arial", 10, FontStyle.Bold);

                        // Draw the label
                        e.Graphics.DrawString(label, labelFont, Brushes.Black, new PointF(x, y));

                        // Prepare the rectangle area for the value
                        var stringFormat = new StringFormat
                        {
                            Alignment = StringAlignment.Near,
                            LineAlignment = StringAlignment.Near,
                            FormatFlags = StringFormatFlags.LineLimit
                        };

                        RectangleF valueRect = new RectangleF(x + labelWidth, y, maxWidth, 1000); // height is generous to allow wrapping
                        SizeF measuredSize = e.Graphics.MeasureString(value, valueFont, new SizeF(maxWidth, 1000), stringFormat);

                        // Draw the wrapped text
                        e.Graphics.DrawString(value, valueFont, Brushes.Black, valueRect, stringFormat);

                        // Update y based on actual height used
                        y += measuredSize.Height + 5; // Add a bit of padding
                    }
                    // Use wrapped drawing method
                    string desc = AssemblyItem?.Custitemproduct_Spec_Description ?? "N/A";
                    PrintWrappedRow("Description:", desc);

                    void PrintCaseRowInline(string label1, string value1, string label2, string value2)
                    {
                        var boldFont = new Font("Arial", 10, FontStyle.Bold);
                        float colSpacing = 250; // Horizontal offset for second pair

                        e.Graphics.DrawString(label1, font, Brushes.Black, new PointF(x, y));
                        e.Graphics.DrawString(value1, boldFont, Brushes.Black, new PointF(x + 100, y));

                        e.Graphics.DrawString(label2, font, Brushes.Black, new PointF(x + 150, y));
                        e.Graphics.DrawString(value2, boldFont, Brushes.Black, new PointF(x + 250, y));

                        y += rowSpacingStandard;
                    }

                    PrintCaseRowInline(
                         "Case Qty:", decimal.TryParse(AssemblyItem?.Custitemproduct_Spec_Qtyperouter, out var qty)
                             ? Math.Round(qty).ToString("0")
                             : "N/A",
                         "Case Weight:", AssemblyItem?.Custitemproduct_Spec_Casewtgrosskg != null
                             ? $"{AssemblyItem.Custitemproduct_Spec_Casewtgrosskg} KG"
                             : "N/A"
                     );


                    // Draw a thicker horizontal line
                    using (var thickPen = new Pen(Color.Black, 2)) // You can change 2 to 3 or more if needed
                    {
                        e.Graphics.DrawLine(thickPen, x, y, x + 400f, y);
                    }

                    y += 40; // Add more vertical space after the line


                    void PrintRowWithDataMatrix(string label, string value)
                    {
                        var regularFont = new Font("Arial", 10);
                        var boldFont = new Font("Arial", 10, FontStyle.Bold);
                        float padding = 10f;
                        int barcodeSize = 50; // Uniform square size

                        // Measure text sizes
                        SizeF labelSize = e.Graphics.MeasureString(label, regularFont);
                        SizeF valueSize = e.Graphics.MeasureString(value, boldFont);

                        // Draw label and value
                        e.Graphics.DrawString(label, regularFont, Brushes.Black, new PointF(x, y));
                        float valueX = 100;
                        e.Graphics.DrawString(value, boldFont, Brushes.Black, new PointF(valueX, y));

                        // Generate square DataMatrix barcode
                        var encodingOptions = new ZXing.Common.EncodingOptions
                        {
                            Height = barcodeSize,
                            Width = barcodeSize,
                            Margin = 0
                        };

                        // Force square shape
                        encodingOptions.Hints[ZXing.EncodeHintType.DATA_MATRIX_SHAPE] =
                            ZXing.Datamatrix.Encoder.SymbolShapeHint.FORCE_SQUARE;

                        var writer = new ZXing.BarcodeWriter
                        {
                            Format = ZXing.BarcodeFormat.DATA_MATRIX,
                            Options = encodingOptions
                        };

                        using var matrixBitmap = writer.Write(value);

                        // Position barcode aligned to middle of row
                        float barcodeX = valueX + valueSize.Width + padding + 50;
                        float barcodeY = y + (Math.Max(labelSize.Height, valueSize.Height) - barcodeSize) / 2;

                        // Draw the barcode
                        e.Graphics.DrawImage(matrixBitmap, new RectangleF(barcodeX, barcodeY, barcodeSize, barcodeSize));

                        // Update vertical position for next line
                        y += Math.Max(rowSpacingStandard, barcodeSize + 5);
                    }

                    PrintRowWithDataMatrix("Item Code:", AssemblyItem?.ItemId ?? "N/A");

                    void PrintDateInline(string label1, string value1, string label2, string value2)
                    {
                        var regularFont = new Font("Arial", 10, FontStyle.Regular);
                        var boldFont = new Font("Arial", 10, FontStyle.Bold);
                        float padding = 20f;
                        float imageHeight = 50f;

                        // Measure widths and heights
                        float label1Width = e.Graphics.MeasureString(label1, regularFont).Width;
                        float value1Width = e.Graphics.MeasureString(value1, boldFont).Width;
                        float label2Width = e.Graphics.MeasureString(label2, regularFont).Width;
                        float value2Width = e.Graphics.MeasureString(value2, boldFont).Width;

                        float label1Height = e.Graphics.MeasureString(label1, regularFont).Height;
                        float value1Height = e.Graphics.MeasureString(value1, boldFont).Height;
                        float label2Height = e.Graphics.MeasureString(label2, regularFont).Height;
                        float value2Height = e.Graphics.MeasureString(value2, boldFont).Height;

                        float textHeight = Math.Max(Math.Max(label1Height, value1Height), Math.Max(label2Height, value2Height));

                        // Try to load image to get width and apply vertical alignment
                        string imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "22PAP.png");
                        float imageWidth = 0f;
                        float imageOffsetY = 0f;
                        Image? img = null;

                        if (System.IO.File.Exists(imagePath))
                        {
                            img = Image.FromFile(imagePath);
                            float imageRatio = (float)img.Width / img.Height;
                            imageWidth = imageHeight * imageRatio;

                            float maxRowHeight = Math.Max(imageHeight, textHeight);
                            imageOffsetY = (maxRowHeight - imageHeight) / 2f;
                        }

                        float maxRowHeightFinal = Math.Max(imageHeight, textHeight);
                        float textOffsetY = (maxRowHeightFinal - textHeight) / 2f;

                        // Positioning
                        float label1X = x;
                        float value1X = label1X + label1Width + padding;
                        float label2X = value1X + value1Width + padding;
                        float value2X = label2X + label2Width + padding;
                        float imageX = value2X + value2Width + padding;

                        float baseTextY = y + textOffsetY;
                        float baseImageY = y + imageOffsetY;

                        // Draw text
                        e.Graphics.DrawString(label1, regularFont, Brushes.Black, new PointF(label1X, baseTextY));
                        e.Graphics.DrawString(value1, boldFont, Brushes.Black, new PointF(value1X, baseTextY));
                        if (!string.IsNullOrWhiteSpace(label2))
                            e.Graphics.DrawString(label2, regularFont, Brushes.Black, new PointF(label2X, baseTextY));
                        if (!string.IsNullOrWhiteSpace(value2))
                            e.Graphics.DrawString(value2, boldFont, Brushes.Black, new PointF(value2X, baseTextY));

                        // Draw image if available
                        if (img != null)
                        {
                            e.Graphics.DrawImage(img, new RectangleF(imageX, baseImageY, imageWidth, imageHeight));
                            img.Dispose();
                        }

                        y += rowSpacingStandard + 7;
                    }


                    PrintDateInline("Date Produced:", labelinfo.Create_Date, "  ", "   ");


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

                    string originalGtin = AssemblyItem?.Custitemcustom_Product_Sepc_Case_Gtin ?? string.Empty;

                    // Only proceed if originalGtin is not null or empty
                    if (!string.IsNullOrWhiteSpace(originalGtin))
                    {
                        string paddedGtin = originalGtin.PadLeft(13, '0');
                        string caseGtinValue = "02" + paddedGtin;
                        string readableCaseGtin = "(02)" + paddedGtin;

                        PrintRowWithGS1BarcodeBelowValue("Case GTIN:", caseGtinValue, readableCaseGtin);
                    }

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
            throw new Exception("Template Print Error: " + ex.Message);
        }
    }
}
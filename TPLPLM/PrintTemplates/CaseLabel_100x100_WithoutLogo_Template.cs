using NetSuite;
using System;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using TPLPLM.Pages;
using ZXing;
using ZXing.Rendering;

public class CaseLabel_100x100_WithoutLogo_Template
{
    public void Print(
        LabelInfo labelinfo,
        AssemblyItem AssemblyItem,
        string SelectedPrinter,
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
                    throw new Exception($"Printer '{SelectedPrinter}' is not valid.");

                printDoc.PrinterSettings = printerSettings;

                var highRes = printerSettings.PrinterResolutions
                    .Cast<PrinterResolution>()
                    .FirstOrDefault(r => r.Kind == PrinterResolutionKind.High);

                if (highRes != null)
                    printDoc.DefaultPageSettings.PrinterResolution = highRes;

                // 100 x 100 mm approx (adjust if needed by printer driver)
                var paperSize = new PaperSize("Custom100x100", 400, 400);
                printDoc.DefaultPageSettings.PaperSize = paperSize;
                printDoc.DefaultPageSettings.Margins = new Margins(0, 0, 0, 0);

                printDoc.PrintPage += (sender, e) =>
                {
                    e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                    float x = 10f, y = 10f;
                    float rowSpacing = 45f;
                    float rowSpacingStandard = 28f;  // was 45f
                    float rowSpacingTight = 22f;     // was 25f
                    float rowSpacingLoose = 55f;
                    float rowSpacingAfterBarcode = 65f;
                    float rowSpacingExtra = 75f;

                    var font = new Font("Arial", 10);



                    string gs1Data = "\u00f1" + labelNoStr;

                    var writer = new BarcodeWriter
                    {
                        Format = BarcodeFormat.CODE_128,
                        Options = new ZXing.Common.EncodingOptions
                        {
                            Width = 320,
                            Height = 60,
                            Margin = 10,
                            PureBarcode = true
                        },
                        Renderer = new BitmapRenderer()
                    };

                    using Bitmap barcodeBitmap = writer.Write(gs1Data);
                    barcodeBitmap.SetResolution(203, 203);

                    // 🔹 Keep barcode exactly where it was before
                    float barcodeX = x + 195;  // 175 logo + 20 spacing
                    float barcodeY = y;

                    e.Graphics.DrawImageUnscaled(barcodeBitmap, (int)barcodeX, (int)barcodeY);

                    // Update Y position
                    y += 40;
                    using (var thickPen = new Pen(Color.Black, 2)) // You can change 2 to 3 or more if needed
                    {
                        e.Graphics.DrawLine(thickPen, x, y, x + 400f, y);
                    }

                    y += 10; // Add more vertical space after the line

                    void PrintRow(string label, string value, Font overrideFont = null)
                    {
                        var labelFont = new Font("Arial", 11, FontStyle.Regular);
                        var valueFont = overrideFont ?? new Font("Arial", 11, FontStyle.Bold);

                        e.Graphics.DrawString(label, labelFont, Brushes.Black, new PointF(x, y));
                        e.Graphics.DrawString(value, valueFont, Brushes.Black, new PointF(x + 110, y));
                        y += rowSpacingStandard;
                    }


                    void PrintWrappedRow(string label, string value, float labelWidth = 100f, float maxWidth = 280f)
                    {
                        var labelFont = new Font("Arial", 11, FontStyle.Regular);
                        var valueFont = new Font("Arial", 11, FontStyle.Bold);

                        e.Graphics.DrawString(label, labelFont, Brushes.Black, new PointF(x, y));

                        var stringFormat = new StringFormat
                        {
                            Alignment = StringAlignment.Near,
                            LineAlignment = StringAlignment.Near,
                            FormatFlags = StringFormatFlags.LineLimit
                        };

                        float valueWidth = maxWidth - 10f;

                        RectangleF valueRect = new RectangleF(
                            x + labelWidth + 10f,
                            y,
                            valueWidth,
                            1000
                        );

                        // IMPORTANT: measure using same width as drawing
                        SizeF measuredSize = e.Graphics.MeasureString(
                            value,
                            valueFont,
                            new SizeF(valueWidth, 1000),
                            stringFormat
                        );

                        e.Graphics.DrawString(value, valueFont, Brushes.Black, valueRect, stringFormat);

                        // Add extra spacing for safety (double-line spacing effect)
                        y += measuredSize.Height + 15;   // <-- increased from 5 to 15
                    }


                    void PrintCaseRowInline(string label1, string value1, string label2, string value2)
                    {
                        var boldFont = new Font("Arial", 11, FontStyle.Bold);
                        float colSpacing = 250;

                        e.Graphics.DrawString(label1, font, Brushes.Black, new PointF(x, y));
                        e.Graphics.DrawString(value1, boldFont, Brushes.Black, new PointF(x + 110, y));
                        e.Graphics.DrawString(label2, font, Brushes.Black, new PointF(x + 160, y));
                        e.Graphics.DrawString(value2, boldFont, Brushes.Black, new PointF(x + 260, y));

                        y += rowSpacingTight;
                    }

                    // Draw rows with reduced spacing
                    PrintRow("Client:", AssemblyItem?.Custitem13 ?? "N/A", new Font("Arial", 16, FontStyle.Bold));
                    PrintRow("Customer SKU:", AssemblyItem?.Custitemproduct_Spec_Sku ?? "N/A");

                    // Use wrapped drawing method
                    string desc = AssemblyItem?.Description ?? "Error!";
                    PrintWrappedRow("Description:", desc);


                    // Inline case row
                    PrintCaseRowInline(
                        "Case Qty:", decimal.TryParse(AssemblyItem?.Custitemproduct_Spec_Qtyperouter, out var qty)
                            ? Math.Round(qty).ToString("0")
                            : "Error!",
                        "Case Weight:", AssemblyItem?.Custitemproduct_Spec_Casewtgrosskg != null
                            ? $"{AssemblyItem.Custitemproduct_Spec_Casewtgrosskg} KG"
                            : "Error!"
                    );


                    void PrintDateWithProductCodeAndRefTable(
                        string dateLabel, string dateValue,
                        string refLabel, string refValue,
                        string caseLabel, string caseValue)
                    {
                        var regularFont = new Font("Arial", 11, FontStyle.Regular);
                        var boldFont = new Font("Arial", 11, FontStyle.Bold);

                        float padding = 6f;
                        float linePadding = 10f;
                        float tableWidth = 400f;
                        float rowSpacing = 5f;

                        float imageHeight = 60f;
                        Image? papImage = null;
                        float imageWidth = 0f;

                        // Load PAP image
                        string imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "22PAP.png");
                        if (System.IO.File.Exists(imagePath))
                        {
                            papImage = Image.FromFile(imagePath);
                            float imageRatio = (float)papImage.Width / papImage.Height;
                            imageWidth = imageHeight * imageRatio;
                        }

                        // Measure label widths
                        float dateLabelWidth = e.Graphics.MeasureString(dateLabel, regularFont).Width;
                        float refLabelWidth = e.Graphics.MeasureString(refLabel, regularFont).Width;
                        float caseLabelWidth = e.Graphics.MeasureString(caseLabel, regularFont).Width;

                        float maxLabelWidth = Math.Max(dateLabelWidth, Math.Max(refLabelWidth, caseLabelWidth));

                        float labelAreaWidth = maxLabelWidth + linePadding;
                        float valueAreaWidth = tableWidth - labelAreaWidth - padding * 2;

                        float verticalLineX = x + labelAreaWidth;
                        float valueX = verticalLineX + padding;
                        float verticalLineTop = y;

                        // --- Top horizontal line ---
                        using (var pen = new Pen(Color.Black, 2))
                        {
                            e.Graphics.DrawLine(pen, x, y - 3, x + tableWidth, y - 3);
                        }

                        // --- Row 1: Date Produced ---
                        float textY1 = y;
                        float rowHeight = Math.Max(
                            e.Graphics.MeasureString(dateLabel, regularFont).Height,
                            e.Graphics.MeasureString(dateValue, boldFont).Height
                        );

                        e.Graphics.DrawString(dateLabel, regularFont, Brushes.Black, new PointF(x, textY1));
                        e.Graphics.DrawString(dateValue, boldFont, Brushes.Black, new PointF(valueX, textY1));

                        // --- PAP image centered across first TWO rows ---
                        if (papImage != null)
                        {
                            float twoRowsHeight = (rowHeight * 2) + rowSpacing;
                            float imageX = valueX + valueAreaWidth - imageWidth - 15f;
                            float imageY = y + (twoRowsHeight - imageHeight) / 2f + 8f;

                            e.Graphics.DrawImage(papImage, new RectangleF(imageX, imageY, imageWidth, imageHeight));
                            papImage.Dispose();
                        }

                        // --- Row 2: TPL Ref ---
                        float textY2 = textY1 + rowHeight + rowSpacing;
                        e.Graphics.DrawString(refLabel, regularFont, Brushes.Black, new PointF(x, textY2));
                        e.Graphics.DrawString(refValue, boldFont, Brushes.Black, new PointF(valueX, textY2));

                        // --- Row 3: Case Number ---
                        float textY3 = textY2 + rowHeight + rowSpacing;
                        e.Graphics.DrawString(caseLabel, regularFont, Brushes.Black, new PointF(x, textY3));
                        e.Graphics.DrawString(caseValue, boldFont, Brushes.Black, new PointF(valueX, textY3));

                        // --- Bottom + vertical lines ---
                        float verticalLineBottom = textY3 + rowHeight;

                        using (var pen = new Pen(Color.Black, 2))
                        {
                            e.Graphics.DrawLine(pen, verticalLineX, verticalLineTop, verticalLineX, verticalLineBottom);
                            e.Graphics.DrawLine(pen, x, verticalLineBottom + 3, x + tableWidth, verticalLineBottom + 3);
                        }

                        // Move Y cursor
                        y = verticalLineBottom + rowSpacing + 6;
                    }


                    // Usage
                    PrintDateWithProductCodeAndRefTable(
                        "Date Produced:", labelinfo.Create_Date,
                        "TPL Ref:", AssemblyItem?.ItemId ?? "N/A",
                        "Case Number:", labelNoStr
                    );



                    void PrintRowWithGS1BarcodeBelowValue(string label, string aiDataRaw, string aiReadable)
                    {
                        var boldFont = new Font("Arial", 11, FontStyle.Bold);

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
                                Height = 30,
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
                    //string originalGtin = "1234567891234";

                    // Only proceed if originalGtin is not null or empty
                    if (!string.IsNullOrWhiteSpace(originalGtin))
                    {
                        string paddedGtin = originalGtin.PadLeft(13, '0');
                        string caseGtinValue = "02" + paddedGtin;
                        string readableCaseGtin = "(02)" + paddedGtin;

                        PrintRowWithGS1BarcodeBelowValue("Case GTIN:", caseGtinValue, readableCaseGtin);
                    }
                };

                printDoc.Print();
            }
        }
        catch (Exception ex)
        {
            throw new Exception("100x100 Template Error: " + ex.Message);
        }
    }
}
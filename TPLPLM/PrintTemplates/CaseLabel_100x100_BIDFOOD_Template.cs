using NetSuite;
using System;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using TPLPLM.Pages;
using ZXing;
using ZXing.Rendering;

public class CaseLabel_100x100_BIDFOOD_Template
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
                string labelNoStr = currentLabelNo.ToString().PadLeft(labelinfo.Label_No.Length, '0');

                var printDoc = new PrintDocument();
                var printerSettings = new PrinterSettings { PrinterName = SelectedPrinter };

                if (!printerSettings.IsValid)
                    throw new Exception($"Printer '{SelectedPrinter}' is not valid.");

                printDoc.PrinterSettings = printerSettings;
                printDoc.PrinterSettings.Copies = 2; // BIDFOOD specific

                var highRes = printerSettings.PrinterResolutions
                    .Cast<PrinterResolution>()
                    .FirstOrDefault(r => r.Kind == PrinterResolutionKind.High);

                if (highRes != null)
                    printDoc.DefaultPageSettings.PrinterResolution = highRes;

                printDoc.DefaultPageSettings.PaperSize = new PaperSize("Custom4x4", 400, 400);
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

                    string imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "mono.png");
                    if (System.IO.File.Exists(imagePath))
                    {
                        using var img = Image.FromFile(imagePath);
                        float ratio = img.Height / (float)img.Width;
                        int logoWidth = 175, logoHeight = (int)(logoWidth * ratio);
                        float topY = y;

                        e.Graphics.DrawImage(img, new RectangleF(x, topY, logoWidth, logoHeight));
                        string gs1Data = "\u00f1" + labelNoStr;
                        var writer = new BarcodeWriter
                        {
                            Format = BarcodeFormat.CODE_128,
                            Options = new ZXing.Common.EncodingOptions
                            {
                                Width = 320,
                                Height = 60,
                                Margin = 10,
                                PureBarcode = true // This removes the human-readable text
                            },
                            Renderer = new BitmapRenderer()
                        };

                        Bitmap barcodeBitmap = writer.Write(gs1Data);
                        barcodeBitmap.SetResolution(203, 203);

                        // Draw on graphics
                        float barcodeX = x + logoWidth + 20;
                        e.Graphics.DrawImageUnscaled(barcodeBitmap, (int)barcodeX, (int)topY);

                        // Update y position
                        y = topY + Math.Max(logoHeight, barcodeBitmap.Height) - 20;

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

                        y += 10; // Add more vertical space after the line

                    }
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

                        // Draw the label
                        e.Graphics.DrawString(label, labelFont, Brushes.Black, new PointF(x, y));

                        // Prepare the rectangle area for the value
                        var stringFormat = new StringFormat
                        {
                            Alignment = StringAlignment.Near,
                            LineAlignment = StringAlignment.Near,
                            FormatFlags = StringFormatFlags.LineLimit
                        };

                        RectangleF valueRect = new RectangleF(x + labelWidth + 10f, y, maxWidth - 10f, 1000); // height is generous to allow wrapping
                        SizeF measuredSize = e.Graphics.MeasureString(value, valueFont, new SizeF(maxWidth, 1000), stringFormat);

                        // Draw the wrapped text
                        e.Graphics.DrawString(value, valueFont, Brushes.Black, valueRect, stringFormat);

                        // Update y based on actual height used
                        y += measuredSize.Height + 5; // Add a bit of padding
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
                    string desc = AssemblyItem?.Description ?? "N/A";
                    PrintWrappedRow("Description:", desc);


                    // Inline case row
                    PrintCaseRowInline(
                        "Case Qty:", decimal.TryParse(AssemblyItem?.Custitemproduct_Spec_Qtyperouter, out var qty)
                            ? Math.Round(qty).ToString("0")
                            : "N/A",
                        "Case Weight:", AssemblyItem?.Custitemproduct_Spec_Casewtgrosskg != null
                            ? $"{AssemblyItem.Custitemproduct_Spec_Casewtgrosskg} KG"
                            : "N/A"
                    );

                    // Small spacing after case row
                    y += 5;

                    // Draw top line only
                    using (var thickPen = new Pen(Color.Black, 2))
                    {
                        e.Graphics.DrawLine(thickPen, x, y, x + 400f, y);
                    }

                    y += 8;

                    // Draw centered bold message
                    var messageFont = new Font("Arial", 11, FontStyle.Bold);
                    string message = "All BIDFOOD Case Need Two Case Labels";

                    // Measure text to center it
                    SizeF msgSize = e.Graphics.MeasureString(message, messageFont);
                    float centerX = x + (400f - msgSize.Width) / 2;

                    e.Graphics.DrawString(message, messageFont, Brushes.Black, new PointF(centerX, y));

                    y += msgSize.Height + 10; // spacing after message


                    void PrintDateWithProductCodeAndRefTable(
                        string dateLabel, string dateValue,
                        string refLabel, string refValue,
                        string caseLabel, string caseValue,
                        string? caseBarcode)
                    {
                        var regularFont = new Font("Arial", 11, FontStyle.Regular);
                        var boldFont = new Font("Arial", 11, FontStyle.Bold);
                        float lineSpacing = 2f;
                        float groupSpacing = 4f;
                        float tableWidth = 400f;

                        float imageHeight = 60f;
                        Image? papImage = null;
                        float imageWidth = 0f;

                        string imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "22PAP.png");
                        if (System.IO.File.Exists(imagePath))
                        {
                            papImage = Image.FromFile(imagePath);
                            float imageRatio = (float)papImage.Width / papImage.Height;
                            imageWidth = imageHeight * imageRatio;
                        }

                        using (var pen = new Pen(Color.Black, 2))
                        {
                            e.Graphics.DrawLine(pen, x, y - 2, x + tableWidth, y - 2);
                        }

                        float currentY = y;
                        float labelBlockRightX = x;
                        float labelBlockTopY = y;

                        void DrawLabelAndValue(string label, string value)
                        {
                            float labelHeight = e.Graphics.MeasureString(label, regularFont).Height;
                            float valueHeight = e.Graphics.MeasureString(value, boldFont).Height;

                            e.Graphics.DrawString(label, regularFont, Brushes.Black, new PointF(x, currentY));
                            currentY += labelHeight + lineSpacing;
                            e.Graphics.DrawString(value, boldFont, Brushes.Black, new PointF(x, currentY));
                            currentY += valueHeight + groupSpacing;

                            float labelWidth = e.Graphics.MeasureString(label, regularFont).Width;
                            float valueWidth = e.Graphics.MeasureString(value, boldFont).Width;
                            float blockRight = x + Math.Max(labelWidth, valueWidth);
                            if (blockRight > labelBlockRightX)
                                labelBlockRightX = blockRight;
                        }

                        DrawLabelAndValue(dateLabel, dateValue);
                        DrawLabelAndValue(refLabel, refValue);
                        DrawLabelAndValue(caseLabel, caseValue);

                        float labelBlockBottomY = currentY - groupSpacing;
                        float verticalLineX = labelBlockRightX + 10f;

                        using (var pen = new Pen(Color.Black, 1.5f))
                        {
                            e.Graphics.DrawLine(pen, verticalLineX, labelBlockTopY, verticalLineX, labelBlockBottomY);
                        }

                        // --- Draw Case GTIN Barcode with value underneath ---
                        if (!string.IsNullOrWhiteSpace(caseBarcode))
                        {
                            string paddedCaseBarcode = caseBarcode.PadLeft(13, '0');
                            string gs1EncodedData = "\u00f1" + paddedCaseBarcode;

                            var writer = new ZXing.BarcodeWriter
                            {
                                Format = ZXing.BarcodeFormat.CODE_128,
                                Options = new ZXing.Common.EncodingOptions
                                {
                                    Width = 150,
                                    Height = 100,
                                    Margin = 0,
                                    PureBarcode = true
                                }
                            };

                            using var barcode = writer.Write(gs1EncodedData);

                            float barcodeX = verticalLineX + 10f;
                            float barcodeY = y + ((labelBlockBottomY - labelBlockTopY - barcode.Height - 15f) / 2f); // leave space for text

                            // Draw the barcode image
                            e.Graphics.DrawImage(barcode, new PointF(barcodeX, barcodeY));

                            // Draw the GTIN value centered under the barcode
                            var readableFont = new Font("Arial", 9, FontStyle.Regular);
                            SizeF textSize = e.Graphics.MeasureString(paddedCaseBarcode, readableFont);
                            float textX = barcodeX + (barcode.Width - textSize.Width) / 2;
                            float textY = barcodeY + barcode.Height + 2;

                            e.Graphics.DrawString(paddedCaseBarcode, readableFont, Brushes.Black, new PointF(textX, textY));

                            verticalLineX = barcodeX + barcode.Width + 10f; // update spacing before image
                        }


                        if (papImage != null)
                        {
                            float contentHeight = labelBlockBottomY - labelBlockTopY;
                            float imageX = x + tableWidth - imageWidth - 20f; // moved left by increasing right margin
                            float imageY = y + (contentHeight - imageHeight) / 2f;
                            e.Graphics.DrawImage(papImage, new RectangleF(imageX, imageY, imageWidth, imageHeight));
                            papImage.Dispose();
                        }

                        using (var pen = new Pen(Color.Black, 2))
                        {
                            e.Graphics.DrawLine(pen, x, labelBlockBottomY + 2, x + tableWidth, labelBlockBottomY + 2);
                        }

                        y = labelBlockBottomY + groupSpacing + 4;
                    }
                    string caseBarcode = AssemblyItem?.Custitem12 ?? string.Empty;

                    PrintDateWithProductCodeAndRefTable(
                        "Date Produced:", labelinfo.Create_Date,
                        "Our Ref:", AssemblyItem?.ItemId ?? "N/A",
                        "Case Number:", labelNoStr,
                        caseBarcode
                    );


                };

                printDoc.Print();
            }
        }
        catch (Exception ex)
        {
            throw new Exception("BIDFOOD Template Error: " + ex.Message);
        }
    }

    private void DrawBottomTable(
        PrintPageEventArgs e,
        ref float y,
        float x,
        LabelInfo labelinfo,
        AssemblyItem item,
        string labelNo)
    {
        var regular = new Font("Arial", 11);
        var bold = new Font("Arial", 11, FontStyle.Bold);

        float tableWidth = 400f;
        float rowSpacing = 5f;

        using var pen = new Pen(Color.Black, 2);
        e.Graphics.DrawLine(pen, x, y - 3, x + tableWidth, y - 3);

        float currentY = y;

        void DrawRow(string label, string value)
        {
            e.Graphics.DrawString(label, regular, Brushes.Black, new PointF(x, currentY));
            e.Graphics.DrawString(value, bold, Brushes.Black, new PointF(x + 120, currentY));
            currentY += 22 + rowSpacing;
        }

        DrawRow("Date Produced:", labelinfo.Create_Date);
        DrawRow("Our Ref:", item?.ItemId ?? "N/A");
        DrawRow("Case Number:", labelNo);

        using var pen2 = new Pen(Color.Black, 2);
        e.Graphics.DrawLine(pen2, x, currentY, x + tableWidth, currentY);

        y = currentY + 5;
    }
}
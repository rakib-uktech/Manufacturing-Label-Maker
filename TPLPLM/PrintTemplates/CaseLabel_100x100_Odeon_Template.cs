using NetSuite;
using System;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using TPLPLM.Pages;
using ZXing;
using ZXing.Rendering;

public class CaseLabel_100x100_Odeon_Template
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
                    throw new Exception($"Printer '{SelectedPrinter}' is not valid.");

                printDoc.PrinterSettings = printerSettings;

                var highRes = printerSettings.PrinterResolutions
                    .Cast<PrinterResolution>()
                    .FirstOrDefault(r => r.Kind == PrinterResolutionKind.High);

                if (highRes != null)
                    printDoc.DefaultPageSettings.PrinterResolution = highRes;

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
                                PureBarcode = true
                            },
                            Renderer = new BitmapRenderer()
                        };

                        Bitmap barcodeBitmap = writer.Write(gs1Data);
                        barcodeBitmap.SetResolution(203, 203);

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



                        // --- Draw Case GTIN Barcode (EAN-13) with full 13-digit value underneath ---
                        if (!string.IsNullOrWhiteSpace(caseBarcode))
                        {
                            string digitsOnly = new string(caseBarcode.Where(char.IsDigit).ToArray());

                            if (digitsOnly.Length > 12)
                                digitsOnly = digitsOnly.Substring(0, 12);

                            if (digitsOnly.Length < 12)
                                digitsOnly = digitsOnly.PadLeft(12, '0');

                            // Generate full 13-digit number
                            string fullEan13 = AddEan13CheckDigit(digitsOnly);

                            var writer = new ZXing.BarcodeWriter
                            {
                                Format = ZXing.BarcodeFormat.EAN_13,
                                Options = new ZXing.Common.EncodingOptions
                                {
                                    Width = 150,
                                    Height = 80,
                                    Margin = 2,
                                    PureBarcode = true
                                }
                            };

                            using var barcode = writer.Write(digitsOnly);

                            float barcodeX = verticalLineX + 10f;
                            float barcodeY = y + ((labelBlockBottomY - labelBlockTopY - barcode.Height - 15f) / 2f);

                            e.Graphics.DrawImage(barcode, new PointF(barcodeX, barcodeY));

                            // Draw full 13-digit number
                            var readableFont = new Font("Arial", 9, FontStyle.Regular);
                            SizeF textSize = e.Graphics.MeasureString(fullEan13, readableFont);

                            float textX = barcodeX + (barcode.Width - textSize.Width) / 2;
                            float textY = barcodeY + barcode.Height + 2;

                            e.Graphics.DrawString(fullEan13, readableFont, Brushes.Black, new PointF(textX, textY));

                            verticalLineX = barcodeX + barcode.Width + 10f;
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
            throw new Exception("Odeon Template Error: " + ex.Message);
        }
    }

    // =========================
    // SPECIAL BLOCK
    // =========================
    private void PrintOdeonBlock(
        Graphics g,
        ref float y,
        float x,
        string date,
        string refNo,
        string caseNo,
        string caseBarcode)
    {
        float tableWidth = 400f;
        float startY = y;

        var labelFont = new Font("Arial", 11);
        var valueFont = new Font("Arial", 11, FontStyle.Bold);

        using var topPen = new Pen(Color.Black, 2);
        g.DrawLine(topPen, x, y - 2, x + tableWidth, y - 2);

        float currentY = y;
        float rightEdge = x;

        void DrawBlock(string label, string value)
        {
            g.DrawString(label, labelFont, Brushes.Black, new PointF(x, currentY));
            currentY += 14;

            g.DrawString(value, valueFont, Brushes.Black, new PointF(x, currentY));
            currentY += 18;

            float width = Math.Max(
                g.MeasureString(label, labelFont).Width,
                g.MeasureString(value, valueFont).Width
            );

            rightEdge = Math.Max(rightEdge, x + width);
        }

        DrawBlock("Date Produced:", date);
        DrawBlock("Our Ref:", refNo);
        DrawBlock("Case Number:", caseNo);

        float bottomY = currentY - 4;
        float dividerX = rightEdge + 10;

        using var midPen = new Pen(Color.Black, 1.5f);
        g.DrawLine(midPen, dividerX, startY, dividerX, bottomY);

        // =========================
        // EAN-13 BARCODE
        // =========================
        if (!string.IsNullOrWhiteSpace(caseBarcode))
        {
            string digits = new string(caseBarcode.Where(char.IsDigit).ToArray());

            if (digits.Length > 12) digits = digits.Substring(0, 12);
            if (digits.Length < 12) digits = digits.PadLeft(12, '0');

            string full = AddEan13CheckDigit(digits);

            var writer = new BarcodeWriter
            {
                Format = BarcodeFormat.EAN_13,
                Options = new ZXing.Common.EncodingOptions
                {
                    Width = 150,
                    Height = 80,
                    Margin = 2,
                    PureBarcode = true
                }
            };

            using var barcode = writer.Write(digits);

            float bx = dividerX + 10;
            float by = startY + ((bottomY - startY - barcode.Height) / 2);

            g.DrawImage(barcode, new PointF(bx, by));

            g.DrawString(full, new Font("Arial", 9),
                Brushes.Black,
                new PointF(bx, by + barcode.Height + 2));
        }

        using var bottomPen = new Pen(Color.Black, 2);
        g.DrawLine(bottomPen, x, bottomY + 2, x + tableWidth, bottomY + 2);

        y = bottomY + 8;
    }

    // =========================
    // EAN CHECK DIGIT
    // =========================
    private string AddEan13CheckDigit(string input12)
    {
        int sum = 0;

        for (int i = 0; i < 12; i++)
        {
            int digit = int.Parse(input12[i].ToString());
            sum += (i % 2 == 0) ? digit : digit * 3;
        }

        int check = (10 - (sum % 10)) % 10;
        return input12 + check;
    }
}
using NetSuite;
using System;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using TPLPLM.Pages;
using ZXing;
using ZXing.Rendering;

public class CaseLabel_100x100_down2earth_Template_Straw
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

                // ===================== 100x100 LABEL =====================
                printDoc.DefaultPageSettings.PaperSize =
                    new PaperSize("100x100", 393, 393);

                printDoc.DefaultPageSettings.Margins =
                    new Margins(0, 0, 0, 0);

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

                    string imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "down2earth_logo.jpg");
                    if (System.IO.File.Exists(imagePath))
                    {
                        using var img = Image.FromFile(imagePath);

                        float ratio = img.Height / (float)img.Width;

                        int logoWidth = 75;
                        int logoHeight = (int)(logoWidth * ratio);

                        float topY = y;

                        // =========================
                        // LEFT LOGO
                        // =========================
                        e.Graphics.DrawImage(
                            img,
                            new RectangleF(x, topY, logoWidth, logoHeight));

                        // =========================
                        // BARCODE
                        // =========================
                        string gs1Data = "\u00f1" + labelNoStr;

                        var writer = new BarcodeWriter
                        {
                            Format = BarcodeFormat.CODE_128,
                            Options = new ZXing.Common.EncodingOptions
                            {
                                Width = 320,
                                Height = 40,
                                Margin = 10,
                                PureBarcode = true
                            },
                            Renderer = new BitmapRenderer()
                        };

                        Bitmap barcodeBitmap = writer.Write(gs1Data);
                        barcodeBitmap.SetResolution(203, 203);

                        float barcodeX = x + logoWidth + 120;

                        e.Graphics.DrawImageUnscaled(
                            barcodeBitmap,
                            (int)barcodeX,
                            (int)topY);

                        // =========================
                        // PAP IMAGE UNDER BARCODE
                        // =========================
                        string papPath = Path.Combine(
                            Directory.GetCurrentDirectory(),
                            "wwwroot",
                            "images",
                            "PAP22d2earthLogo.jpg");

                        if (System.IO.File.Exists(papPath))
                        {
                            using var papImage = Image.FromFile(papPath);

                            float papRatio = papImage.Height / (float)papImage.Width;

                            int papWidth = 73;
                            int papHeight = 38;

                            // Center under barcode
                            float papX =
                                barcodeX + (barcodeBitmap.Width / 2f) - (papWidth / 2f) - 55f;

                            float papY =
                                topY + barcodeBitmap.Height - 10f;

                            e.Graphics.DrawImage(
                                papImage,
                                new RectangleF(
                                    papX,
                                    papY,
                                    papWidth,
                                    papHeight));
                        }

                        // =========================
                        // UPDATE Y POSITION
                        // =========================
                        y = topY
                            + Math.Max(
                                logoHeight,
                                barcodeBitmap.Height)
                            + 10;
                    }
                    if (!string.IsNullOrEmpty(Address))
                    {
                        // Draw separator line only
                        using (var thickPen = new Pen(Color.Black, 2))
                        {
                            e.Graphics.DrawLine(thickPen, x, y, x + 400f, y);
                        }

                        // Add vertical spacing after line
                        y += 20;
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
                    //PrintRow("Client:", AssemblyItem?.Custitem20 ?? "N/A", new Font("Arial", 16, FontStyle.Bold));
                    //PrintRow("Customer SKU:", AssemblyItem?.Custitemproduct_Spec_Sku ?? "N/A");

                    // Use wrapped drawing method
                    string desc = AssemblyItem?.Description ?? "Error!";
                    PrintWrappedRow("Description:", desc);
                    PrintRow("TPL Ref:", AssemblyItem?.ItemId ?? "N/A");

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
                        "------------------  ", "",
                        "Case Number:", labelNoStr,
                        caseBarcode
                    );




                };

                printDoc.Print();
            }
        }
        catch (Exception ex)
        {
            throw new Exception(
                "Down2Earth Template Print Error: " + ex.Message);
        }
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
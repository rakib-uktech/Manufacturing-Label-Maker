using NetSuite;
using System;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using TPLPLM.Pages;
using ZXing;

public class PalletLabel_150x200_LillyPackaging_Template
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

                string labelNoStr = currentLabelNo
                    .ToString()
                    .PadLeft(labelinfo.Label_No.Length, '0');

                var printDoc = new PrintDocument();

                var printerSettings = new PrinterSettings
                {
                    PrinterName = SelectedPrinter
                };

                if (!printerSettings.IsValid)
                    throw new Exception($"Selected printer '{SelectedPrinter}' is not valid.");

                printDoc.PrinterSettings = printerSettings;
                printDoc.PrinterSettings.Copies = 2;

                var forcedSize = new PaperSize("Lilly6x8", 600, 800);

                printDoc.DefaultPageSettings.PaperSize = forcedSize;
                printDoc.DefaultPageSettings.Margins = new Margins(0, 0, 0, 0);

                printDoc.PrintPage += (sender, e) =>
                {
                    e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                    float x = 30f, y = 30f;
                    float rowSpacing = 45f;
                    float rowSpacingStandard = 45f;
                    float rowSpacingTight = 25f;
                    float rowSpacingLoose = 55f;
                    float rowSpacingAfterBarcode = 65f;
                    float rowSpacingExtra = 75f;

                    var font = new Font("Arial", 14);
                    var largeFont = new Font("Arial", 20, FontStyle.Bold);
                    var boldFont = new Font("Arial", 12, FontStyle.Bold);

                    string label = "TPL Ref:";
                    string value = AssemblyItem?.ItemId ?? "N/A";
                    string originLabel = "";
                    string originValue = "";

                    var labelFont = new Font("Arial", 10);
                    var valueFont = new Font("Arial", 10, FontStyle.Bold);

                    int barcodeSize = 50;

                    var encodingOptions = new ZXing.Common.EncodingOptions
                    {
                        Height = barcodeSize,
                        Width = barcodeSize,
                        Margin = 0
                    };
                    encodingOptions.Hints[ZXing.EncodeHintType.DATA_MATRIX_SHAPE] =
                        ZXing.Datamatrix.Encoder.SymbolShapeHint.FORCE_SQUARE;

                    var writer = new ZXing.BarcodeWriter
                    {
                        Format = ZXing.BarcodeFormat.DATA_MATRIX,
                        Options = encodingOptions
                    };

                    using var matrixBitmap = writer.Write(value);

                    // Measure text
                    SizeF labelSize = e.Graphics.MeasureString(label, labelFont);
                    SizeF originLabelSize = e.Graphics.MeasureString(originLabel, labelFont);

                    float totalTextHeight = labelSize.Height * 2 + 2;
                    float maxBlockHeight = Math.Max(barcodeSize, totalTextHeight);

                    // 🔹 Keep barcode EXACT same position as before (200 logo + 40 spacing)
                    float barcodeX = x + 240;
                    float barcodeY = y + (maxBlockHeight - barcodeSize) / 2f;

                    // Text position (same relative spacing as before)
                    float textX = barcodeX + barcodeSize + 40;
                    float textY = y + (maxBlockHeight - totalTextHeight) / 2f;

                    // Draw DataMatrix
                    e.Graphics.DrawImage(matrixBitmap,
                        new RectangleF(barcodeX, barcodeY, barcodeSize, barcodeSize));

                    // Draw right-side text
                    e.Graphics.DrawString(label, labelFont, Brushes.Black,
                        new PointF(textX, textY));

                    e.Graphics.DrawString(value, valueFont, Brushes.Black,
                        new PointF(textX + labelSize.Width + 5, textY));

                    float originY = textY + labelSize.Height + 2;

                    e.Graphics.DrawString(originLabel, labelFont, Brushes.Black,
                        new PointF(textX, originY));

                    e.Graphics.DrawString(originValue, valueFont, Brushes.Black,
                        new PointF(textX + originLabelSize.Width + 5, originY));

                    // Update Y
                    y += maxBlockHeight + 10;

                    // Draw separator line (same style as your Code128 section)
                    using (var thickPen = new Pen(Color.Black, 2))
                    {
                        e.Graphics.DrawLine(thickPen, x, y, x + 550f, y);
                    }

                    y += 20;

                    void PrintRow(string label, string value, Font overrideFont = null)
                    {
                        e.Graphics.DrawString(label, font, Brushes.Black, new PointF(x, y));
                        e.Graphics.DrawString(value, overrideFont ?? boldFont, Brushes.Black, new PointF(x + 100, y));
                        y += rowSpacingStandard;
                    }

                    //PrintRow("Customer:", AssemblyItem?.Custitem13 ?? "N/A", largeFont);

                    void PrintWrappedRow(string label, string value, float labelWidth = 100f, float maxWidth = 460f)
                    {
                        var labelFont = new Font("Arial", 14, FontStyle.Regular);
                        var valueFont = new Font("Arial", 20, FontStyle.Bold);

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
                    string desc = AssemblyItem?.Description ?? "N/A";
                    PrintWrappedRow("Description:", desc);



                    void PrintInline(string label1, string value1, string label2, string value2)
                    {
                        var regularFont = new Font("Arial", 14, FontStyle.Regular);

                        // Use NARROW font for values
                        var narrowBoldFont = new Font("Arial Narrow", 14, FontStyle.Bold);

                        float padding = 8f;
                        float rightMargin = 780f; // <-- Adjust based on your page width

                        // Measure widths
                        float label1Width = e.Graphics.MeasureString(label1, regularFont).Width;
                        float value1Width = e.Graphics.MeasureString(value1, narrowBoldFont).Width;
                        float label2Width = e.Graphics.MeasureString(label2, regularFont).Width;
                        float value2Width = e.Graphics.MeasureString(value2, narrowBoldFont).Width;

                        float textHeight = Math.Max(
                            e.Graphics.MeasureString(label1, regularFont).Height,
                            e.Graphics.MeasureString(value1, narrowBoldFont).Height
                        );

                        float baseTextY = y;

                        float label1X = x;
                        float value1X = label1X + label1Width + padding;
                        float label2X = value1X + value1Width + padding;
                        float value2X = label2X + label2Width + padding;

                        // 🚨 Prevent right overflow for SSCC
                        if (value2X + value2Width > rightMargin)
                        {
                            value2X = rightMargin - value2Width;
                        }

                        // Draw
                        e.Graphics.DrawString(label1, regularFont, Brushes.Black, new PointF(label1X, baseTextY));
                        e.Graphics.DrawString(value1, narrowBoldFont, Brushes.Black, new PointF(value1X, baseTextY));

                        if (!string.IsNullOrWhiteSpace(label2))
                            e.Graphics.DrawString(label2, regularFont, Brushes.Black, new PointF(label2X, baseTextY));

                        if (!string.IsNullOrWhiteSpace(value2))
                            e.Graphics.DrawString(value2, narrowBoldFont, Brushes.Black, new PointF(value2X, baseTextY));

                        y += rowSpacingStandard;
                    }

                    PrintInline(
                            "Customer SKU:", AssemblyItem?.Custitemproduct_Spec_Sku ?? "N/A",
                            //"GTIN:", AssemblyItem?.Custitemcustom_Product_Sepc_Case_Gtin ?? "N/A"
                            "SSCC:", CalculateSSCCWithCheckDigit(labelNoStr) ?? "N/A"
                        );

                    // Draw a thicker horizontal line
                    using (var thickPen = new Pen(Color.Black, 2)) // You can change 2 to 3 or more if needed
                    {
                        e.Graphics.DrawLine(thickPen, x, y, x + 550f, y);
                    }

                    y += 40; // Add more vertical space after the line

                    PrintInline("Date Produced:", labelinfo.Create_Date, "Best Before:", labelinfo.Best_Before);

                    PrintInline(
                            "Cases:", decimal.TryParse(AssemblyItem?.Custitemproduct_Spec_Caseperpallet, out var caseQty)
                                    ? Math.Round(caseQty).ToString("0")
                                    : "Error!",
                        "Pieces:", decimal.TryParse(AssemblyItem?.Custitemproduct_Spec_Qtyperpallet, out var pcsQty)
                                    ? Math.Round(pcsQty).ToString("0")
                                    : "Error!"
                    );


                    PrintInline(
                        "Pallet Weight Net:", $"{AssemblyItem?.Custitemproduct_Spec_Palletwtnetkg} KG"?.ToString() ?? "N/A",
                        "Gross:", $"{AssemblyItem?.Custitemproduct_Spec_Palletwtgrosskg} KG"?.ToString() ?? "Error!"
                        );

                    // PrintRow("SSCC:", labelinfo.Label_No ?? "N/A");


                    // Draw a thicker horizontal line
                    using (var thickPen = new Pen(Color.Black, 2)) // You can change 2 to 3 or more if needed
                    {
                        e.Graphics.DrawLine(thickPen, x, y, x + 550f, y);
                    }

                    y += 10; // Add more vertical space after the line


                    // Helper to crop white margins from left and right of barcode
                    Bitmap CropWhiteSides(Bitmap original)
                    {
                        int width = original.Width;
                        int height = original.Height;

                        int left = 0;
                        int right = width - 1;

                        // Find left boundary
                        for (int x = 0; x < width; x++)
                        {
                            bool hasDarkPixel = false;
                            for (int y = 0; y < height; y++)
                            {
                                var pixel = original.GetPixel(x, y);
                                if (pixel.R < 250 || pixel.G < 250 || pixel.B < 250) // not white
                                {
                                    hasDarkPixel = true;
                                    break;
                                }
                            }
                            if (hasDarkPixel)
                            {
                                left = x;
                                break;
                            }
                        }

                        // Find right boundary
                        for (int x = width - 1; x >= 0; x--)
                        {
                            bool hasDarkPixel = false;
                            for (int y = 0; y < height; y++)
                            {
                                var pixel = original.GetPixel(x, y);
                                if (pixel.R < 250 || pixel.G < 250 || pixel.B < 250) // not white
                                {
                                    hasDarkPixel = true;
                                    break;
                                }
                            }
                            if (hasDarkPixel)
                            {
                                right = x;
                                break;
                            }
                        }

                        int croppedWidth = right - left + 1;
                        var cropped = new Bitmap(croppedWidth, height);
                        using (Graphics g = Graphics.FromImage(cropped))
                        {
                            g.DrawImage(original,
                                new Rectangle(0, 0, croppedWidth, height),
                                new Rectangle(left, 0, croppedWidth, height),
                                GraphicsUnit.Pixel);
                        }

                        return cropped;
                    }


                    // Draws GS1 barcode + readable text below, trimming white margins
                    void PrintGS1BarcodeWithTextBelow(string aiDataRaw, string aiReadable, int barcodeWidth = 550, int barcodeHeight = 100)
                    {
                        // Add FNC1 for GS1-128
                        string gs1EncodedData = "\u00f1" + aiDataRaw;

                        var writer = new ZXing.BarcodeWriter
                        {
                            Format = ZXing.BarcodeFormat.CODE_128,
                            Options = new ZXing.Common.EncodingOptions
                            {
                                Width = barcodeWidth,
                                Height = barcodeHeight,
                                Margin = 0,
                                PureBarcode = true
                            }
                        };

                        using var fullBarcodeImage = writer.Write(gs1EncodedData);
                        using var barcode = CropWhiteSides(fullBarcodeImage); // Trim left/right white

                        float barcodeX = 30f;  // left aligned (no left margin)
                        float barcodeY = y;

                        // Draw barcode
                        e.Graphics.DrawImage(barcode, new PointF(barcodeX, barcodeY));

                        y += barcode.Height + 4;

                        // Draw readable text below barcode
                        using var readableFont = new Font("Consolas", 10, FontStyle.Regular); // Clean monospace font
                        SizeF textSize = e.Graphics.MeasureString(aiReadable, readableFont);

                        float textX = barcodeX; // small left padding
                        float textY = y;

                        e.Graphics.DrawString(aiReadable, readableFont, Brushes.Black, new PointF(textX, textY));

                        y += textSize.Height + 10; // Add spacing after barcode text
                    }
                    // Draws GS1 barcode + readable text below, trimming white margins
                    void PrintGS1BarcodeWithTextBelowSSCC(string aiDataRaw, string aiReadable, int barcodeWidth = 350, int barcodeHeight = 100)
                    {
                        // Add FNC1 for GS1-128 (used in GS1 barcodes)
                        string gs1EncodedData = "\u00f1" + aiDataRaw;

                        var writer = new ZXing.BarcodeWriter
                        {
                            Format = ZXing.BarcodeFormat.CODE_128,
                            Options = new ZXing.Common.EncodingOptions
                            {
                                Width = barcodeWidth,
                                Height = barcodeHeight,
                                Margin = 0,
                                PureBarcode = true
                            }
                        };

                        using var fullBarcodeImage = writer.Write(gs1EncodedData);
                        using var barcode = CropWhiteSides(fullBarcodeImage);

                        float barcodeX = 30f;
                        float barcodeY = y;

                        // Draw barcode
                        e.Graphics.DrawImage(barcode, new PointF(barcodeX, barcodeY));

                        // Load and draw PAP image to the right of barcode
                        string imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "22PAP.png");
                        if (System.IO.File.Exists(imagePath))
                        {
                            using var papImage = Image.FromFile(imagePath);

                            float targetHeight = 100f; // Target height for PAP image
                            float imageRatio = (float)papImage.Width / papImage.Height;
                            float targetWidth = targetHeight * imageRatio;

                            float papX = barcodeX + barcode.Width + 20; // Right of barcode with padding
                            float papY = barcodeY + (barcode.Height - targetHeight) / 2; // Centered vertically with barcode

                            e.Graphics.DrawImage(papImage, new RectangleF(papX, papY, targetWidth, targetHeight));
                        }

                        y += barcode.Height + 4;

                        // Draw readable text below the barcode
                        using var readableFont = new Font("Consolas", 10, FontStyle.Regular);
                        SizeF textSize = e.Graphics.MeasureString(aiReadable, readableFont);

                        float textX = barcodeX;
                        float textY = y;

                        e.Graphics.DrawString(aiReadable, readableFont, Brushes.Black, new PointF(textX, textY));

                        y += textSize.Height + 10;
                    }

                    // Sample usage:
                    string originalGTIN =
                    !string.IsNullOrWhiteSpace(AssemblyItem?.Custitemproduct_Spec_Sku)
                        ? AssemblyItem.Custitemproduct_Spec_Sku
                        : AssemblyItem?.ItemId ?? "";

                    string originalSSCC = CalculateSSCCWithCheckDigit(labelNoStr) ?? "";
                    string originalProdDate = labelinfo?.Expiry ?? "";
                    string originalExpiryDate = labelinfo?.Used_By ?? "";
                    string originalPalletWeight = AssemblyItem?.Custitemproduct_Spec_Palletwtnetkg ?? "";
                    string originalBatchNo = labelNoStr;
                    string originalCase = decimal.TryParse(AssemblyItem?.Custitemproduct_Spec_Caseperpallet, out var caseNo)
                    ? Math.Round(caseNo).ToString("0")
                    : "N/A";

                    string paddedGtin = originalGTIN.PadLeft(1, '0');
                    string gtinValue = "02" + paddedGtin;
                    string prodDateValue = "11" + originalProdDate;
                    string expiryDateValue = "17" + originalExpiryDate;
                    string batchNoValue = "10" + originalBatchNo;
                    string caseNoValue = "37" + originalCase;
                    string palletWeightValue = "3101" + originalPalletWeight;
                    string ssccValue = "00" + originalSSCC;

                    string readablePalletGtin = "(02)" + paddedGtin;
                    string readableProdDate = "(11)" + originalProdDate;
                    string readableExpiryDate = "(17)" + originalExpiryDate;
                    string readableBatchNo = "(10)" + originalBatchNo;
                    string readableCaseNo = "(37)" + originalCase;
                    string readablePalletWeight = "(3101)" + originalPalletWeight;
                    string readableSSCC = "(00)" + originalSSCC;

                    string palletGtinValueWithDate =
                        gtinValue + expiryDateValue + caseNoValue + palletWeightValue;

                    //string palletGtinValueWithDate = gtinValue + expiryDateValue +  caseNoValue + palletWeightValue;
                    string readableGtinWithDate =
                        readablePalletGtin + readableExpiryDate + readableCaseNo + readablePalletWeight;

                    string palletSSCCValueWithDate = ssccValue;
                    string readablePalletSSCCWithDate = readableSSCC;

                    // Print both barcodes with text
                    PrintGS1BarcodeWithTextBelow(palletGtinValueWithDate, readableGtinWithDate);
                    PrintGS1BarcodeWithTextBelowSSCC(palletSSCCValueWithDate, readablePalletSSCCWithDate);

                    // Optional: Horizontal line after barcode + text
                    using (var thickPen = new Pen(Color.Black, 2))
                    {
                        e.Graphics.DrawLine(thickPen, x, y, x + 550f, y);
                    }
                };

                printDoc.Print();
            }
        }
        catch (Exception ex)
        {
            throw new Exception(
                "Lilly Packaging Pallet Label Error: " + ex.Message);
        }
    }

    // ============================================================
    // DRAW ROW
    // ============================================================

    private void DrawRow(
        PrintPageEventArgs e,
        string label,
        string value,
        float x,
        ref float y,
        Font labelFont,
        Font valueFont)
    {
        e.Graphics.DrawString(
            label,
            labelFont,
            Brushes.Black,
            new PointF(x, y));

        e.Graphics.DrawString(
            value,
            valueFont,
            Brushes.Black,
            new PointF(x + 110, y - 3));

        y += 45;
    }

    // ============================================================
    // WRAPPED DESCRIPTION
    // ============================================================

    private void DrawWrappedRow(
        PrintPageEventArgs e,
        string label,
        string value,
        float x,
        ref float y)
    {
        var labelFont = new Font("Arial", 14);
        var valueFont = new Font("Arial", 18, FontStyle.Bold);

        e.Graphics.DrawString(
            label,
            labelFont,
            Brushes.Black,
            new PointF(x, y));

        RectangleF rect =
            new RectangleF(x + 120, y, 420, 1000);

        SizeF size =
            e.Graphics.MeasureString(
                value,
                valueFont,
                new SizeF(420, 1000));

        e.Graphics.DrawString(
            value,
            valueFont,
            Brushes.Black,
            rect);

        y += size.Height + 10;
    }

    // ============================================================
    // INLINE ROW
    // ============================================================

    private void DrawInline(
        PrintPageEventArgs e,
        float x,
        ref float y,
        string label1,
        string value1,
        string label2,
        string value2)
    {
        var labelFont = new Font("Arial", 12);
        var valueFont = new Font("Arial Narrow", 12, FontStyle.Bold);

        float padding = 6f;

        float label1Width =
            e.Graphics.MeasureString(label1, labelFont).Width;

        float value1Width =
            e.Graphics.MeasureString(value1, valueFont).Width;

        float label2X =
            x + label1Width + value1Width + 60;

        e.Graphics.DrawString(
            label1,
            labelFont,
            Brushes.Black,
            new PointF(x, y));

        e.Graphics.DrawString(
            value1,
            valueFont,
            Brushes.Black,
            new PointF(x + label1Width + padding, y));

        e.Graphics.DrawString(
            label2,
            labelFont,
            Brushes.Black,
            new PointF(label2X, y));

        e.Graphics.DrawString(
            value2,
            valueFont,
            Brushes.Black,
            new PointF(label2X + 55, y));

        y += 40;
    }

    // ============================================================
    // GS1 BARCODE
    // ============================================================

    private void PrintGS1Barcode(
        PrintPageEventArgs e,
        ref float y,
        string rawData,
        string readableText,
        int width = 550)
    {
        string gs1Data = "\u00f1" + rawData;

        var writer = new BarcodeWriter
        {
            Format = BarcodeFormat.CODE_128,
            Options = new ZXing.Common.EncodingOptions
            {
                Width = width,
                Height = 90,
                Margin = 0,
                PureBarcode = true
            }
        };

        using var barcode = writer.Write(gs1Data);

        e.Graphics.DrawImage(barcode, new PointF(30, y));

        y += barcode.Height + 5;

        using var textFont =
            new Font("Consolas", 10, FontStyle.Regular);

        e.Graphics.DrawString(
            readableText,
            textFont,
            Brushes.Black,
            new PointF(30, y));

        y += 28;
    }

    // ============================================================
    // SSCC
    // ============================================================

    private string CalculateSSCCWithCheckDigit(
        string ssccWithoutCheckDigit)
    {
        ssccWithoutCheckDigit =
            new string(
                ssccWithoutCheckDigit
                    .Where(char.IsDigit)
                    .ToArray());

        int sum = 0;
        bool multiplyBy3 = true;

        for (int i = ssccWithoutCheckDigit.Length - 1; i >= 0; i--)
        {
            int digit = ssccWithoutCheckDigit[i] - '0';

            sum += digit * (multiplyBy3 ? 3 : 1);

            multiplyBy3 = !multiplyBy3;
        }

        int mod = sum % 10;
        int checkDigit = (10 - mod) % 10;

        return ssccWithoutCheckDigit + checkDigit;
    }
}


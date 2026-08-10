using NetSuite;
using System;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using TPLPLM.Pages;
using ZXing;
using ZXing.Rendering;

namespace TPLPLM.PrintTemplates
{
    public class PalletLabel_150x200_Pepsi_Template
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
                    printDoc.PrinterSettings.Copies = 2;

                    var highRes = printerSettings.PrinterResolutions
                        .Cast<PrinterResolution>()
                        .FirstOrDefault(r => r.Kind == PrinterResolutionKind.High);

                    if (highRes != null)
                        printDoc.DefaultPageSettings.PrinterResolution = highRes;

                    var forcedSize = new PaperSize("Forced6x8", 600, 800);

                    printDoc.DefaultPageSettings.PaperSize = forcedSize;
                    printDoc.DefaultPageSettings.Margins = new Margins(0, 0, 0, 0);

                    printDoc.PrinterSettings = printerSettings;
                    printDoc.DefaultPageSettings = printerSettings.DefaultPageSettings;

                    // Assign print handler
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

                        // === PEPSI HEADER (REPLACES LOGO BLOCK) ===
                        var labelFont = new Font("Arial", 16, FontStyle.Regular);
                        var valueFont = new Font("Arial", 22, FontStyle.Bold);

                        // 1. BIG DESCRIPTION

                        // === 1. DESCRIPTION ===
                        string label = "Product Desc:";
                        string descText = "CANNUCCIA CARTA PEPSI";

                        float labelWidth = e.Graphics.MeasureString(label, labelFont).Width;
                        float valueX = x + labelWidth + 5;

                        // Draw label
                        e.Graphics.DrawString(label, labelFont, Brushes.Black, new PointF(x, y));

                        // Draw value
                        e.Graphics.DrawString(descText, valueFont, Brushes.Black, new PointF(valueX, y));

                        // Move Y
                        float height = Math.Max(
                            e.Graphics.MeasureString(label, labelFont).Height,
                            e.Graphics.MeasureString(descText, valueFont).Height
                        );

                        y += height + 10;

                        // 2. TWO COLUMN INFO
                        void PrintTwoColumnRow(string label1, string value1, string label2, string value2)
                        {
                            var font = new Font("Arial", 12, FontStyle.Bold);

                            float col1X = x;
                            float col2X = x + 300;

                            e.Graphics.DrawString(label1, font, Brushes.Black, new PointF(col1X, y));
                            e.Graphics.DrawString(value1, font, Brushes.Black, new PointF(col1X, y + 12));

                            e.Graphics.DrawString(label2, font, Brushes.Black, new PointF(col2X, y));
                            e.Graphics.DrawString(value2, font, Brushes.Black, new PointF(col2X, y + 12));

                            y += 35;
                        }

                        var infoFont = new Font("Arial", 14, FontStyle.Bold);

                        string line1 = "Materiale: 100% carta";
                        string line2 = "Per contatto con i prodotti alimentari";

                        // Draw aligned with descText (valueX)
                        e.Graphics.DrawString(line1, infoFont, Brushes.Black, new PointF(valueX, y));
                        y += e.Graphics.MeasureString(line1, infoFont).Height;

                        e.Graphics.DrawString(line2, infoFont, Brushes.Black, new PointF(valueX, y));
                        y += e.Graphics.MeasureString(line2, infoFont).Height + 8;

                        // 3. DISTRIBUTOR BLOCK
                        void PrintDistributorBlock(string leftBlock)
                        {
                            var font = new Font("Arial", 9, FontStyle.Regular);
                            float lineHeight = e.Graphics.MeasureString("A", font).Height;

                            string[] lines = leftBlock.Split('\n');

                            foreach (var line in lines)
                            {
                                e.Graphics.DrawString(line.Trim(), font, Brushes.Black, new PointF(valueX, y));
                                y += lineHeight;
                            }

                            y += 5;
                        }

                        string distributorBlock =
                        @"Distribuito da: Mana'o Lab Srl
                        Via Gamboloita, 4-20139 Milano - ITA
                        info@manaolab.it - www.manaolab.it
                        Num. di lotto: 304PBI25OG
                        Made in UK";

                        PrintDistributorBlock(distributorBlock);


                      
                        void PrintRow(string label, string value, Font overrideFont = null)
                        {
                            e.Graphics.DrawString(label, font, Brushes.Black, new PointF(x, y));
                            e.Graphics.DrawString(value, overrideFont ?? boldFont, Brushes.Black, new PointF(x + 100, y));
                            y += rowSpacingStandard;
                        }

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
                            "", AssemblyItem?.Custitemproduct_Spec_Sku ?? "",
                            //"GTIN:", AssemblyItem?.Custitemcustom_Product_Sepc_Case_Gtin ?? "N/A"
                            "SSCC:", CalculateSSCCWithCheckDigit(labelNoStr) ?? "N/A"
                        );

                        // Draw a thicker horizontal line
                        using (var thickPen = new Pen(Color.Black, 2)) // You can change 2 to 3 or more if needed
                        {
                            e.Graphics.DrawLine(thickPen, x, y, x + 550f, y);
                        }

                        y += 20; // Add more vertical space after the line

                      
                        // === TPL REF + DATAMATRIX (NO LOGO) ===

                        string tpllabel = "Our Ref:";
                        string value = AssemblyItem?.ItemId ?? "N/A";

                        // --- Generate DataMatrix ---
                        int barcodeSize = 60;

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

                        // --- Use SAME font style as other labels ---
                        Font labelFontUsed = labelFont; // reuse your existing label font
                        Font valueFontUsed = valueFont;

                        // --- Measure text ---
                        float ourRefLabelWidth = e.Graphics.MeasureString(tpllabel, labelFontUsed).Width;
                        float ourRefLabelHeight = e.Graphics.MeasureString(tpllabel, labelFontUsed).Height;

                        // --- Layout ---
                        float startX = x;
                        float startY = y;

                        // Text baseline centered with barcode
                        float textY = startY + (barcodeSize - ourRefLabelHeight) / 2;

                        // --- Draw "Our Ref:" (same style as others) ---
                        e.Graphics.DrawString(tpllabel, labelFontUsed, Brushes.Black, new PointF(startX, textY));

                        // --- Value next to label ---
                        float ourRefValueX = startX + ourRefLabelWidth + 5;
                        e.Graphics.DrawString(value, valueFontUsed, Brushes.Black, new PointF(ourRefValueX, textY));

                        // --- DataMatrix after text ---
                        float barcodeX = ourRefValueX + e.Graphics.MeasureString(value, valueFontUsed).Width + 15;
                        float barcodeY = startY;

                        e.Graphics.DrawImage(matrixBitmap, new RectangleF(barcodeX, barcodeY, barcodeSize, barcodeSize));

                        // --- Move Y down ---
                        y += barcodeSize + 10;

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

                        //string palletGtinValueWithDate =
                        //       gtinValue + expiryDateValue + caseNoValue + palletWeightValue;
                        string palletGtinValueWithDate =
                              gtinValue + expiryDateValue + caseNoValue + palletWeightValue;



                        //string palletGtinValueWithDate = gtinValue + expiryDateValue +  caseNoValue + palletWeightValue;
                        //string readableGtinWithDate = readablePalletGtin + readableExpiryDate + readableCaseNo + readablePalletWeight;
                        string readableGtinWithDate = readablePalletGtin + readableExpiryDate + readableCaseNo + readablePalletWeight;

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
                throw new Exception("Standard Pallet Label Error: " + ex.Message);
            }
        }

        private string CalculateSSCCWithCheckDigit(string input)
        {
            input = new string(input.Where(char.IsDigit).ToArray());

            int sum = 0;
            bool alt = true;

            for (int i = input.Length - 1; i >= 0; i--)
            {
                int d = input[i] - '0';
                sum += d * (alt ? 3 : 1);
                alt = !alt;
            }

            int mod = sum % 10;
            return input + ((10 - mod) % 10);
        }
    }
}
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
    public class PalletLabel_150x200_Nestle_Template
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
                        float rowSpacing = 60f;
                        float rowSpacingStandard = 60f;
                        float rowSpacingTight = 45f;
                        float rowSpacingLoose = 75f;
                        float rowSpacingAfterBarcode = 65f;
                        float rowSpacingExtra = 80f;

                        var font = new Font("Arial", 15);          // was 14
                        var largeFont = new Font("Arial", 21, FontStyle.Bold); // was 20
                        var boldFont = new Font("Arial", 12, FontStyle.Bold);  // was 12


                        if (!string.IsNullOrEmpty(Address))
                        {
                            float columnWidth = 250f;
                            float verticalSpacing = 8f;
                            float sectionSpacing = 22f;

                            var labelFont = new Font("Arial", 12, FontStyle.Bold);
                            var companyFont = new Font("Arial", 12);
                            var addressFont = new Font("Arial", 12);

                            // LEFT SIDE
                            float leftX = x;
                            float currentY = y;

                            e.Graphics.DrawString("From:", labelFont, Brushes.Black, new PointF(leftX, currentY));
                            currentY += labelFont.GetHeight(e.Graphics) + verticalSpacing;

                            string companyName = "Transcend Packaging LTD";
                            e.Graphics.DrawString(companyName, companyFont, Brushes.Black, new PointF(leftX, currentY));
                            currentY += companyFont.GetHeight(e.Graphics) + verticalSpacing;

                            // Measure left address
                            SizeF leftSize = e.Graphics.MeasureString(Address, addressFont, (int)columnWidth);
                            e.Graphics.DrawString(Address, addressFont, Brushes.Black,
                                                  new RectangleF(leftX, currentY, columnWidth, leftSize.Height));

                            float leftHeight = currentY + leftSize.Height;


                            // RIGHT SIDE
                            float rightX = x + columnWidth + 50f;
                            float rightY = y;

                            e.Graphics.DrawString("To:", labelFont, Brushes.Black, new PointF(rightX, rightY));
                            rightY += labelFont.GetHeight(e.Graphics) + verticalSpacing;

                            string shipToAddress = AssemblyItem?.Custitemitem_Shipping_Address ?? "N/A";

                            SizeF rightSize = e.Graphics.MeasureString(shipToAddress, addressFont, (int)columnWidth);
                            e.Graphics.DrawString(shipToAddress, addressFont, Brushes.Black,
                                                  new RectangleF(rightX, rightY, columnWidth, rightSize.Height));

                            float rightHeight = rightY + rightSize.Height;


                            // Use whichever side is taller
                            y = Math.Max(leftHeight, rightHeight) + sectionSpacing;

                            // Horizontal line
                            using (var thickPen = new Pen(Color.Black, 2))
                            {
                                e.Graphics.DrawLine(thickPen, x, y, x + columnWidth * 2 + 50f, y);
                            }

                            y += sectionSpacing;
                        }

                        void PrintRow(string label, string value, Font overrideFont = null)
                        {
                            e.Graphics.DrawString(label, font, Brushes.Black, new PointF(x, y));
                            e.Graphics.DrawString(value, overrideFont ?? boldFont, Brushes.Black, new PointF(x + 100, y));
                            y += rowSpacingStandard;
                        }
                        void PrintTwoColumnRow(string label1, string value1, string label2, string value2)
                        {
                            var font = new Font("Arial", 13, FontStyle.Regular);
                            var boldFont = new Font("Arial", 13, FontStyle.Bold);
                            float spacing = 10f;

                            float col1X = x;
                            float col2X = x + 175;  // Adjust as needed based on width of first column

                            // Column 1
                            e.Graphics.DrawString(label1, font, Brushes.Black, new PointF(col1X, y));
                            e.Graphics.DrawString(value1, boldFont, Brushes.Black, new PointF(col1X, y + 25));

                            // Column 2
                            e.Graphics.DrawString(label2, font, Brushes.Black, new PointF(col2X, y));
                            e.Graphics.DrawString(value2, boldFont, Brushes.Black, new PointF(col2X, y + 25));

                            y += rowSpacingStandard; // Move to next line
                        }

                        void PrintThreeColumnRow(string label1, string value1, string label2, string value2, string label3, string value3)
                        {
                            var font = new Font("Arial", 13, FontStyle.Regular);
                            var boldFont = new Font("Arial", 13, FontStyle.Bold);
                            float spacing = 10f;

                            float col1X = x;
                            float col2X = x + 175;  // Adjust as needed based on width of first column
                            float col3X = x + 350;  // Adjust for proper spacing

                            // Column 1
                            e.Graphics.DrawString(label1, font, Brushes.Black, new PointF(col1X, y));
                            e.Graphics.DrawString(value1, boldFont, Brushes.Black, new PointF(col1X, y + 25));

                            // Column 2
                            e.Graphics.DrawString(label2, font, Brushes.Black, new PointF(col2X, y));
                            e.Graphics.DrawString(value2, boldFont, Brushes.Black, new PointF(col2X, y + 25));

                            // Column 3
                            e.Graphics.DrawString(label3, font, Brushes.Black, new PointF(col3X, y));
                            e.Graphics.DrawString(value3, boldFont, Brushes.Black, new PointF(col3X, y + 25));

                            y += rowSpacingStandard; // Move to next line
                        }
                        void PrintThreeColumnRowWithWrappedDescription(string label1, string value1, string label2, string value2, string label3, string value3, float descriptionWidth = 150f)
                        {
                            var font = new Font("Arial", 13, FontStyle.Regular);
                            var boldFont = new Font("Arial", 13, FontStyle.Bold);
                            float spacing = 5f;

                            float col1X = x;
                            float col2X = x + 175;
                            float col3X = x + 350;

                            // Column 1 (no wrapping)
                            e.Graphics.DrawString(label1, font, Brushes.Black, new PointF(col1X, y));
                            e.Graphics.DrawString(value1, boldFont, Brushes.Black, new PointF(col1X, y + 25));

                            // Column 2 (wrap only this)
                            e.Graphics.DrawString(label2, font, Brushes.Black, new PointF(col2X, y));
                            SizeF descriptionSize = e.Graphics.MeasureString(value2, boldFont, (int)descriptionWidth);
                            e.Graphics.DrawString(value2, boldFont, Brushes.Black,
                            new RectangleF(col2X, y + 25, descriptionWidth, descriptionSize.Height));

                            // Column 3 (align top of description)
                            e.Graphics.DrawString(label3, font, Brushes.Black, new PointF(col3X, y));
                            e.Graphics.DrawString(value3, boldFont, Brushes.Black, new PointF(col3X, y + 25));

                            // Increase Y only by the description height if it’s taller than one line
                            float rowHeight = rowSpacingStandard;
                            y += rowHeight + spacing;
                        }

                        PrintThreeColumnRowWithWrappedDescription(
                            "Item:", AssemblyItem?.Custitemproduct_Spec_Sku ?? "N/A",
                            "Description:", AssemblyItem?.Description ?? "N/A",
                            "WO No:", labelinfo.Work_Order?.Substring(2)
                        );

                        PrintThreeColumnRow(
                           "GTIN:", AssemblyItem?.Custitemproduct_Spec_Gtin ?? "0000000000000",
                           "", "",
                           "Count:", AssemblyItem?.Custitemproduct_Spec_Caseperpallet != null
                                    ? Math.Ceiling(Convert.ToDecimal(AssemblyItem.Custitemproduct_Spec_Caseperpallet)).ToString("0")
                                    : "N/A"

                        );
                        PrintThreeColumnRow(
                          "Prod Date:", labelinfo.Create_Date,
                          "", "",
                          "Pallet Quantity:", AssemblyItem?.Custitemproduct_Spec_Qtyperpallet != null
                            ? Math.Ceiling(Convert.ToDecimal(AssemblyItem.Custitemproduct_Spec_Qtyperpallet)).ToString("0")
                            : "N/A"

                        );

                        PrintThreeColumnRow(
                            "Variant:", "01" ?? "N/A",
                            "", "",
                            "TPL Ref:", AssemblyItem?.ItemId ?? "N/A"
                        );
                        PrintThreeColumnRow(
                            "SSCC:", CalculateSSCCWithCheckDigit(labelNoStr),
                             "", "",
                            "Country of Origin:", "UK"
                        );


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
                            float barcodeLeftMargin = 50f;
                            float barcodeX = barcodeLeftMargin;  // left aligned (no left margin)
                            float barcodeY = y;

                            // Draw barcode
                            e.Graphics.DrawImage(barcode, new PointF(barcodeX, barcodeY));

                            y += barcode.Height + 4;

                            // Draw readable text below barcode
                            using var readableFont = new Font("Consolas", 12, FontStyle.Regular); // was 10
                            SizeF textSize = e.Graphics.MeasureString(aiReadable, readableFont);

                            float textX = barcodeX; // small left padding
                            float textY = y;

                            e.Graphics.DrawString(aiReadable, readableFont, Brushes.Black, new PointF(textX, textY));

                            y += textSize.Height + 10; // Add spacing after barcode text
                        }
                        // Draws GS1 barcode + readable text below, trimming white margins
                        void PrintGS1BarcodeWithTextBelowSSCC(string aiDataRaw, string aiReadable, int barcodeWidth = 500, int barcodeHeight = 100)
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

                            float barcodeLeftMargin = 50f;
                            float barcodeX = barcodeLeftMargin;
                            float barcodeY = y;

                            // Draw barcode
                            e.Graphics.DrawImage(barcode, new PointF(barcodeX, barcodeY));

                            // Load and draw PAP image to the right of barcode


                            y += barcode.Height + 4;

                            // Draw readable text below the barcode
                            using var readableFont = new Font("Consolas", 12, FontStyle.Regular);
                            SizeF textSize = e.Graphics.MeasureString(aiReadable, readableFont);

                            float textX = barcodeX;
                            float textY = y;

                            e.Graphics.DrawString(aiReadable, readableFont, Brushes.Black, new PointF(textX, textY));

                            y += textSize.Height + 10;
                        }

                        // Sample usage:

                        string originalGTIN = AssemblyItem?.Custitemproduct_Spec_Gtin ?? "0000000000000";
                        string originalSSCC = CalculateSSCCWithCheckDigit(labelNoStr) ?? "";
                        //string originalSSCC = labelNoStr;
                        string originalProdDate = labelinfo.Expiry ?? "";
                        string originalExpiryDate = labelinfo.Used_By ?? "";
                        string originalPalletWeight = AssemblyItem?.Custitemproduct_Spec_Palletwtnetkg ?? "";
                        string originalBatchNo = labelinfo.Work_Order?.Substring(2);
                        string originalCase =
                        decimal.TryParse(AssemblyItem?.Custitemproduct_Spec_Caseperpallet, out var caseNo)
                            ? Math.Round(caseNo).ToString("00")   // <-- two digits
                            : "00";

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

                        // Insert FNC1 after variable-length fields (37, 10, etc.)
                        //string palletGtinValueWithDate = gtinValue + caseNoValue + "\u00f1" + batchNoValue;
                        string palletGtinValueWithDate =
                        gtinValue.All(char.IsDigit)
                            ? gtinValue + caseNoValue + batchNoValue
                            : gtinValue + caseNoValue + "\u00f1" + batchNoValue;

                        string readableGtinWithDate = readablePalletGtin + readableCaseNo + readableBatchNo;

                        string palletSSCCValueWithDate = ssccValue + prodDateValue;
                        string readablePalletSSCCWithDate = readableSSCC + readableProdDate;
                        //string palletSSCCValueWithDate = ssccValue + prodDateValue + caseNoValue;
                        //string readablePalletSSCCWithDate = readableSSCC + readableProdDate + readableCaseNo;

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

using NetSuite;
using System;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using TPLPLM.Pages;
using ZXing;

public class CaseLabel_SBUX_Template
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
                string labelNoStr = currentLabelNo.ToString().PadLeft(labelinfo.Label_No.Length, '0');

                var printDoc = new PrintDocument();
                var printerSettings = new PrinterSettings { PrinterName = SelectedPrinter };

                if (!printerSettings.IsValid)
                    throw new Exception($"Printer '{SelectedPrinter}' is not valid.");

                printDoc.PrinterSettings = printerSettings;
                printDoc.DefaultPageSettings.Landscape = true;

                var highRes = printerSettings.PrinterResolutions
                    .Cast<PrinterResolution>()
                    .FirstOrDefault(r => r.Kind == PrinterResolutionKind.High);

                if (highRes != null)
                    printDoc.DefaultPageSettings.PrinterResolution = highRes;

                // Assign print handler
                printDoc.PrintPage += (sender, e) =>
                {
                    e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                    float x = 10f, y = 10f;
                    float rowSpacing = 45f;
                    float rowSpacingStandard = 35f;
                    float rowSpacingTight = 25f;
                    float rowSpacingLoose = 45f;
                    float rowSpacingAfterBarcode = 65f;
                    float rowSpacingExtra = 55f;

                    var font = new Font("Arial", 10);

                    string imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "mono.png");
                    string imagePAPPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "PAP Label.png");

                    if (System.IO.File.Exists(imagePath) && System.IO.File.Exists(imagePAPPath))
                    {
                        using var img = Image.FromFile(imagePath);
                        using var imgPAP = Image.FromFile(imagePAPPath);

                        float ratio = img.Height / (float)img.Width;
                        float ratioPAP = imgPAP.Height / (float)imgPAP.Width;

                        int logoWidth = 200, logoHeight = (int)(logoWidth * ratio);
                        int logoPAPWidth = 150, logoPAPHeight = (int)(logoPAPWidth * ratioPAP); // Enlarged PAP logo
                        float topY = y;

                        // Draw main logo
                        e.Graphics.DrawImage(img, new RectangleF(x, topY, logoWidth, logoHeight));

                        // Generate DataMatrix
                        var writer = new ZXing.BarcodeWriter
                        {
                            Format = ZXing.BarcodeFormat.DATA_MATRIX,
                            Options = new ZXing.Common.EncodingOptions
                            {
                                Width = 50,
                                Height = 50,
                                Margin = 0,
                                PureBarcode = true
                            }
                        };

                        Bitmap barcodeBitmap = writer.Write(labelNoStr);

                        float barcodeX = x + logoWidth + 20;
                        float barcodeY = topY;
                        e.Graphics.DrawImage(barcodeBitmap, new PointF(barcodeX, barcodeY));

                        // Draw PAP Logo right after barcode
                        float papX = barcodeX + barcodeBitmap.Width + 20;
                        float papY = topY;
                        e.Graphics.DrawImage(imgPAP, new RectangleF(papX, papY, logoPAPWidth, logoPAPHeight));

                        // Draw ItemId next to PAP logo
                        var itemId = AssemblyItem?.ItemId ?? "N/A";
                        var itemFont = new Font("Arial", 10, FontStyle.Bold); // Smaller font

                        float itemTextX = papX + logoPAPWidth + 10;
                        float itemTextY = papY + (logoPAPHeight - e.Graphics.MeasureString(itemId, itemFont).Height) / 2; // vertically centered with PAP image
                        e.Graphics.DrawString(itemId, itemFont, Brushes.Black, new PointF(itemTextX, itemTextY));

                        // Move Y down for next row
                        float maxHeight = Math.Max(logoHeight, Math.Max(barcodeBitmap.Height, logoPAPHeight));
                        y = topY + maxHeight + 10;
                    }


                    void PrintRow(string label, string value, Font overrideFont = null)
                    {
                        var labelFont = new Font("Arial", 10, FontStyle.Regular);
                        var valueFont = overrideFont ?? new Font("Arial", 10, FontStyle.Bold);

                        e.Graphics.DrawString(label, labelFont, Brushes.Black, new PointF(x, y));
                        e.Graphics.DrawString(value, valueFont, Brushes.Black, new PointF(x + 100, y));
                        y += rowSpacingStandard;
                    }

                    void PrintTwoColumnRow(string label1, string value1, string label2, string value2)
                    {
                        var font = new Font("Arial", 10, FontStyle.Regular);
                        var boldFont = new Font("Arial", 10, FontStyle.Bold);
                        float spacing = 10f;

                        float col1X = x;
                        float col2X = x + 175;  // Adjust as needed based on width of first column

                        // Column 1
                        e.Graphics.DrawString(label1, font, Brushes.Black, new PointF(col1X, y));
                        e.Graphics.DrawString(value1, boldFont, Brushes.Black, new PointF(col1X, y + 15));

                        // Column 2
                        e.Graphics.DrawString(label2, font, Brushes.Black, new PointF(col2X, y));
                        e.Graphics.DrawString(value2, boldFont, Brushes.Black, new PointF(col2X, y + 15));

                        y += rowSpacingStandard; // Move to next line
                    }

                    void PrintThreeColumnRow(string label1, string value1, string label2, string value2, string label3, string value3)
                    {
                        var font = new Font("Arial", 10, FontStyle.Regular);
                        var boldFont = new Font("Arial", 10, FontStyle.Bold);
                        float spacing = 10f;

                        float col1X = x;
                        float col2X = x + 175;  // Adjust as needed based on width of first column
                        float col3X = x + 350;  // Adjust for proper spacing

                        // Column 1
                        e.Graphics.DrawString(label1, font, Brushes.Black, new PointF(col1X, y));
                        e.Graphics.DrawString(value1, boldFont, Brushes.Black, new PointF(col1X, y + 15));

                        // Column 2
                        e.Graphics.DrawString(label2, font, Brushes.Black, new PointF(col2X, y));
                        e.Graphics.DrawString(value2, boldFont, Brushes.Black, new PointF(col2X, y + 15));

                        // Column 3
                        e.Graphics.DrawString(label3, font, Brushes.Black, new PointF(col3X, y));
                        e.Graphics.DrawString(value3, boldFont, Brushes.Black, new PointF(col3X, y + 15));

                        y += rowSpacingStandard; // Move to next line
                    }
                    PrintTwoColumnRow(
                        "Item:", AssemblyItem?.Custitemproduct_Spec_Sku ?? "N/A",
                        "Description:", AssemblyItem?.Description ?? "N/A"
                    );


                    PrintThreeColumnRow(
                        "MFG Site ID:", "GBTRAPACTYDYYST",
                        "Prod Date:", labelinfo.Create_Date,
                        "Case No:", labelNoStr
                    );

                    PrintThreeColumnRow(
                        "Case Count:", AssemblyItem?.Custitemproduct_Spec_Qtyperouter?.ToString() ?? "N/A",
                        "Net Weight:", AssemblyItem?.Custitemproduct_Spec_Casewtnet != null
                            ? $"{AssemblyItem.Custitemproduct_Spec_Casewtnet} KG"
                            : "N/A",
                         "Gross Weight:", AssemblyItem?.Custitemproduct_Spec_Casewtgrosskg != null
                            ? $"{AssemblyItem.Custitemproduct_Spec_Casewtgrosskg} KG"
                            : "N/A"
                    );
                    PrintThreeColumnRow(
                       "Country of Origin:", "UK",
                       "GTIN:", AssemblyItem?.Custitemproduct_Spec_Gtin ?? "0000000000000",
                        "WO No:", labelinfo.Work_Order?.Substring(2));

                    // Draw a thicker horizontal line
                    using (var thickPen = new Pen(Color.Black, 2)) // You can change 2 to 3 or more if needed
                    {
                        e.Graphics.DrawLine(thickPen, x, y, x + 600f, y);
                    }

                    y += 10; // Add more vertical space after the line

                    void PrintGS1BarcodeWithTextBelow(string aiDataRaw, string aiReadable)
                    {
                        // Prepare GS1-128 encoded data (FNC1)
                        string gs1EncodedData = "\u00f1" + aiDataRaw;

                        // Generate the GS1-128 barcode
                        var writer = new ZXing.BarcodeWriter
                        {
                            Format = ZXing.BarcodeFormat.CODE_128,
                            Options = new ZXing.Common.EncodingOptions
                            {
                                Width = 500,
                                Height = 100,
                                Margin = 0,
                                PureBarcode = true
                            }
                        };

                        using var barcode = writer.Write(gs1EncodedData);

                        float barcodeX = x + 10;
                        float barcodeY = y;

                        // Draw barcode
                        e.Graphics.DrawImage(barcode, new PointF(barcodeX, barcodeY));

                        // Move down below barcode
                        y += barcode.Height + 2;

                        // Draw human-readable text centered under barcode
                        var readableFont = new Font("Arial", 10, FontStyle.Bold);
                        float textWidth = e.Graphics.MeasureString(aiReadable, readableFont).Width;
                        float textX = barcodeX + (barcode.Width - textWidth) / 2;
                        e.Graphics.DrawString(aiReadable, readableFont, Brushes.Black, new PointF(textX, y));

                        // Move down for next row
                        y += e.Graphics.MeasureString(aiReadable, readableFont).Height + rowSpacingTight;
                    }



                    string originalGtin = AssemblyItem?.Custitemproduct_Spec_Gtin ?? "0000000000000";
                    string originalProdDate = labelinfo.Expiry;
                    string originalBatchNo = labelinfo.Work_Order?.Substring(2);

                    string paddedGtin = originalGtin.PadLeft(13, '0');
                    string caseGtinValue = "01" + paddedGtin;
                    string prodDateValue = "11" + originalProdDate;
                    string batchNoValue = "10" + originalBatchNo;

                    string readableCaseGtin = "(01)" + paddedGtin;
                    string readableProdDate = "(11)" + originalProdDate;
                    string readableBatchNo = "(10)" + originalBatchNo;

                    string caseGtinValueWithDate = caseGtinValue + prodDateValue + batchNoValue;
                    string readableCaseGtinWithDate = readableCaseGtin + " " + readableProdDate + " " + readableBatchNo;

                    PrintGS1BarcodeWithTextBelow(caseGtinValueWithDate, readableCaseGtinWithDate);

                    // Optional: Horizontal line after barcode + text
                    using (var thickPen = new Pen(Color.Black, 2))
                    {
                        e.Graphics.DrawLine(thickPen, x, y, x + 600f, y);
                    }
                   
                };

                printDoc.Print();
            }
        }
        catch (Exception ex)
        {
            throw new Exception("SBUX Template Error: " + ex.Message);
        }
    }
}
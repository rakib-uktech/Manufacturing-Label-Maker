using NetSuite;
using System;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using TPLPLM.Pages;
using ZXing;
using ZXing.Rendering;

public class CaseLabel_100x100_RR_Template
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

                    // Draw company name (slightly smaller font so barcode fits beside it)
                    using (Font companyFont = new Font("Arial", 16, FontStyle.Bold)) // Smaller than 20
                    {
                        string companyName = "R & R Packaging";
                        SizeF textSize = e.Graphics.MeasureString(companyName, companyFont);

                        float topY = y;

                        // Draw company name on the left
                        e.Graphics.DrawString(companyName, companyFont, Brushes.Black, new PointF(x, topY));

                        // Barcode writer remains the same
                        string gs1Data = "\u00f1" + labelNoStr;
                        var writer = new BarcodeWriter
                        {
                            Format = BarcodeFormat.CODE_128,
                            Options = new ZXing.Common.EncodingOptions
                            {
                                Width = 320,
                                Height = 60,
                                Margin = 10,
                                PureBarcode = true // Removes human-readable text
                            },
                            Renderer = new BitmapRenderer()
                        };

                        Bitmap barcodeBitmap = writer.Write(gs1Data);
                        barcodeBitmap.SetResolution(203, 203);
                        // Draw barcode on the same line, next to company name
                        float barcodeX = x + textSize.Width + 20; // Add spacing after company name
                        e.Graphics.DrawImageUnscaled(barcodeBitmap, (int)barcodeX, (int)topY);
                        // Update y position after tallest element
                        y = topY + Math.Max(textSize.Height, barcodeBitmap.Height) - 20;
                    }


                    // Replace address with website
                    string website = "http://www.rrpackaging.co.uk";
                    using (Font websiteFont = new Font("Arial", 11, FontStyle.Regular)) // Slightly larger font
                    {
                        e.Graphics.DrawString(website, websiteFont, Brushes.Black, new PointF(x, y));
                    }

                    // Move y down
                    y += 25;

                    // Draw a thicker horizontal line
                    using (var thickPen = new Pen(Color.Black, 2)) // Adjust thickness if needed
                    {
                        e.Graphics.DrawLine(thickPen, x, y, x + 400f, y);
                    }

                    y += 10; // Extra spacing after line

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

                    // Description should come right after Client
                    string desc = AssemblyItem?.Description ?? "N/A";
                    PrintWrappedRow("Description:", desc);

                    // Replace "Order Code" with "Customer SKU"
                    PrintRow("Customer SKU:", AssemblyItem?.Custitemproduct_Spec_Sku ?? "N/A");


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
                       string codeLabel, string codeValue,
                       string refLabel, string refValue,
                       string caseLabel, string caseValue)
                    {
                        var regularFont = new Font("Arial", 11, FontStyle.Regular);
                        var boldFont = new Font("Arial", 11, FontStyle.Bold);
                        float padding = 6f;
                        float linePadding = 10f;
                        float tableWidth = 400f;

                        float imageHeight = 60f; // Increased image height
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
                        float codeLabelWidth = e.Graphics.MeasureString(codeLabel, regularFont).Width;
                        float refLabelWidth = e.Graphics.MeasureString(refLabel, regularFont).Width;
                        float caseLabelWidth = e.Graphics.MeasureString(caseLabel, regularFont).Width;
                        float maxLabelWidth = Math.Max(Math.Max(dateLabelWidth, codeLabelWidth), Math.Max(refLabelWidth, caseLabelWidth));

                        float labelAreaWidth = maxLabelWidth + linePadding;
                        float valueAreaWidth = tableWidth - labelAreaWidth - padding * 2;

                        float verticalLineX = x + labelAreaWidth;
                        float valueX = verticalLineX + padding;
                        float verticalLineTop = y;

                        float rowSpacing = 5f;

                        // --- Draw top horizontal line ---
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

                        // --- Row 2: Product Code ---
                        float textY2 = textY1 + rowHeight + rowSpacing;
                        e.Graphics.DrawString(codeLabel, regularFont, Brushes.Black, new PointF(x, textY2));
                        e.Graphics.DrawString(codeValue, boldFont, Brushes.Black, new PointF(valueX, textY2));

                        // --- Draw PAP image slightly to the right and vertically centered across first two rows ---
                        if (papImage != null)
                        {
                            float imageX = valueX + valueAreaWidth - imageWidth - 15f; // Moved 15px to the left
                            float twoRowsHeight = (rowHeight * 3) + rowSpacing;
                            float imageY = y + (twoRowsHeight - imageHeight) / 2f + 10f; // Moved 10px down
                            e.Graphics.DrawImage(papImage, new RectangleF(imageX, imageY, imageWidth, imageHeight));
                            papImage.Dispose();
                        }

                        // --- Row 3: Our Ref ---
                        float textY3 = textY2 + rowHeight + rowSpacing;
                        e.Graphics.DrawString(refLabel, regularFont, Brushes.Black, new PointF(x, textY3));
                        e.Graphics.DrawString(refValue, boldFont, Brushes.Black, new PointF(valueX, textY3));

                        // --- Row 4: Case Number ---
                        float textY4 = textY3 + rowHeight + rowSpacing;
                        e.Graphics.DrawString(caseLabel, regularFont, Brushes.Black, new PointF(x, textY4));
                        e.Graphics.DrawString(caseValue, boldFont, Brushes.Black, new PointF(valueX, textY4));

                        // --- Draw vertical and bottom horizontal lines ---
                        float verticalLineBottom = textY4 + rowHeight;

                        using (var pen = new Pen(Color.Black, 2))
                        {
                            e.Graphics.DrawLine(pen, verticalLineX, verticalLineTop, verticalLineX, verticalLineBottom);
                            e.Graphics.DrawLine(pen, x, verticalLineBottom + 3, x + tableWidth, verticalLineBottom + 3);
                        }

                        // Update y for next section
                        y = verticalLineBottom + rowSpacing + 6;
                    }

                    // Usage
                    PrintDateWithProductCodeAndRefTable(
                        "Date Produced:", labelinfo.Create_Date,
                        "Product Code:", AssemblyItem?.Custitemproduct_Spec_Productcode ?? "N/A",
                        "Our Ref:", AssemblyItem?.ItemId ?? "N/A",
                        "Case Number:", labelNoStr
                    );

                };

                printDoc.Print();
            }
        }
        catch (Exception ex)
        {
            throw new Exception("R&R Template Error: " + ex.Message);
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
        DrawRow("Product Code:", item?.Custitemproduct_Spec_Productcode ?? "N/A");
        DrawRow("Our Ref:", item?.ItemId ?? "N/A");
        DrawRow("Case Number:", labelNo);

        using var pen2 = new Pen(Color.Black, 2);
        e.Graphics.DrawLine(pen2, x, currentY, x + tableWidth, currentY);

        y = currentY + 5;
    }
}
using NetSuite;
using System;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using TPLPLM.Pages;
using ZXing;

public class CaseLabel_PEPSI_Template
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

                printDoc.PrintPage += (sender, e) =>
                {
                    e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                    float x = 10f, y = 10f;
                    float rowSpacing = 25f;
                    float rowSpacingStandard = 20f;
                    float rowSpacingTight = 15f;
                    float rowSpacingLoose = 25f;
                    float rowSpacingAfterBarcode = 30f;
                    float rowSpacingExtra = 30f;

                    var font = new Font("Arial", 10);

                    string imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "mono.png");
                    string imagePAPPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "pap_images.png");

                    // === TOP ROW: BARCODE (LEFT) + ITEM ID (AFTER BARCODE) ===

                    // Generate Code128 barcode
                    var writerTop = new ZXing.BarcodeWriter
                    {
                        Format = ZXing.BarcodeFormat.CODE_128,
                        Options = new ZXing.Common.EncodingOptions
                        {
                            Width = 275,
                            Height = 30,
                            Margin = 5,
                            PureBarcode = true
                        }
                    };

                    using var topBarcode = writerTop.Write(labelNoStr);

                    // Align with table
                    float barcodeX = x - 10;
                    float barcodeY = y;

                    // Draw barcode
                    e.Graphics.DrawImage(topBarcode, new PointF(barcodeX, barcodeY));

                    // ✅ Single font for both label + value
                    var textFont = new Font("Arial", 12, FontStyle.Bold);

                    // Text values
                    string labelText = "TPL REF:";
                    string valueText = AssemblyItem?.ItemId ?? "N/A";

                    // Start text AFTER barcode
                    float spacingAfterBarcode = 15f;
                    float textStartX = barcodeX + topBarcode.Width + spacingAfterBarcode + 50;

                    // Vertical alignment (center with barcode)
                    float textHeight = e.Graphics.MeasureString(labelText, textFont).Height;
                    float textY = barcodeY + (topBarcode.Height - textHeight) / 2;

                    // Draw label
                    e.Graphics.DrawString(labelText, textFont, Brushes.Black, new PointF(textStartX, textY));

                    // Draw value (same line)
                    float labelWidth = e.Graphics.MeasureString(labelText, textFont).Width;
                    e.Graphics.DrawString(valueText, textFont, Brushes.Black, new PointF(textStartX + labelWidth + 5, textY));

                    // Move Y down
                    y += topBarcode.Height + 10;

                    // === SECOND LINE: CENTERED "Cod. Ard" ===

                    // Text
                    string codText = "Cod. Art: 70-09548";

                    // Bigger bold font
                    var codFont = new Font("Arial", 18, FontStyle.Bold);

                    // Measure for centering
                    float codTextWidth = e.Graphics.MeasureString(codText, codFont).Width;

                    // Use printable area for true center
                    float pageWidth = e.PageSettings.PrintableArea.Width;
                    float pageLeft = e.PageSettings.PrintableArea.Left;

                    // Center horizontally
                    float codX = (pageLeft + (pageWidth - codTextWidth) / 2)+50;
                    float codY = y;

                    // Draw text
                    e.Graphics.DrawString(codText, codFont, Brushes.Black, new PointF(codX, codY));

                    // Move Y down after this line
                    y += e.Graphics.MeasureString(codText, codFont).Height -2;

                    // === THIRD LINE: LEFT-ALIGNED DESCRIPTION ===

                    string descText = "CANNUCCIA CARTA PEPSI";

                    // Bigger + bold font
                    var descFont = new Font("Arial", 30, FontStyle.Bold); // increase size as needed

                    // ⬅️ align with your table (same as other rows)
                    float descX = x;
                    float descY = y;

                    // Draw text
                    e.Graphics.DrawString(descText, descFont, Brushes.Black, new PointF(descX, descY));

                    // Move Y down
                    y += e.Graphics.MeasureString(descText, descFont).Height - 5;

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
                        var font = new Font("Arial", 11, FontStyle.Bold);

                        float col1X = x;
                        float col2X = x + 275;

                        // Measure heights
                        float h1 = e.Graphics.MeasureString(label1, font).Height +
                                   e.Graphics.MeasureString(value1, font).Height;

                        float h2 = e.Graphics.MeasureString(label2, font).Height +
                                   e.Graphics.MeasureString(value2, font).Height;

                        float blockHeight = Math.Max(h1, h2);

                        // Draw column 1
                        e.Graphics.DrawString(label1, font, Brushes.Black, new PointF(col1X, y));
                        e.Graphics.DrawString(value1, font, Brushes.Black, new PointF(col1X, y + 12));

                        // Draw column 2
                        e.Graphics.DrawString(label2, font, Brushes.Black, new PointF(col2X, y));
                        e.Graphics.DrawString(value2, font, Brushes.Black, new PointF(col2X, y + 12));

                        // Move Y correctly
                        y += blockHeight;
                    }



                    void PrintCustomTwoColumnBlock(
                     string leftBlock,
                     string packedValue,
                     string weightValue,
                     string caseNoValue)
                    {
                        var leftFont = new Font("Arial", 9, FontStyle.Regular);

                        var rightFont = new Font("Arial", 10, FontStyle.Bold);

                        float col1X = x;
                        float col2X = x + 275; // adjust if needed

                        float startY = y;

                        // === LEFT BLOCK (MULTILINE) ===
                        float lineHeight = e.Graphics.MeasureString("A", leftFont).Height;

                        string[] leftLines = leftBlock.Split('\n');
                        foreach (var line in leftLines)
                        {
                            e.Graphics.DrawString(line.Trim(), leftFont, Brushes.Black, new PointF(col1X, startY));
                            startY += lineHeight;
                        }

                        // === RIGHT COLUMN (INLINE LABEL + VALUE) ===
                        float rightY = y;
                        float lineGap = 20f;

                        e.Graphics.DrawString(
                            "Date Packed: " + (packedValue ?? "N/A"),
                            rightFont,
                            Brushes.Black,
                            new PointF(col2X, rightY)
                        );

                        rightY += lineGap;

                        e.Graphics.DrawString(
                            "Case Weight: " + (weightValue ?? "N/A"),
                            rightFont,
                            Brushes.Black,
                            new PointF(col2X, rightY)
                        );

                        rightY += lineGap;

                        e.Graphics.DrawString(
                            "Case Number: " + (caseNoValue ?? "N/A"),
                            rightFont,
                            Brushes.Black,
                            new PointF(col2X, rightY)
                        );

                        // === MOVE Y BASED ON TALLEST SIDE ===
                        float leftHeight = startY - y;
                        float rightHeight = rightY - y;

                        y += Math.Max(leftHeight, rightHeight) + 5;
                    }

                    PrintTwoColumnRow(
                        "Questo scatola contiene 3.500pz", "(14 sacchetti da 250pz cad)",
                        "Materiale: 100% carta", "Per contatto con i prodotti alimentari"
                    );

                    string distributorBlock =
                        @"Distribuito da: Mana'o Lab Srl
                        Via Gamboloita, 4-20139 Milano - ITA
                        info@manaolab.it - www.manaolab.it
                        Num. di lotto: 304PBI25OG
                        Made in UK";

                    PrintCustomTwoColumnBlock(
                        distributorBlock,
                        labelinfo?.Create_Date.ToString() ?? "N/A",
                        AssemblyItem?.Custitemproduct_Spec_Casewtnet != null
                            ? $"{AssemblyItem.Custitemproduct_Spec_Casewtgrosskg} KG"
                            : "N/A",
                        labelNoStr
                    );

                    void PrintCenteredSingleColumnTableWithBorder(string[] rows)
                    {
                        float pageWidth = e.PageSettings.PrintableArea.Width;
                        float pageLeft = e.PageSettings.PrintableArea.Left;

                        // 🔹 Table width (SMALL column, not full page)
                        float tableWidth = 300f;

                        // Center table horizontally
                        float tableX = (pageLeft + (pageWidth - tableWidth) / 2) + 100;

                        var font1 = new Font("Arial", 13, FontStyle.Bold);
                        var font2 = new Font("Arial", 11, FontStyle.Bold);
                        var font3 = new Font("Arial", 10, FontStyle.Bold);
                        var font4 = new Font("Arial", 8, FontStyle.Regular);

                        Font[] fonts = { font1, font2, font3, font4 };

                        float rowHeight = 22f;   // try 20f if still tight
                        float tableHeight = rowHeight * rows.Length;

                        var pen = new Pen(Color.Black, 1);

                        // === DRAW OUTER BORDER ===
                        e.Graphics.DrawRectangle(pen, tableX, y, tableWidth, tableHeight);

                        for (int i = 0; i < rows.Length; i++)
                        {
                            float rowY = y + (i * rowHeight);

                            // horizontal line (row separator)
                            e.Graphics.DrawLine(pen, tableX, rowY, tableX + tableWidth, rowY);

                            // choose font per row
                            Font font = fonts[Math.Min(i, fonts.Length - 1)];

                            // centered text inside cell
                            var format = new StringFormat
                            {
                                Alignment = StringAlignment.Center,
                                LineAlignment = StringAlignment.Center
                            };

                            e.Graphics.DrawString(
                                rows[i],
                                font,
                                Brushes.Black,
                                new RectangleF(tableX, rowY, tableWidth, rowHeight),
                                format
                            );
                        }

                        // bottom border line
                        e.Graphics.DrawLine(pen, tableX, y + tableHeight, tableX + tableWidth, y + tableHeight);

                        y += tableHeight + 5;
                    }
                    PrintCenteredSingleColumnTableWithBorder(new string[]
                        {
                        "SCATOLA",
                        "PAP20",
                        "RACCOLTA CARTA",
                        "Verifica le disposizioni del tuo Comune. Riduci il volume dell'imballaggio prima di conferirlo in raccolta"
                        });


                    // === PAP IMAGE BELOW TABLE (CENTERED) ===
                    if (System.IO.File.Exists(imagePAPPath))
                    {
                        using var imgPAP = Image.FromFile(imagePAPPath);

                        float ratioPAP = imgPAP.Height / (float)imgPAP.Width;

                        int logoPAPWidth = 200;
                        int logoPAPHeight = (int)(logoPAPWidth * ratioPAP);

                        //// Use printable area for centering (same logic as table)
                        //float pageWidth = e.PageSettings.PrintableArea.Width;
                        //float pageLeft = e.PageSettings.PrintableArea.Left;

                        // Center horizontally
                        float papX = (pageLeft + (pageWidth - logoPAPWidth) / 2) + 100;

                        float papY = y;

                        e.Graphics.DrawImage(
                            imgPAP,
                            new RectangleF(papX, papY, logoPAPWidth, logoPAPHeight)
                        );

                        // Move Y after image
                        y += logoPAPHeight + 10;
                    }
                };

                printDoc.Print();
            }
        }
        catch (Exception ex)
        {
            throw new Exception("PEPSI Template Error: " + ex.Message);
        }
    }
}
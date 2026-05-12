using DominiShop.Model;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace DominiShop.Service;

public static class OrderPdfExportService
{
    private sealed record PdfLine(string Text, int FontSize = 10, int Indent = 0);

    public static byte[] CreateOrderPdf(Order order, string customerName, string customerTier, string customerTierDiscount)
    {
        var lines = BuildLines(order, customerName, customerTier, customerTierDiscount);
        var pages = Paginate(lines, 44);
        return BuildPdf(pages);
    }

    private static List<PdfLine> BuildLines(Order order, string customerName, string customerTier, string customerTierDiscount)
    {
        var lines = new List<PdfLine>
        {
            new("DominiShop Order Receipt", 18),
            new($"Order #{order.Id}", 14),
            new($"Date: {order.OrderAt:dd/MM/yyyy HH:mm}"),
            new($"Status: {order.StatusLabel}"),
            new(""),
            new("Customer", 12),
            new($"Name: {customerName}", 10, 12),
            new($"Phone: {order.CustomerPhone}", 10, 12),
            new($"Tier: {customerTier} ({customerTierDiscount})", 10, 12),
            new(""),
            new("Items", 12)
        };

        foreach (var detail in order.OrderDetails)
        {
            lines.Add(new($"{detail.ProductName}", 10, 12));
            lines.Add(new($"Qty: {detail.Quantity}    Unit: {FormatMoney(detail.Price)}    Subtotal: {FormatMoney(detail.SubTotal)}", 10, 24));
        }

        lines.Add(new(""));
        lines.Add(new("Vouchers", 12));
        if (order.OrderVouchers.Any())
        {
            foreach (var voucher in order.OrderVouchers)
                lines.Add(new($"{voucher.VoucherCode} - {voucher.VoucherDiscount}", 10, 12));
        }
        else
        {
            lines.Add(new("None", 10, 12));
        }

        lines.Add(new(""));
        lines.Add(new("Taxes", 12));
        if (order.OrderTaxes.Any())
        {
            foreach (var tax in order.OrderTaxes)
                lines.Add(new($"{tax.TaxName} - {tax.TaxFormattedValue}", 10, 12));
        }
        else
        {
            lines.Add(new("None", 10, 12));
        }

        if (order.IsOnline)
        {
            lines.Add(new(""));
            lines.Add(new("Shipping", 12));
            lines.Add(new($"Address: {order.Address}", 10, 12));
            lines.Add(new($"Shipping fee: {FormatMoney(order.ShippingFee ?? 0)}", 10, 12));
        }

        lines.Add(new(""));
        lines.Add(new($"Subtotal: {FormatMoney(order.OrderDetails.Sum(d => d.SubTotal))}", 11));
        lines.Add(new($"Grand total: {FormatMoney(order.TotalPrice ?? 0)}", 14));

        return lines;
    }

    private static List<List<PdfLine>> Paginate(List<PdfLine> lines, int maxLinesPerPage)
    {
        var pages = new List<List<PdfLine>>();
        for (var i = 0; i < lines.Count; i += maxLinesPerPage)
            pages.Add(lines.Skip(i).Take(maxLinesPerPage).ToList());
        return pages.Count > 0 ? pages : new List<List<PdfLine>> { new() };
    }

    private static byte[] BuildPdf(List<List<PdfLine>> pages)
    {
        var objects = new SortedDictionary<int, string>();
        objects[1] = "<< /Type /Catalog /Pages 2 0 R >>";
        objects[3] = "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>";

        var kids = new List<string>();
        var nextObjectId = 4;
        foreach (var page in pages)
        {
            var pageObjectId = nextObjectId++;
            var contentObjectId = nextObjectId++;
            kids.Add($"{pageObjectId} 0 R");

            var stream = BuildContentStream(page);
            var streamLength = Encoding.ASCII.GetByteCount(stream);
            objects[pageObjectId] = $"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 3 0 R >> >> /Contents {contentObjectId} 0 R >>";
            objects[contentObjectId] = $"<< /Length {streamLength} >>\nstream\n{stream}endstream";
        }

        objects[2] = $"<< /Type /Pages /Kids [{string.Join(" ", kids)}] /Count {pages.Count} >>";

        using var buffer = new MemoryStream();
        WriteAscii(buffer, "%PDF-1.4\n");

        var offsets = new Dictionary<int, long>();
        foreach (var item in objects)
        {
            offsets[item.Key] = buffer.Position;
            WriteAscii(buffer, $"{item.Key} 0 obj\n{item.Value}\nendobj\n");
        }

        var xrefOffset = buffer.Position;
        var maxObjectId = objects.Keys.Max();
        WriteAscii(buffer, $"xref\n0 {maxObjectId + 1}\n");
        WriteAscii(buffer, "0000000000 65535 f \n");
        for (var i = 1; i <= maxObjectId; i++)
        {
            var offset = offsets.TryGetValue(i, out var value) ? value : 0;
            WriteAscii(buffer, $"{offset:0000000000} 00000 n \n");
        }

        WriteAscii(buffer, $"trailer\n<< /Size {maxObjectId + 1} /Root 1 0 R >>\nstartxref\n{xrefOffset}\n%%EOF");
        return buffer.ToArray();
    }

    private static string BuildContentStream(List<PdfLine> lines)
    {
        var builder = new StringBuilder();
        var y = 800;

        foreach (var line in lines)
        {
            var escaped = EscapePdfText(ToPdfSafeText(line.Text));
            var x = 50 + line.Indent;
            builder.Append(CultureInfo.InvariantCulture, $"BT /F1 {line.FontSize} Tf {x} {y} Td ({escaped}) Tj ET\n");
            y -= line.FontSize + 6;
        }

        return builder.ToString();
    }

    private static string FormatMoney(decimal amount) => $"{amount:N0} VND";

    private static string ToPdfSafeText(string? text)
    {
        var source = (text ?? string.Empty)
            .Replace("₫", "VND")
            .Replace("đ", "d")
            .Replace("Đ", "D")
            .Replace("—", "-")
            .Replace("–", "-")
            .Replace("…", "...");

        var builder = new StringBuilder(source.Length);
        foreach (var ch in source)
            builder.Append(ch is >= ' ' and <= '~' ? ch : '?');
        return builder.ToString();
    }

    private static string EscapePdfText(string text) =>
        text.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");

    private static void WriteAscii(Stream stream, string value)
    {
        var bytes = Encoding.ASCII.GetBytes(value);
        stream.Write(bytes, 0, bytes.Length);
    }
}

using ClivoxApp.Models;
using ClivoxApp.Models.Clients;
using ClivoxApp.Models.Invoice;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.IO;
using System.Linq;

namespace ClivoxApp.Services;

public class ExportInvoicePdf
{
    static ExportInvoicePdf()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public static MemoryStream ExportToPdf(Invoice invoice, Client client, BusinessOwner businessOwner)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(12));

                page.Header().Element(container => ComposeHeader(container, businessOwner, invoice));
                page.Content().Element(container => ComposeContent(container, invoice, client, businessOwner));
                page.Footer().Element(container => ComposeFooter(container, businessOwner, invoice));
            });
        });

        var stream = new MemoryStream();
        document.GeneratePdf(stream);
        stream.Position = 0;
        return stream;
    }

    private static void ComposeHeader(IContainer container, BusinessOwner businessOwner, Invoice invoice)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(column =>
            {
                string ownerName = !string.IsNullOrEmpty(businessOwner.CompanyName)
                    ? businessOwner.CompanyName
                    : $"{businessOwner.FirstName} {businessOwner.LastName}";

                column.Item().Text(ownerName).SemiBold().FontSize(20).FontColor(Colors.Blue.Medium);

                if (!string.IsNullOrEmpty(businessOwner.Email))
                    column.Item().Text(businessOwner.Email);

                if (businessOwner.Address != null)
                {
                    column.Item().Text(businessOwner.Address.Street ?? "");
                    column.Item().Text($"{businessOwner.Address.PostalCode} {businessOwner.Address.City}");
                }
            });

            row.ConstantItem(220).Column(column =>
            {
                column.Item().BorderBottom(1).PaddingBottom(5).Text("RECHNUNG").SemiBold().FontSize(15);

                column.Item().Row(row =>
                {
                    row.RelativeItem().Text("Rechnungsnummer:");
                    row.RelativeItem().Text(invoice.InvoiceNumber).SemiBold();
                });

                column.Item().Row(row =>
                {
                    row.RelativeItem().Text("Datum:");
                    row.RelativeItem().Text(invoice.InvoiceDate.ToString("dd.MM.yyyy")).SemiBold();
                });
                column.Item().Row(row =>
                {
                    row.RelativeItem().Text("Fälligkeitsdatum:");
                    row.RelativeItem().Text(invoice.DueDate.ToString("dd.MM.yyyy")).SemiBold();
                });
                column.Item().Row(row =>
                {
                    row.RelativeItem().Text("Leistungsdatum:");
                    row.RelativeItem().Text(invoice.ServiceDate.ToString("dd.MM.yyyy")).SemiBold();
                });
                column.Item().Row(row =>
                {
                    row.RelativeItem().Text("Steuernummer:");
                    row.RelativeItem().Text(businessOwner.TaxNumber).SemiBold();
                });
            });
        });
    }

    private static void ComposeContent(IContainer container, Invoice invoice, Client client, BusinessOwner businessOwner)
    {
        container.PaddingVertical(40).Column(column =>
        {
            column.Item().Element(container => ComposeClientInfo(container, client));
            column.Item().PaddingTop(25);
            column.Item().Element(container => ComposeTable(container, invoice));
            column.Item().PaddingTop(25);
            column.Item().Element(container => ComposeTotal(container, invoice));
        });
    }

    private static void ComposeClientInfo(IContainer container, Client client)
    {
        container.Column(column =>
        {
            var salutation = client.Gender == Gender.Female ? "Frau" : "Herr";
            column.Item().Text(salutation);
            column.Item().Text(client.FullName).SemiBold();

            if (client.Address != null)
            {
                column.Item().Text(client.Address.Street ?? "");
                column.Item().Text($"{client.Address.PostalCode} {client.Address.City}");
            }
        });
    }

    private static void ComposeTable(IContainer container, Invoice invoice)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(50);
                columns.RelativeColumn(3);
                columns.ConstantColumn(80);
                columns.ConstantColumn(100);
                columns.ConstantColumn(80);
            });

            table.Header(header =>
            {
                header.Cell().Element(CellStyle).Text("Pos.").SemiBold();
                header.Cell().Element(CellStyle).Text("Beschreibung").SemiBold();
                header.Cell().Element(CellStyle).Text("Menge").SemiBold();
                header.Cell().Element(CellStyle).Text("Preis/€").SemiBold();
                header.Cell().Element(CellStyle).Text("Gesamt/€").SemiBold();

                static IContainer CellStyle(IContainer container)
                {
                    return container.DefaultTextStyle(x => x.SemiBold()).PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Black);
                }
            });

            int position = 1;
            foreach (var item in invoice.Items)
            {
                table.Cell().Element(CellStyle).Text(position.ToString());
                table.Cell().Element(CellStyle).Text(item.Description);
                table.Cell().Element(CellStyle).Text(GetQuantityText(item));
                table.Cell().Element(CellStyle).Text(GetUnitPriceText(item));
                table.Cell().Element(CellStyle).Text(item.Total.ToString("F2"));

                static IContainer CellStyle(IContainer container)
                {
                    return container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5);
                }

                position++;
            }
        });
    }

    private static void ComposeTotal(IContainer container, Invoice invoice)
    {
        decimal totalAmount = invoice.Items.Sum(i => i.Total);

        container.AlignRight().Column(column =>
        {
            column.Item().Background(Colors.Blue.Lighten3).Border(1).BorderColor(Colors.Blue.Medium)
                .Padding(10).Row(row =>
                {
                    row.RelativeItem().Text("Gesamtsumme:").SemiBold().FontSize(14);
                    row.ConstantItem(100).Text($"{totalAmount:F2} €").SemiBold().FontSize(14);
                });
        });
    }

    private static void ComposeFooter(IContainer container, BusinessOwner businessOwner, Invoice invoice)
    {
        container.Column(column =>
        {
            column.Item().PaddingTop(25).Text("Vielen Dank für Ihren Auftrag!").SemiBold();
            column.Item().Text("Ich bitte um Überweisung des Rechnungsbetrages innerhalb von 14 Tagen an").SemiBold();
            if (businessOwner.bankAccount != null)
            {
                column.Item().PaddingTop(15).Background(Colors.Grey.Lighten4).Padding(10).Column(bankColumn =>
                {
                    bankColumn.Item().Text("Bankverbindung:").SemiBold();
                    bankColumn.Item().Text($"Kontoinhaber: {businessOwner.bankAccount!.AccountHolder}");
                    bankColumn.Item().Text($"IBAN: {businessOwner.bankAccount!.IBAN}");
                    bankColumn.Item().Text($"BIC: {businessOwner.bankAccount!.BIC}");
                    bankColumn.Item().Text($"Verwendungszweck: {invoice.InvoiceNumber}");
                });
            }
            column.Item().Text("Hinweis: Als Kleinunternehmer im Sinne von § 19 Abs. 1 UStG wird Umsatzsteuer nicht berechnet").SemiBold();
        });
    }

    private static string GetQuantityText(InvoiceItem item)
    {
        return item.BillingType switch
        {
            BillingType.PerHour => item.Quantity.ToString("F2"),
            BillingType.PerSquareMeter => item.Area.ToString("F2"),
            BillingType.PerObject => item.Quantity.ToString("F0"),
            BillingType.FixedPrice => "",
            _ => ""
        };
    }

    private static string GetUnitPriceText(InvoiceItem item)
    {
        return item.BillingType switch
        {
            BillingType.PerHour => item.UnitPrice.ToString("F2"),
            BillingType.PerSquareMeter => item.PricePerSquareMeter.ToString("F2"),
            BillingType.PerObject => item.UnitPrice.ToString("F2"),
            BillingType.FixedPrice => "",
            _ => ""
        };
    }
}
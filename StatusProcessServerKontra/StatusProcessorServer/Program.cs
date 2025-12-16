using System;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using StatusProcessorServer;
using InvoiceProcessorLogic;
class Program
{
    static async Task Main()
    {
        var settings = new SettingsManager("config.json");
        var processor = new InvoiceProcessorService(settings.Get().ConnectionString);

        var httpServer = new HttpServer(settings);

        var serverThread = new Thread(() => httpServer.Start());
        serverThread.Start();


        var random = new Random();

        while (true)
        {
            Console.WriteLine("Обработка инвойсов");

            try
            {
                var invoices = processor.GetPendingInvoices();
                Console.WriteLine($"Найдено: {invoices.Count}");

                foreach (var invoice in invoices)
                {
                    bool success = random.Next(100) < 30;

                    if (success)
                    {
                        processor.UpdateInvoice(invoice.Id, "success", invoice.RetryCount);
                        Console.WriteLine($"Invoice {invoice.Id}: SUCCESS");
                    }
                    else
                    {
                        int newRetry = invoice.RetryCount + 1;

                        if (newRetry >= settings.Get().MaxErrorRetries)
                        {
                            processor.UpdateInvoice(invoice.Id, "error", newRetry);
                            Console.WriteLine($"Invoice {invoice.Id}: ERROR");
                        }
                        else
                        {
                            processor.UpdateInvoice(invoice.Id, "pending", newRetry);
                            Console.WriteLine($"Invoice {invoice.Id}: RETRY {newRetry}/{settings.Get().MaxErrorRetries}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка: " + ex.Message);
            }

            Thread.Sleep(settings.Get().ProcessingIntervalSeconds * 300);
        }
    }
}
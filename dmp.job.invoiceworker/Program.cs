using System.Net.Http.Headers;
using System.Text;
using DMP.BL.Models.Configurations;
using DMP.BL.Services;
using DMP.DataAccess;
using Job.InvoiceWorker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;

var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    Args = args,
    // appsettings files are copied next to the binaries, so resolve them from there
    // regardless of the working directory the process was started from.
    ContentRootPath = AppContext.BaseDirectory,
});

var dataSourceBuilder = new NpgsqlDataSourceBuilder(builder.Configuration.GetConnectionString("DmpConnection"));
dataSourceBuilder.EnableDynamicJson(); // required for jsonb <-> POCO/dictionary mapping
var dataSource = dataSourceBuilder.Build();
builder.Services.AddDbContextFactory<DmpDbContext>(options => options.UseNpgsql(dataSource));

builder.Services.Configure<DmpWebApiOptions>(builder.Configuration.GetSection(DmpWebApiOptions.Position));
builder.Services.Configure<BitcartOptions>(builder.Configuration.GetSection(BitcartOptions.Position));
builder.Services.Configure<DmpHostsSettings>(builder.Configuration.GetSection(DmpHostsSettings.Position));
builder.Services.Configure<MinioSettings>(builder.Configuration.GetSection(MinioSettings.Position));

builder.Services.AddHttpClient(DmpWebApiOptions.Position, (sp, client) =>
{
    var options = sp.GetRequiredService<IOptions<DmpWebApiOptions>>().Value;
    client.BaseAddress = new Uri(options.Url);
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
        "Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($"{options.User}:{options.Password}")));
});

builder.Services.AddTransient<IPaymentService, PaymentService>();
builder.Services.AddHostedService<InvoiceWorkerService>();

await builder.Build().RunAsync();

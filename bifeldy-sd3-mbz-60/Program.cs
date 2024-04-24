/**
 * 
 * Author       :: Basilius Bias Astho Christyono
 * Phone        :: (+62) 889 236 6466
 * 
 * Department   :: IT SD 03
 * Mail         :: bias@indomaret.co.id
 * 
 * Catatan      :: Bifeldy's Entry Point
 * 
 */

using Microsoft.AspNetCore.Hosting.StaticWebAssets;
using Microsoft.AspNetCore.HttpOverrides;
// using Microsoft.AspNetCore.Mvc.ApplicationParts;

using MudBlazor.Services;

using bifeldy_sd3_lib_60;
using bifeldy_sd3_lib_60.Extensions;

using bifeldy_sd3_mbz_60.JobSchedulers;
using bifeldy_sd3_mbz_60.Models;
using bifeldy_sd3_mbz_60.Components.Spinner;
using bifeldy_sd3_mbz_60.Services;

string apiUrlPrefix = "api";

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
StaticWebAssetsLoader.UseStaticWebAssets(builder.Environment, builder.Configuration);
builder.Services.Configure<ENV>(builder.Configuration.GetSection("ENV"));

Bifeldy.InitBuilder(builder);
Bifeldy.SetupSerilog();
Bifeldy.LoadConfig();
Bifeldy.AddSwagger(
    apiUrlPrefix,
    docsDescription: "API Documentation Untuk Ambil Data Prod Mast Dari DC (Optional Via HO) ~",
    enableJwt: true
);
Bifeldy.AddJobScheduler();

builder.Services.AddCors();
builder.Services.AddControllers(x => {
    x.UseRoutePrefix(apiUrlPrefix);
}).AddJsonOptions(x => {
    x.JsonSerializerOptions.PropertyNamingPolicy = null;
}).AddXmlSerializerFormatters(); // .PartManager.ApplicationParts.Add(new AssemblyPart(typeof(Bifeldy).Assembly));
builder.Services.AddAuthenticationCore();
builder.Services.AddRazorPages(x => {
    x.RootDirectory = "/Templates";
});
builder.Services.AddServerSideBlazor();
builder.Services.AddMudServices();

// Tambah Dependency Injection Di Sini --
Bifeldy.AddDependencyInjection();

// Background Hosted Service Long Run Task Di Sini --
// Bifeldy.AddKafkaProducerBackground("127.0.0.1:9092", "bias_uji_coba", suffixKodeDc: true);
// Bifeldy.AddKafkaConsumerBackground("127.0.0.1:9092", "bias_uji_coba", suffixKodeDc: true);
Bifeldy.AddKafkaAutoProducerConsumerBackground();
// --
builder.Services.AddScoped<ISpinnerService, CSpinnerService>();

// Job Scheduler Di Sini -- https://www.freeformatter.com/cron-expression-generator-quartz.html
Bifeldy.CreateJobSchedule<JobFolderCleaner>("0 * * ? * *");
// Bifeldy.CreateJobSchedules("0 * * ? * *", typeof(JobFolderCleaner), typeof(JobFolderCleaner), typeof(JobFolderCleaner));

WebApplication app = builder.Build();

Bifeldy.AppContextOverride();
Bifeldy.InitApp(app);
Bifeldy.UseSwagger(apiUrlPrefix);
await Bifeldy.StartJobScheduler();
Bifeldy.UseHelmet();
Bifeldy.UseNginxProxyPathSegment();
Bifeldy.UseErrorHandlerMiddleware();
Bifeldy.UseApiKeyMiddleware();
Bifeldy.UseJwtMiddleware();

if (app.Environment.IsDevelopment()) {
    app.UseDeveloperExceptionPage();
}
else {
    app.UseExceptionHandler("/Error");
    // app.UseHsts();
}

// app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseCors(x =>
    x.SetIsOriginAllowed(origin => true)
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials()
);
// app.UseAuthentication();
app.UseAuthorization();

app.UseForwardedHeaders(
    new ForwardedHeadersOptions {
        ForwardedHeaders = ForwardedHeaders.All
    }
);

app.UseEndpoints(x => {
    x.MapControllers();
    x.Map("/" + apiUrlPrefix + "/{*url:regex(^(?!swagger).*$)}", Bifeldy.Handle404ApiNotFound);
    x.MapBlazorHub();
    x.MapRazorPages();
    x.MapFallbackToPage("/{*url:regex(^(?!" + apiUrlPrefix + "/swagger).*$)}", "/_Host");
});

await app.RunAsync();

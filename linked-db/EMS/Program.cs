using System.Data.SqlClient;
using System.Diagnostics;
using Dapper;
using OpenTelemetry.Exporter;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

var lsToken = builder.Configuration.GetValue<string>("LSToken");

builder.Services.AddScoped(_ =>
    new SqlConnection(builder.Configuration.GetConnectionString("EmployeeDbConnectionString")));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddOpenTelemetryTracing(builder => builder
    .AddAspNetCoreInstrumentation(options =>
    {
        options.Filter = context => context.Request.Path.Value?.Contains("ems") ?? false;
        options.RecordException = true;
        options.Enrich = (activity, eventName, rawObject) =>
        {
            switch (eventName)
            {
                case "OnStartActivity":
                    {
                        if (rawObject is not HttpRequest httpRequest)
                        {
                            return;
                        }

                        activity.SetTag("service.version", "1.0.0.0");
                        activity.SetTag("requestProtocol", httpRequest.Protocol);
                        activity.SetTag("requestMethod", httpRequest.Method);
                        break;
                    }
                case "OnStopActivity":
                    {
                        if (rawObject is HttpResponse httpResponse)
                        {
                            activity.SetTag("responseLength", httpResponse.ContentLength);
                        }

                        break;
                    }
            }
        };
    })
    .AddHttpClientInstrumentation()
    .AddSqlClientInstrumentation(options =>
    {
        options.EnableConnectionLevelAttributes = true;
        options.SetDbStatementForStoredProcedure = true;
        options.SetDbStatementForText = true;
        options.RecordException = true;
        options.Enrich = (activity, _, _) => activity.SetTag("db.type", "sql");
    })
    .AddSource("my-corp.ems.ems-api")
    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("ems-api"))
    .SetSampler(new AlwaysOnSampler())
    .AddOtlpExporter(otlpOptions =>
    {
        otlpOptions.Endpoint = new Uri("https://ingest.lightstep.com:443/traces/otlp/v0.9");
        otlpOptions.Headers = $"lightstep-access-token={lsToken}";
        otlpOptions.Protocol = OtlpExportProtocol.HttpProtobuf;
    }));

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

var activitySource = new ActivitySource("my-corp.ems.ems-api", "1.0.0");

app.MapPost("/ems/billing", async (Timekeeping timekeepingRecord, SqlConnection db) =>
    {
        using var activity = activitySource.StartActivity("Record project work", ActivityKind.Server);
        activity?.AddEvent(new ActivityEvent("Project billed"));
        activity?.SetTag(nameof(Timekeeping.EmployeeId), timekeepingRecord.EmployeeId);
        activity?.SetTag(nameof(Timekeeping.ProjectId), timekeepingRecord.ProjectId);
        activity?.SetTag(nameof(Timekeeping.WeekClosingDate), timekeepingRecord.WeekClosingDate);

        await db.ExecuteAsync(
            "INSERT INTO Timekeeping Values(@EmployeeId, @ProjectId, @WeekClosingDate, @HoursWorked)",
            timekeepingRecord);
        return Results.Created($"/ems/billing/{timekeepingRecord.EmployeeId}", timekeepingRecord);
    })
    .WithName("RecordProjectWork")
    .Produces(StatusCodes.Status201Created);

app.MapGet("/ems/billing/{empId}/", async (int empId, SqlConnection db) =>
    {
        using var activity = activitySource.StartActivity("Fetch projects for employee", ActivityKind.Server);
        activity?.SetTag(nameof(Timekeeping.EmployeeId), empId);

        var result = await db.QueryAsync<Timekeeping>("SELECT * FROM Timekeeping WHERE EmployeeId=@empId", empId);
        return result.Any() ? Results.Ok(result) : Results.NotFound();
    })
    .WithName("GetBillingDetails")
    .Produces<IEnumerable<Timekeeping>>()
    .Produces(StatusCodes.Status404NotFound);

app.MapPost("/ems/payroll/add/{empId}/{rate}", () =>
    {
        using var activity = activitySource.StartActivity("SayHello");
        activity?.SetTag("foo", 1);
        activity?.SetTag("bar", "Hello, World!");
        activity?.SetTag("baz", new[] { 1, 2, 3 });
    })
    .WithName("AddEmployeeToPayroll");

app.MapGet("/ems/payroll/{empId}", () =>
    {
        using var activity = activitySource.StartActivity("SayHello");
        activity?.SetTag("foo", 1);
        activity?.SetTag("bar", "Hello, World!");
        activity?.SetTag("baz", new[] { 1, 2, 3 });
    })
    .WithName("GetEmployeePayroll");

app.Run();


public class Timekeeping
{
    public int EmployeeId { get; set; }

    public int ProjectId { get; set; }

    public DateTime WeekClosingDate { get; set; }

    public int HoursWorked { get; set; }
}

public class Payroll
{
    public int EmployeeId { get; set; }

    public decimal PayRateInUSD { get; set; }
}
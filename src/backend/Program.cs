
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Parking.Api.Data;
using Parking.Api.Interfaces;
using Parking.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(opt =>
{
    opt.UseNpgsql(builder.Configuration.GetConnectionString("Postgres"));
});

builder.Services.AddScoped<IPlacaService,PlacaService>();
builder.Services.AddScoped<IFaturamentoService,FaturamentoService>();
builder.Services.AddScoped<IImportService, ImportService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Parking API", Version = "v1" });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    db.Database.ExecuteSqlRaw(@"
        CREATE TABLE IF NOT EXISTS public.veiculo_historico (
            id uuid NOT NULL DEFAULT uuid_generate_v4(),
            veiculo_id uuid NOT NULL,
            cliente_id uuid NOT NULL,
            data_inicio timestamp with time zone NOT NULL,
            data_fim timestamp with time zone NULL,
            CONSTRAINT pk_veiculo_historico PRIMARY KEY (id),
            CONSTRAINT fk_vh_veiculo FOREIGN KEY (veiculo_id) REFERENCES public.veiculo(id) ON DELETE CASCADE,
            CONSTRAINT fk_vh_cliente FOREIGN KEY (cliente_id) REFERENCES public.cliente(id)
        )
    ");

    
    db.Database.ExecuteSqlRaw(@"
        INSERT INTO public.veiculo_historico (id, veiculo_id, cliente_id, data_inicio, data_fim)
        SELECT uuid_generate_v4(),
               v.id,
               v.cliente_id,
               DATE_TRUNC('day', v.data_inclusao AT TIME ZONE 'UTC'),
               NULL
        FROM public.veiculo v
        WHERE NOT EXISTS (
            SELECT 1 FROM public.veiculo_historico vh
            WHERE vh.veiculo_id = v.id AND vh.data_fim IS NULL
        )
    ");
}

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Parking API v1");
    c.RoutePrefix = string.Empty; 
});

app.UseCors("AllowFrontend");

app.MapControllers();

app.Run();

var builder = WebApplication.CreateBuilder(args);

// Bug 4 — CORS
builder.Services.AddCors(options => {
    options.AddPolicy("PermitirTudo", b =>
        b.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

// Bug 3 — registar os controllers
builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Bug 4 — ativar CORS (tem de vir antes do MapControllers)
app.UseCors("PermitirTudo");

app.UseHttpsRedirection();

// Bug 3 — mapear as rotas dos controllers
app.MapControllers();

app.MapGet("/", () => "API de Resultados a funcionar perfeitamente!");

app.Run();
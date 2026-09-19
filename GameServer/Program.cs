using System.Text;
using GameServer.Data;
using GameServer.Entities;
using GameServer.Hubs;
using GameServer.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers(); //MVC의 Controller가 만들어진다.

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, ct) =>
    {
        document.Info.Title = "Casual RPG Game API server";
        document.Info.Version = "0.0.1";
        document.Info.Description = "케쥬얼 RPG 서버 REST API";
        //원래 이건 비동기 작업을 위한 람다식인데 우리가 지금 한건 비동기 작업이 없기 때문에,
        //그냥 빈 테스크를 리턴하면 된다.
        return Task.CompletedTask;
    });
});

builder.Services.AddSignalR();

string? connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                            ?? throw new InvalidOperationException("Connection string not found.");

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 4, 11)));
});

builder.Services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<CharacterService>();
builder.Services.AddScoped<DungeonService>();

builder.Services.AddSingleton(GameCatalog.LoadFrom(builder.Environment.ContentRootPath));

//들어온 요청에서 JWT 토큰을 분해해서 알맞은 값으로 셋업해주는 서비를 만든다.
string jwtKey = builder.Configuration["Jwt:key"] 
                ?? throw new InvalidOperationException("Jwt:key not configured.");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        //sub 가 표준 이름이야. Net은 내부적으로 이걸 받으면 .net전용으로 변경해. 그래서 이거 하지 말라는 뜻
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true, //서명 검사 할꺼다.
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        };
        //옵션은 넣어줄꺼고
        
        //시그널R용 토큰 처리가 필요합니다.
        // WebSocket 핸드셰이크는 Authorization 헤더가 존재하지 않는다. (애초에 Content-header가 없어)
        // 그래서 토큰을 쓸 때(?access_token=asdads) 형태의 쿼리스트링으로 보낸다.
        // 경
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken)
                    && path.StartsWithSegments("/hubs/chat"))
                    context.Token = accessToken;
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization(); //권한 관리

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "CasualRPG Server");
    });
}

app.UseHttpsRedirection();

//인증(누구인지) -> 인가(권한이 있는지) 순서로 파이프라인에 추가된 뒤 MapController가 이뤄지면 된다.
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => "Server is running!");
app.MapControllers(); //라우팅에 엔드포인트를 추가해주는 작업을 한다.
app.MapHub<ChatHub>("/hubs/chat");
app.Run();
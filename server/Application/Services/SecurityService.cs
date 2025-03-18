using System.ComponentModel.DataAnnotations;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Text;
using Application.Interfaces;
using Application.Interfaces.Infrastructure.Postgres;
using Application.Interfaces.Infrastructure.Websocket;
using Application.Models;
using Application.Models.Dtos;
using Application.Models.Enums;
using Core.Domain.Entities;
using JWT;
using JWT.Algorithms;
using JWT.Builder;
using JWT.Serializers;
using Microsoft.Extensions.Options;

namespace Application.Services;

public class SecurityService(IOptionsMonitor<AppOptions> optionsMonitor, 
    IConnectionManager connectionManager,
    IQuestionRepository repository) : ISecurityService
{


    public AuthResponseDto Login(AuthRequestDto dto)
    {
        if (dto.Password != optionsMonitor.CurrentValue.Pass)
            throw new Exception("Wrong pass");

        connectionManager.AddToTopic("alex", dto.ClientId);
        
        return new AuthResponseDto
        {
            Jwt = GenerateJwt()
        };
    }
    
    public string GenerateJwt()
    {
        var tokenBuilder = new JwtBuilder()
            .WithAlgorithm(new HMACSHA512Algorithm())
            .WithSecret(optionsMonitor.CurrentValue.JwtSecret)
            .WithUrlEncoder(new JwtBase64UrlEncoder())
            .WithJsonSerializer(new JsonNetSerializer());
        return tokenBuilder.Encode();
    }

    public void VerifyJwtOrThrow(string jwt)
    {
        var builder = new JwtBuilder()
            .WithAlgorithm(new HMACSHA512Algorithm())
            .WithSecret(optionsMonitor.CurrentValue.JwtSecret)
            .WithUrlEncoder(new JwtBase64UrlEncoder())
            .WithJsonSerializer(new JsonNetSerializer())
            .MustVerifySignature();
            builder.Decode(jwt);
        
    }
}
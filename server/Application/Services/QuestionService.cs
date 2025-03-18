using Application.Interfaces;
using Application.Interfaces.Infrastructure.Postgres;
using Application.Interfaces.Infrastructure.Websocket;
using Application.Models;
using Application.Models.Dtos;
using Core.Domain.Entities;

namespace Application.Services;

public class QuestionService(IQuestionRepository questionRepository, IConnectionManager connectionManager ) : IQuestionService
{
    public Question AddQuestion(CreateQuestionDto dto)
    {
        var q = new Question()
        {
            Id = Guid.NewGuid().ToString(),
            Questiontext = dto.QuestionText,
            Timestamp = DateTime.UtcNow
        };
        var broadcast = new BroadcastToAlex()
        {
            Question = q
        };
        connectionManager.BroadcastToTopic("alex",broadcast);
        return questionRepository.AddQuestion(q);
    }

    public List<Question> GetPreviousXQuestions(Question? lastQuestoin = null, int take = 5)
    {
        return questionRepository.GetPreviousXQuestions(lastQuestoin, take);
    }
}
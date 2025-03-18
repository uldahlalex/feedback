using Application.Models.Dtos;
using Core.Domain.Entities;

namespace Application.Interfaces.Infrastructure.Postgres;

public interface IQuestionRepository
{
    public Question AddQuestion(Question q);
    public List<Question> GetPreviousXQuestions(Question? lastQuestion = null, int take = 5);
}
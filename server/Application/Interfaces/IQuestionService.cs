using Application.Models.Dtos;
using Core.Domain.Entities;

namespace Application.Interfaces;

public interface IQuestionService
{
    
    public Question AddQuestion(CreateQuestionDto dto);
    public List<Question> GetPreviousXQuestions(Question? lastQuestoin = null, int take = 5);
}
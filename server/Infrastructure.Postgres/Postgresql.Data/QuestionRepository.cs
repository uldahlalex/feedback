using Application.Interfaces.Infrastructure.Postgres;
using Application.Models.Dtos;
using Core.Domain.Entities;
using Infrastructure.Postgres.Scaffolding;

namespace Infrastructure.Postgres.Postgresql.Data;

public class QuestionRepository(MyDbContext ctx) : IQuestionRepository
{
    public Question AddQuestion(Question q)
    {
        ctx.Questions.Add(q);
        ctx.SaveChanges();
        return q;
    }

    public List<Question> GetPreviousXQuestions(Question? lastQuestion = null, int take = 5)
    {
        if (lastQuestion == null)
        {
            return ctx.Questions
                .OrderByDescending(q => q.Timestamp)
                .Take(take)
                .ToList();
        }

        return ctx.Questions
            .Where(q => q.Timestamp < lastQuestion.Timestamp)
            .OrderByDescending(q => q.Timestamp)
            .Take(take)
            .ToList();
        
    }
}
using Api.Rest.Extensions;
using Application.Interfaces;
using Application.Models.Dtos;
using Core.Domain.Entities;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Api.Rest.Controllers;

[ApiController]
public class QuestionController(IQuestionService questionService, ISecurityService securityService) : ControllerBase
{
    public const string AddQuestionRoute = nameof(AddQuestion);

    [Route(AddQuestionRoute)]
    public ActionResult AddQuestion([FromBody]CreateQuestionDto dto)
    {
        questionService.AddQuestion(dto);
        return Ok();
    }
    
    public const string GetPreviousQuestionsRoute = nameof(GetPreviousQuestions);

    [Route(GetPreviousQuestionsRoute)]
    public ActionResult<List<Question>> GetPreviousQuestions([FromBody] Question? lastQuestion = null, int take = 5)
    {
        var jwt = HttpContext.GetJwt();
        securityService.VerifyJwtOrThrow(jwt);
        return Ok(questionService.GetPreviousXQuestions(lastQuestion, take));
    }

}
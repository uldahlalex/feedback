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
    public async Task<ActionResult> AddQuestion([FromBody]CreateQuestionDto dto)
    {
        await questionService.AddQuestion(dto);
        return Ok();
    }
    
    public const string GetPreviousQuestionsRoute = nameof(GetPreviousQuestions);

    [Route(GetPreviousQuestionsRoute)]
    public ActionResult<List<Question>> GetPreviousQuestions( 
        [FromHeader]string authorization,
        [FromBody] Question? lastQuestion = null, 
        int take = 5)
    {
        securityService.VerifyJwtOrThrow(authorization);
        return Ok(questionService.GetPreviousXQuestions(lastQuestion, take));
    }

}
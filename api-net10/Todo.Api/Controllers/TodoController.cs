using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Api.Base;
using SharedKernel.Application.Exceptions;
using Todo.Application.Commands;
using Todo.Application.DTO;
using Todo.Application.Queries;

namespace Todo.Api.Controllers;

[Route("api/todo")]
[ApiController]
[Authorize]                       // áp cho mọi action, khỏi lặp từng cái
public class TodoController : BaseApiController
{
    [HttpPost]
    public async Task<ActionResult<TodoDto>> Create([FromBody] CreateTodoCommand request)
    {
        try
        {
            return Ok(await Mediator.Send(request));
        }
        catch (Exception ex)
        {
            var err = ErrorCtr.ExtractErrorInfo(ex); // ← "dịch" exception thành status code + message
            return StatusCode((int)err.errorCode, err.errors); // ← trả về đúng status code đó
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TodoDto>> GetById(Guid id)
    {
        try
        {
            return Ok(await Mediator.Send(new GetTodoByIdQuery { id = id }));
        }
        catch (Exception ex)
        {
            var err = ErrorCtr.ExtractErrorInfo(ex);
            return StatusCode((int)err.errorCode, err.errors);
        }
    }
}
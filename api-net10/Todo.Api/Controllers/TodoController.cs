using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Api.Base;
using SharedKernel.Application.DTO;
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
    #region get all 
    [HttpGet]
    [Route("get-all")]
    public async Task<ActionResult<PagedResult<TodoDto>>> GetList([FromQuery] GetTodoAllQuery request)
    {
        try
        {
            var result = await Mediator.Send(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            var err = ErrorCtr.ExtractErrorInfo(ex);
            return StatusCode(Convert.ToInt32(err.errorCode), err.errors);
        }
    }
    #endregion

    #region get by id
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
    #endregion

    #region create
    [HttpPost]
    [Route("create")]
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
    #endregion

    #region update by id
    [HttpPut]
    [Route("update")]
    public async Task<ActionResult<TodoDto>> Update(UpdateTodoCommand request)
    {
        try
        {
            var result = await Mediator.Send(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            var err = ErrorCtr.ExtractErrorInfo(ex);
            return StatusCode(Convert.ToInt32(err.errorCode), err.errors);
        }
    }
    #endregion

    #region delete by id
    [HttpDelete("delete/{id:guid}")]
    public async Task<ActionResult> Delete(Guid id)
    {
        try
        {
            await Mediator.Send(new DeleteTodoCommand { id = id });
            return Ok(new { message = "Xóa công việc thành công." });
        }
        catch (Exception ex)
        {
            var err = ErrorCtr.ExtractErrorInfo(ex);
            return StatusCode((int)err.errorCode, err.errors);
        }
    }
    #endregion
}
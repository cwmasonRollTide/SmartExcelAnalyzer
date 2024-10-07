using MediatR;
using Application.Queries;
using Application.Commands;
using Microsoft.AspNetCore.Mvc;
using Domain.Persistence.DTOs;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AnalysisController(IMediator mediator) : ControllerBase
{
    private readonly IMediator _mediator = mediator;

    [HttpPost("query")]
    public async Task<ActionResult<QueryAnswer>> SubmitQuery([FromBody] SubmitQuery query) 
    {
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpPost("upload")]
    public async Task<IActionResult> UploadFile(IFormFile file) => Ok( new { DocumentId = await _mediator.Send(new UploadFileCommand { File = file }) });
}
using MediatR;
using Application.Queries;
using Application.Commands;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AnalysisController(IMediator mediator) : ControllerBase
{
    private readonly IMediator _mediator = mediator;

    [HttpPost("query")]
    public async Task<IActionResult> SubmitQuery([FromBody] SubmitQuery query) => Ok( new { Result = await _mediator.Send(query) });

    [HttpPost("upload")]
    public async Task<IActionResult> UploadFile(IFormFile file) => Ok( new { DocumentId = await _mediator.Send(new { File = file }) });
}
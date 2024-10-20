using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics.CodeAnalysis;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
[ExcludeFromCodeCoverage]
public class BaseController(IMediator mediator) : ControllerBase
{
    protected readonly IMediator _mediator = mediator;
}
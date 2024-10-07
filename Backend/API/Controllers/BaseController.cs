using System.Diagnostics.CodeAnalysis;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ExcludeFromCodeCoverage]
[ApiController]
[Route("api/[controller]")]
public class BaseController(IMediator mediator) : ControllerBase
{
    protected readonly IMediator _mediator = mediator;
}
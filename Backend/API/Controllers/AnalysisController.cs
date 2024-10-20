using MediatR;
using Application.Queries;
using Application.Commands;
using Domain.Persistence.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

/// <summary>
/// AnalysisController handles the API requests for the LLM and the vector database.
/// dependencies: IMediator
/// routes: api/analysis
/// endpoints: /query, /upload
/// </summary>
/// <param name="mediator"></param>
public class AnalysisController(IMediator mediator) : BaseController(mediator)
{
    /// <summary>
    /// Submits a query to the LLM and returns the answer.
    /// Computes the embedding of the query with the LLM and compares it to the embeddings of the rows in the database.
    /// </summary>
    /// <param name="query"></param>
    /// <returns>
    ///     string Answer
    ///     string Question
    ///     string DocumentId
    ///     List<Dictionary<string, object>> RelevantRows
    /// </returns>
    [HttpPost("query")]
    [ProducesResponseType(typeof(QueryAnswer), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SubmitQuery([FromBody] SubmitQuery query)
    {
        try
        {
            var result = await _mediator.Send(query);
            if (result == null)
            {
                return BadRequest("Invalid query or unable to process the request.");
            }
            return Ok(result);
        }
        catch (Exception ex)
        {
            // Log the exception here
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
        }
    }

    /// <summary>
    /// Uploads an excel file to the vector database and returns the documentId. 
    /// If the upload fails, returns null.
    /// Iterates over the rows of the excel file and saves them to the vector database.
    /// Computes each row's embedding with the LLM and stores it in the database.
    /// So when a query is submitted, the LLM can be used to compute the embedding of the query and compare it to the embeddings of the rows.
    /// </summary>
    /// <param name="file"></param>
    /// <returns>Document Id - Nullable</returns>
    [HttpPost("upload")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UploadFile([FromForm] IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("No file uploaded.");
        }

        try
        {
            var result = await _mediator.Send(new UploadFileCommand { File = file });
            if (string.IsNullOrEmpty(result))
            {
                return BadRequest("File upload failed.");
            }
            return Ok(result);
        }
        catch (Exception ex)
        {
            // Log the exception here
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while uploading the file.");
        }
    }
}

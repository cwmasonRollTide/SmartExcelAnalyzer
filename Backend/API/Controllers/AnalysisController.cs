using MediatR;
using Persistence.Hubs;
using Application.Queries;
using Application.Commands;
using Domain.Persistence.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

/// <summary>
/// AnalysisController handles the API requests for the LLM and the vector database.
/// dependencies: IMediator, IProgressHubWrapper
/// routes: api/analysis
/// endpoints: /query, /upload
/// </summary>
public class AnalysisController(
    IMediator mediator,
    IProgressHubWrapper _hubContext
) : BaseController(mediator)
{
    /// <summary>
    /// Submits a query to the LLM and returns the answer.
    /// Computes the embedding of the query with the LLM and compares it to the embeddings of the rows in the database.
    /// </summary>
    /// <param name="queryAboutExcelDocument"></param>
    /// <returns>
    ///     string Answer
    ///     string Question
    ///     string DocumentId
    ///     List<Dictionary<string, object>> RelevantRows
    /// </returns>
    [HttpPost("query")]
    [ProducesResponseType(typeof(QueryAnswer), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status408RequestTimeout)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SubmitQuery(
        [FromBody] SubmitQuery queryAboutExcelDocument, 
        CancellationToken cancellationToken = default
    ) =>
        Ok(await _mediator.Send(queryAboutExcelDocument, cancellationToken));

    /// <summary>
    /// Uploads an excel file to the vector database and returns the documentId. 
    /// If the upload fails, returns null.
    /// Iterates over the rows of the excel file and saves them to the vector database.
    /// Computes each row's embedding with the LLM and stores it in the database.
    /// So when a query is submitted, the LLM can be used to compute the embedding of the query and compare it to the embeddings of the rows.
    /// </summary>
    /// <param name="fileToUpload"></param>
    /// <returns>Document Id - Nullable</returns>
    [HttpPost("upload")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status408RequestTimeout)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadFile(
        [FromForm] IFormFile fileToUpload, 
        CancellationToken cancellationToken = default
    ) =>
        Ok(
            await _mediator.Send(new UploadFileCommand
            {
                File = fileToUpload,
                Progress = new Progress<(
                    double ParseProgress,
                    double SaveProgress
                )>(
                    async progressTuple =>
                        await _hubContext.SendProgress(
                            progressTuple.ParseProgress,
                            progressTuple.SaveProgress,
                            cancellationToken
                        )
                )
            }, cancellationToken)
        );
}

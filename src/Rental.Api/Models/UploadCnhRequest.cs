using Microsoft.AspNetCore.Http;

namespace Rental.Api.Models;

public class UploadCnhRequest
{
    public IFormFile? File { get; set; }
}
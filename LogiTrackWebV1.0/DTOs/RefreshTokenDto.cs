using System.ComponentModel.DataAnnotations;

namespace LogiTrackWebV1._0.DTOs
{
    public class RefreshTokenDto
    {

        [Required]
        public string AccessToken { get; set; } = string.Empty;

        [Required]
        public string RefreshToken { get; set; } = string.Empty;
    }
}

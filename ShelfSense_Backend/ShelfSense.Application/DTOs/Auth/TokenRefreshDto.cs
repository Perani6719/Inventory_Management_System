using System.ComponentModel.DataAnnotations;

namespace ShelfSense.Application.DTOs.Auth
{
    public class TokenRefreshDto
    {
        //[Required(ErrorMessage = "Access token is required.")]
        //public string AccessToken { get; set; }

        [Required(ErrorMessage = "Refresh token is required.")]
        public string RefreshToken { get; set; }
    }
}
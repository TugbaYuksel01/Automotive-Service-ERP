using System.ComponentModel.DataAnnotations;

namespace OtoServisApi.DTOs
{
    public class SifremiUnuttumDto
    {
        [Required(ErrorMessage = "E-posta adresi zorunludur.")]
        [EmailAddress]
        public string Email { get; set; }
    }

}